using HL7Processor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HL7Processor.Web.Services;
using HL7Processor.Web.Hubs;
using HL7Processor.Infrastructure.Repositories;
using HL7Processor.Core.Communication.Queue;
using HL7Processor.Infrastructure.Auth;
using HL7Processor.Application.UseCases;
using HL7Processor.Infrastructure.UseCases;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for Azure App Service
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Database
var connectionString = Environment.GetEnvironmentVariable("HL7_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("Hl7Db") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=HL7ProcessorDb;Trusted_Connection=true;MultipleActiveResultSets=true";

builder.Services.AddDbContextPool<HL7DbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContextFactory<HL7DbContext>(options =>
    options.UseSqlServer(connectionString));


// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

// Get JWT secret from environment variable or configuration
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? builder.Configuration["JWT_SECRET_KEY"] 
    ?? "default-fallback-secret-for-demo-purposes-only";

jwtSettings = new JwtSettings 
{ 
    Issuer = jwtSettings.Issuer, 
    Audience = jwtSettings.Audience, 
    ExpirationMinutes = jwtSettings.ExpirationMinutes,
    SecretKey = jwtSecret 
};
builder.Services.AddSingleton(jwtSettings);

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };

        // Handle authentication for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/dashboardHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Admin"));
});

// Infrastructure Repositories
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// Application Use Cases (Interface -> Implementation mapping)
builder.Services.AddScoped<IGetDashboardDataUseCase, GetDashboardDataUseCase>();
builder.Services.AddScoped<IGetSystemHealthUseCase, GetSystemHealthUseCase>();
builder.Services.AddScoped<IGetValidationDataUseCase, GetValidationDataUseCase>();
builder.Services.AddScoped<IGetParserMetricsUseCase, GetParserMetricsUseCase>();
builder.Services.AddScoped<IGetTransformationDataUseCase, GetTransformationDataUseCase>();
builder.Services.AddScoped<IGetArchivedMessagesUseCase, GetArchivedMessagesUseCase>();
builder.Services.AddScoped<IGetArchivedMessageCountUseCase, GetArchivedMessageCountUseCase>();
builder.Services.AddScoped<ICreateTransformationRuleUseCase, CreateTransformationRuleUseCase>();
builder.Services.AddScoped<IUpdateTransformationRuleUseCase, UpdateTransformationRuleUseCase>();
builder.Services.AddScoped<IDeleteTransformationRuleUseCase, DeleteTransformationRuleUseCase>();
builder.Services.AddScoped<IGetTransformationStatsUseCase, GetTransformationStatsUseCase>();

// Web Layer Services 
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISystemHealthService, SystemHealthService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IParserMetricsService, ParserMetricsService>();
builder.Services.AddScoped<ITransformationService, TransformationService>();

// Infrastructure Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IMessageQueue, InMemoryMessageQueue>();

// Toast Notification Service
builder.Services.AddScoped<IToastService, ToastService>();

// HTTP Client for API calls
builder.Services.AddHttpClient("HL7ProcessorApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001/");
});

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline
// 1. UseForwardedHeaders() - Must be first to properly handle proxy headers
app.UseForwardedHeaders();

// 2. UseHttpsRedirection() - Redirect HTTP to HTTPS (skip in development)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

// 3. UseStaticFiles() - Serve static content
app.UseStaticFiles();

// 4. UseRouting() - Enable routing
app.UseRouting();

// 5. UseAuthentication() - Must come after routing but before authorization
app.UseAuthentication();

// 6. UseAuthorization() - Must come after authentication
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapHub<DashboardHub>("/dashboardHub");
app.MapHub<SystemHub>("/systemhub");
app.MapFallbackToPage("/_Host");

// Ensure database is created and seeded
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<HL7DbContext>();
        context.Database.EnsureCreated();
        
        // Seed with sample data (development only)
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedDataService>>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HL7DbContext>>();
        var seedService = new SeedDataService(context, logger, environment, contextFactory);
        await seedService.SeedDataAsync();
    }
}
catch (Exception ex)
{
    // Log but don't fail the app startup if database setup fails
    Console.WriteLine($"Database setup failed: {ex.Message}");
}

app.Run();