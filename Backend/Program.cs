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

builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<Backend.Infrastructure.Email.IEmailSender, Backend.Infrastructure.Email.LoggingEmailSender>();
builder.Services.AddScoped<Backend.Infrastructure.Parents.IParentService, Backend.Infrastructure.Parents.ParentService>();
builder.Services.AddScoped<Backend.Infrastructure.Admin.IAdminService, Backend.Infrastructure.Admin.AdminService>();
builder.Services.AddScoped<Backend.Infrastructure.Admin.ITutorAdminService, Backend.Infrastructure.Admin.TutorAdminService>();
builder.Services.AddScoped<Backend.Infrastructure.Admin.IUserAdminService, Backend.Infrastructure.Admin.UserAdminService>();
builder.Services.AddScoped<Backend.Infrastructure.Admin.ISubjectAdminService, Backend.Infrastructure.Admin.SubjectAdminService>();
builder.Services.AddScoped<Backend.Infrastructure.Admin.IMonitoringAdminService, Backend.Infrastructure.Admin.MonitoringAdminService>();
builder.Services.AddScoped<Backend.Infrastructure.Admin.IDisputeAdminService, Backend.Infrastructure.Admin.DisputeAdminService>();
builder.Services.AddScoped<Backend.Infrastructure.Admin.IAnalyticsAdminService, Backend.Infrastructure.Admin.AnalyticsAdminService>();

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
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

app.UseCors("AllowClient");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
