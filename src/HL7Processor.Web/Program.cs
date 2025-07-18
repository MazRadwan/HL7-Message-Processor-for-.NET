using HL7Processor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HL7Processor.Web.Services;
using HL7Processor.Web.Hubs;
using HL7Processor.Infrastructure.Repositories;
using HL7Processor.Core.Communication.Queue;
using HL7Processor.Api.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Database
var connectionString = Environment.GetEnvironmentVariable("HL7_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("Hl7Db") 
    ?? throw new InvalidOperationException("HL7_CONNECTION_STRING environment variable or 'Hl7Db' connection string is required");

builder.Services.AddDbContextPool<HL7DbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContextFactory<HL7DbContext>(options =>
    options.UseSqlServer(connectionString));


// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

// Get JWT secret from environment variable or configuration
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? builder.Configuration["JWT_SECRET_KEY"] 
    ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable or configuration value is required");

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

// Application Services
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISystemHealthService, SystemHealthService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IMessageQueue, InMemoryMessageQueue>();

// Stage 6b: Parser & Validation Services
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<IParserMetricsService, ParserMetricsService>();

// Stage 6c: Transformation Services
builder.Services.AddScoped<ITransformationService, TransformationService>();

// HTTP Client for API calls
builder.Services.AddHttpClient("HL7ProcessorApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001/");
});

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapHub<DashboardHub>("/dashboardHub");
app.MapHub<SystemHub>("/systemhub");
app.MapFallbackToPage("/_Host");

// Ensure database is created and seeded
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

app.Run();