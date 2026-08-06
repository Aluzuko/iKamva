using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Infrastructure.Auth;
using Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database (Supabase Postgres, via connection string from config/env)
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["Supabase:ConnectionString"]
    ?? builder.Configuration["SUPABASE_CONNECTION_STRING"]
    ?? builder.Configuration["ConnectionStrings__DefaultConnection"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No database connection string configured. Set ConnectionStrings:DefaultConnection or SUPABASE_CONNECTION_STRING.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// ---------------------------------------------------------------------------
// JWT auth
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Jwt configuration section is missing. Set Jwt:Secret via user-secrets/env — see README-BACKEND-SETUP.md.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// App services
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<Backend.Infrastructure.Email.IEmailSender, Backend.Infrastructure.Email.LoggingEmailSender>();
builder.Services.AddScoped<Backend.Infrastructure.Parents.IParentService, Backend.Infrastructure.Parents.ParentService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum values (e.g. UserRole) serialize/deserialize as "student", "parent", etc.
        // — matching the Angular UserRole enum exactly, so no casing translation is needed.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

// ---------------------------------------------------------------------------
// OpenAPI — native ASP.NET Core generation (Microsoft.AspNetCore.OpenApi),
// with Swashbuckle used only for the Swagger UI viewer, not document
// generation. Swashbuckle's own AddSwaggerGen()/OpenApiReference APIs are
// NOT used here — Microsoft.OpenApi v2 (pulled in by Swashbuckle 10.x on
// .NET 10) removed the old Reference-based security scheme API entirely.
// ---------------------------------------------------------------------------
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

app.UseCors("AllowClient");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // serves /openapi/v1.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Backend API v1");
    });

    var clientUrl = app.Configuration["ClientUrl"] ?? "http://localhost:5173";
    app.MapGet("/", () => Results.Redirect(clientUrl));

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await Backend.Infrastructure.Data.DevDataSeeder.SeedAsync(db, hasher);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

/// <summary>
/// Registers the Bearer scheme in the generated OpenAPI document so Swagger UI
/// shows an "Authorize" option. Paste an access token from /api/auth/login there.
/// Note: this declares the scheme at the document level; if you also want the
/// padlock icon on individual protected endpoints, that requires an operation
/// transformer too — left out here since Microsoft.OpenApi's per-operation
/// security-requirement API is still shifting release to release. Test
/// protected endpoints via a manual "Authorization: Bearer <token>" header
/// in Swagger UI's request editor, curl, or Postman in the meantime.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authenticationSchemes.Any(scheme => scheme.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
            },
        };
    }
}