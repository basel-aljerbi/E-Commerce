using System.Threading.RateLimiting;
using E_Commerce.API.Data;
using E_Commerce.API.Endpoints;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Middleware;
using E_Commerce.API.Models;
using E_Commerce.API.Models.Enums;
using E_Commerce.API.Options;
using E_Commerce.API.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Stripe;
using System.Text;

// ============================================================
// Serilog Bootstrap
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Skip Serilog in Test environment to avoid frozen-logger conflicts
    // when WebApplicationFactory runs Program.Main in parallel
    var isTestEnv = string.Equals(
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        "Test",
        StringComparison.OrdinalIgnoreCase);

    if (!isTestEnv)
    {
        // ============================================================
        // Serilog (full pipeline)
        // ============================================================
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
                    "{Properties:j}{NewLine}{Exception}");

            var seqUrl = context.Configuration["Seq:Url"];
            if (!string.IsNullOrWhiteSpace(seqUrl))
            {
                configuration.WriteTo.Seq(seqUrl,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
            }
        });
    }

    // ============================================================
    // CORS - Production-safe: fails if origins missing in production
    // ============================================================
    var corsSection = builder.Configuration.GetSection(CorsOptions.SectionName);
    if (corsSection.Exists() is false)
        throw new InvalidOperationException("CORS configuration section is missing");

    var corsOptions = corsSection.Get<CorsOptions>()
        ?? throw new InvalidOperationException("Failed to bind CORS configuration");

    if (builder.Environment.IsProduction() &&
        (corsOptions.AllowedOrigins is null || corsOptions.AllowedOrigins.Length == 0))
    {
        throw new InvalidOperationException(
            "CORS AllowedOrigins must be configured in production. " +
            "Set Cors:AllowedOrigins in configuration.");
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            if (corsOptions.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(corsOptions.AllowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                // Dev-only fallback (never reaches here in production due to guard above)
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
    });

    // ============================================================
    // Configuration Validation
    // ============================================================
    builder.Services.AddOptions<JwtOptions>()
        .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<StripeOptions>()
        .Bind(builder.Configuration.GetSection(StripeOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<SendGridOptions>()
        .Bind(builder.Configuration.GetSection(SendGridOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<RateLimitingOptions>()
        .Bind(builder.Configuration.GetSection(RateLimitingOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // ============================================================
    // Database
    // ============================================================
    builder.AddECommerceDb();

    // ============================================================
    // FluentValidation
    // ============================================================
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // ============================================================
    // JWT Authentication
    // ============================================================
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT configuration is missing");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Key)),
                ClockSkew = TimeSpan.Zero
            };
        });

    // ============================================================
    // Authorization Policies
    // ============================================================
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
    });

    // ============================================================
    // Rate Limiting
    // ============================================================
    var rateLimitOptions = builder.Configuration.GetSection(RateLimitingOptions.SectionName)
        .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("Global", config =>
        {
            config.PermitLimit = rateLimitOptions.PermitLimit;
            config.Window = TimeSpan.FromSeconds(rateLimitOptions.WindowInSeconds);
            config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            config.QueueLimit = 0;
        });

        options.AddFixedWindowLimiter("Auth", config =>
        {
            config.PermitLimit = rateLimitOptions.AuthPermitLimit;
            config.Window = TimeSpan.FromMinutes(rateLimitOptions.AuthWindowInMinutes);
            config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            config.QueueLimit = 0;
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ============================================================
    // Swagger / OpenAPI
    // ============================================================
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "E-Commerce API",
            Version = "v1",
            Description = "Production-grade e-commerce backend API"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT Token"
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

    // ============================================================
    // Response Compression
    // ============================================================
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // ============================================================
    // Exception Handling
    // ============================================================
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ============================================================
    // Services
    // ============================================================
    builder.Services.AddScoped<JwtService>();
    builder.Services.AddScoped<StripeService>();
    builder.Services.AddScoped<EmailService>();
    builder.Services.AddScoped<AuditService>();
    builder.Services.AddScoped<FileUploadService>();
    builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
    builder.Services.AddHostedService<BackgroundJobProcessor>();
    builder.Services.AddHttpContextAccessor();

    // ============================================================
    // Health Checks
    // ============================================================
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ECommerceContext>("database");

    var app = builder.Build();

    // ============================================================
    // Stripe API Key (set once globally)
    // ============================================================
    var stripeOpts = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StripeOptions>>();
    StripeConfiguration.ApiKey = stripeOpts.Value.SecretKey;

    // ============================================================
    // Middleware Pipeline
    // ============================================================
    app.UseCorrelation();
    app.UseRequestLogging();
    if (!isTestEnv) app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseHsts();
    }

    app.UseExceptionHandler();

    if (!app.Environment.IsEnvironment("Test"))
    {
        app.UseHttpsRedirection();
    }

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "0";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
        {
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' https://js.stripe.com 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: blob:; " +
                "frame-src https://js.stripe.com https://hooks.stripe.com; " +
                "connect-src 'self' https://api.stripe.com; " +
                "font-src 'self' data:;";
        }
        await next();
    });

    app.UseResponseCompression();
    app.UseStaticFiles();
    app.UseCors("AllowFrontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // ============================================================
    // Endpoints
    // ============================================================
    if (app.Environment.IsDevelopment())
    {
        app.MapPost("/dev/setup", async (
            ECommerceContext db,
            CancellationToken ct) =>
        {
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@admin.com", ct);
            if (admin is not null)
            {
                admin.Role = "Admin";
                admin.IsEmailVerified = true;
            }
            else
            {
                db.Users.Add(new User
                {
                    Email = "admin@admin.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
                    FullName = "Admin",
                    Role = "Admin",
                    IsEmailVerified = true
                });
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Admin account ready — email: admin@admin.com, password: Admin@123" });
        }).AllowAnonymous();
    }

    app.MapProductsEndpoints();
    app.MapCartEndpoints();
    app.MapOrdersEndpoints();
    app.MapAuthEndpoints().RequireRateLimiting("Auth");
    app.MapReviewsEndpoints();
    app.MapWishlistEndpoints();
    app.MapAuditLogsEndpoints();
    app.MapPaymentsEndpoints();
    app.MapPublicCategoriesEndpoint();
    app.MapProfileEndpoints();
    app.MapAdminEndpoints();
    app.MapAnalyticsEndpoints();

    // Health check endpoints
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapHealthChecks("/health/ready").AllowAnonymous();
    app.MapHealthChecks("/health/live").AllowAnonymous();

    // ============================================================
    // Database Migration & Seeding
    // ============================================================
    if (app.Environment.IsDevelopment() || args.Contains("--migrate"))
    {
        await app.MigrateDbAsync();

        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ECommerceContext>();
            await context.SeedDataAsync();
        }
    }

    await app.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
