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


static string? NormalizeConnectionString(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    var trimmed = value.Trim();
    if (trimmed.StartsWith("<", StringComparison.Ordinal)
        && trimmed.EndsWith(">", StringComparison.Ordinal)
        && trimmed.Contains("connection string", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    var placeholderPatterns = new[]
    {
        "your supabase postgres connection string",
        "your postgres connection string",
        "your connection string",
        "<your supabase postgres connection string>",
        "<your postgres connection string>",
        "<your connection string>"
    };

    if (placeholderPatterns.Any(pattern => trimmed.Equals(pattern, StringComparison.OrdinalIgnoreCase)))
    {
        return null;
    }

    return trimmed;
}

var connectionString = new[]
    {
        builder.Configuration.GetConnectionString("DefaultConnection"),
        builder.Configuration["Supabase:ConnectionString"],
        builder.Configuration["SUPABASE_CONNECTION_STRING"],
        builder.Configuration["ConnectionStrings__DefaultConnection"],
    }
    .Select(NormalizeConnectionString)
    .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No database connection string configured. Set ConnectionStrings:DefaultConnection or SUPABASE_CONNECTION_STRING with a valid Supabase Postgres connection string.");
}

Console.WriteLine($"[DEBUG] connectionString resolved, length={connectionString.Length}");

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