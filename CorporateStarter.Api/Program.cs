using CorporateStarter.Api.Infrastructure.Auth;
using CorporateStarter.Api.Infrastructure.Diagnostics;
using CorporateStarter.Application;
using CorporateStarter.Application.Common.Interfaces.Repositories.Audit;
using CorporateStarter.Application.Common.Interfaces.Repositories.Auth;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Application.Common.Interfaces.Security.Mfa;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Common.Security.Mfa;
using CorporateStarter.Infrastructure.Mapping;
using CorporateStarter.Infrastructure.Persistence;
using CorporateStarter.Infrastructure.Persistence.Repositories.Audit;
using CorporateStarter.Infrastructure.Persistence.Repositories.Auth;
using CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Companies;
using CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Persons;
using CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Cities;
using CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Countries;
using CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Positions;
using CorporateStarter.Infrastructure.Persistence.Repositories.Security;
using CorporateStarter.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

const string CorsPolicyName = "DefaultCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthLogin", context =>
    {
        var ip = RateLimitPartitionKeyHelper.GetClientIp(context);
        var loginFingerprint = context.Items.TryGetValue(
            LoginRateLimitFingerprintMiddleware.ItemKey,
            out var value)
            ? value?.ToString()
            : "unknown-login";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"login:{ip}:{loginFingerprint}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.AddPolicy("AuthIp", context =>
    {
        var ip = RateLimitPartitionKeyHelper.GetClientIp(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ip:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.AddPolicy("AuthRefresh", context =>
    {
        var ip = RateLimitPartitionKeyHelper.GetClientIp(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"refresh:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CorporateStarter API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste JWT access token. Do not include the Bearer prefix.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

builder.Services.Configure<InitialAdminOptions>(
    builder.Configuration.GetSection(InitialAdminOptions.SectionName));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            IssuerSigningKeyResolver = (_, _, kid, _) =>
            {
                var signingKeyOptions = builder.Configuration
                    .GetSection(JwtSigningKeyOptions.SectionName)
                    .Get<JwtSigningKeyOptions>();

                if (signingKeyOptions is null || string.IsNullOrWhiteSpace(kid))
                {
                    return [];
                }

                var now = DateTime.UtcNow;

                return signingKeyOptions.Keys
                    .Where(x =>
                        x.IsEnabled
                        && x.KeyId == kid
                        && !string.IsNullOrWhiteSpace(x.Secret)
                        && (x.NotBeforeUtc is null || x.NotBeforeUtc <= now)
                        && (x.NotAfterUtc is null || x.NotAfterUtc > now))
                    .Select(x => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(x.Secret))
                    {
                        KeyId = x.KeyId
                    });
            },
            ValidateIssuerSigningKey = true,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        if (builder.Environment.IsEnvironment("Testing"))
        {
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    context.Response.Headers.Append(
                        "X-Test-Auth-Failed",
                        context.Exception.GetType().Name + ": " + context.Exception.Message);

                    return Task.CompletedTask;
                }
            };
        }

    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(
            permission.Code,
            policy => policy.RequireClaim("permission", permission.Code));
    }
});




builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString);
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly);
});

builder.Services.AddScoped<IAuditReadRepository, AuditReadRepository>();
builder.Services.AddScoped<ICompanyReadRepository, CompanyReadRepository>();
builder.Services.AddScoped<ICompanyWriteRepository, CompanyWriteRepository>();
builder.Services.AddScoped<IPersonReadRepository, PersonReadRepository>();
builder.Services.AddScoped<IPersonWriteRepository, PersonWriteRepository>();
builder.Services.AddScoped<ICityReadRepository, CityReadRepository>();
builder.Services.AddScoped<ICityWriteRepository, CityWriteRepository>();
builder.Services.AddScoped<ICountryReadRepository, CountryReadRepository>();
builder.Services.AddScoped<ICountryWriteRepository, CountryWriteRepository>();
builder.Services.AddScoped<IPositionReadRepository, PositionReadRepository>();
builder.Services.AddScoped<IPositionWriteRepository, PositionWriteRepository>();
builder.Services.AddScoped<IRoleReadRepository, RoleReadRepository>();
builder.Services.AddScoped<IRoleWriteRepository, RoleWriteRepository>();
builder.Services.AddScoped<IUserReadRepository, UserReadRepository>();
builder.Services.AddScoped<IUserWriteRepository, UserWriteRepository>();
builder.Services.AddScoped<IPermissionReadRepository, PermissionReadRepository>();
builder.Services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserAuthRepository, UserAuthRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services
    .AddOptions<CsrfOptions>()
    .Bind(builder.Configuration.GetSection(CsrfOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddScoped<CsrfTokenService>();
builder.Services.AddScoped<CsrfCookieHelper>();
builder.Services.AddScoped<RequireCsrfFilter>();

builder.Services
    .AddOptions<RefreshTokenOptions>()
    .Bind(builder.Configuration.GetSection(RefreshTokenOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddScoped<RefreshTokenCookieHelper>();

builder.Services.AddScoped<ILoginAttemptRepository, LoginAttemptRepository>();

builder.Services
    .AddOptions<JwtSigningKeyOptions>()
    .Bind(builder.Configuration.GetSection(JwtSigningKeyOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<JwtSigningKeyOptions>>().Value);

builder.Services.AddSingleton<IValidateOptions<JwtSigningKeyOptions>, JwtSigningKeyOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<RefreshTokenOptions>, RefreshTokenOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<CsrfOptions>, CsrfOptionsValidator>();

builder.Services.Configure<PasswordPolicyOptions>(
    builder.Configuration.GetSection(PasswordPolicyOptions.SectionName));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<PasswordPolicyOptions>>().Value);

builder.Services.AddSingleton<IPasswordPolicyValidator, PasswordPolicyValidator>();

builder.Services.Configure<PasswordHistoryOptions>(
    builder.Configuration.GetSection(PasswordHistoryOptions.SectionName));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<PasswordHistoryOptions>>().Value);

builder.Services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();

builder.Services.AddScoped<IMfaPolicyService, NoOpMfaPolicyService>();
builder.Services.AddScoped<IMfaChallengeService, NoOpMfaChallengeService>();

MapsterConfig.RegisterMappings();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed(_ => builder.Environment.IsDevelopment());
        }
        else
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<RefreshTokenOptions>>().Value);

builder.Services.Configure<LoginAttemptOptions>(
    builder.Configuration.GetSection(LoginAttemptOptions.SectionName));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<LoginAttemptOptions>>().Value);

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<CsrfOptions>>().Value);


var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var initialAdminOptions = scope.ServiceProvider
        .GetRequiredService<IOptions<InitialAdminOptions>>()
        .Value;

    if (app.Environment.IsEnvironment("Testing"))
    {
        await dbContext.Database.MigrateAsync();
    }

    await AppDbSeeder.SeedAsync(dbContext, initialAdminOptions);
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.TryAdd("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");

    await next();
});

app.UseCors(CorsPolicyName);

app.UseMiddleware<LoginRateLimitFingerprintMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    app = "CorporateStarter.Api",
    timeUtc = DateTime.UtcNow
}));

app.Run();

public partial class Program
{
}
