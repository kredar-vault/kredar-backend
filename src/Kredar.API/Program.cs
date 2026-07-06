using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Kredar.API.ApiKeys;
using Kredar.API.Auth;
using Kredar.API.Checkout;
using Kredar.API.Common;
using Kredar.API.Config;
using Kredar.API.Customers;
using Kredar.API.Data;
using Kredar.API.DedicatedAccounts;
using Kredar.API.Nomba;
using Kredar.API.Settlement;
using Kredar.API.Team;
using Kredar.API.Tenants;
using Kredar.API.Transactions;
using Kredar.API.Transfers;
using Kredar.API.Insights;
using Kredar.API.Settlement;
using Kredar.API.Webhooks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Resend;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<NombaSettings>(builder.Configuration.GetSection("NombaSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Resend"));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("scope", "admin"));
});

// CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? builder.Configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:3000", "https://localhost:3000"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Resend email
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["Resend:ApiKey"] ?? "";
});

// HTTP clients
var nombaSettings = builder.Configuration.GetSection("NombaSettings").Get<NombaSettings>();
builder.Services.AddHttpClient("nomba", c =>
{
    var baseUrl = nombaSettings?.BaseUrl ?? "https://api.nomba.com/v1/";
    if (!baseUrl.EndsWith('/')) baseUrl += "/";
    c.BaseAddress = new Uri(baseUrl);
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("outbound-webhook", c =>
{
    c.Timeout = TimeSpan.FromSeconds(15);
});

// Nomba services
builder.Services.AddSingleton<NombaTokenProvider>();
builder.Services.AddScoped<NombaClient>();
builder.Services.AddScoped<NombaSignatureVerifier>();

// Auth services
builder.Services.AddScoped<TenantRepository>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<RefreshTokenRepository>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();

// Customer services
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<KycRepository>();
builder.Services.AddScoped<KycService>();

// Transaction services
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<TransactionService>();

// Team services
builder.Services.AddScoped<TeamRepository>();
builder.Services.AddScoped<TeamService>();

// Dedicated account services
builder.Services.AddScoped<DedicatedAccountRepository>();
builder.Services.AddScoped<DedicatedAccountService>();

// Transfer services
builder.Services.AddScoped<TransferRepository>();
builder.Services.AddScoped<TransferService>();

// Insights
builder.Services.AddScoped<InsightsService>();

// Webhook services
builder.Services.AddScoped<WebhookEndpointRepository>();
builder.Services.AddScoped<WebhookDeliveryRepository>();
builder.Services.AddScoped<WebhookEndpointService>();
builder.Services.AddScoped<NombaWebhookService>();
builder.Services.AddHostedService<WebhookDeliveryWorker>();
builder.Services.AddHostedService<ReconciliationSweepWorker>();
builder.Services.AddHostedService<SettlementWorker>();

// API Keys
builder.Services.AddScoped<ApiKeyRepository>();
builder.Services.AddScoped<ApiKeyService>();

// Checkout
builder.Services.AddSingleton<CheckoutEventBus>();
builder.Services.AddScoped<CheckoutService>();

// Settlement
builder.Services.AddScoped<SettlementService>();

// Per-API-key rate limiting: test keys = 60 req/min, live keys = 300 req/min
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("api-key-limit", httpContext =>
    {
        var clientId = httpContext.Request.Headers["X-Client-Id"].FirstOrDefault()
            ?? httpContext.User.FindFirstValue("clientId")
            ?? "anonymous";
        var isLive = clientId.StartsWith("krd_live", StringComparison.OrdinalIgnoreCase);
        return RateLimitPartition.GetSlidingWindowLimiter(clientId, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = isLive ? 300 : 60,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            error = "Rate limit exceeded. Upgrade to a live API key for higher limits.",
            retryAfterSeconds = 60
        }, token);
    };
});

// Exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health check
builder.Services.AddHealthChecks();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Kredar API", Version = "v1" });
    c.CustomSchemaIds(type => type.FullName);
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token here."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
