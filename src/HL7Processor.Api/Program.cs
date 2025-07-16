using HL7Processor.Core.Communication.Queue;
using HL7Processor.Core.Communication.MLLP;
using Microsoft.AspNetCore.SignalR;
using HL7Processor.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HL7Processor.Infrastructure;
using Microsoft.EntityFrameworkCore;
using HL7Processor.Infrastructure.Retention;
using HL7Processor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// JWT settings
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<TokenService>();

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
    });

builder.Services.AddAuthorization();

// Add services
builder.Services.AddSingleton<IMessageQueue, InMemoryMessageQueue>();
builder.Services.AddSignalR();
builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("Hl7Db") ?? "";
builder.Services.AddDbContextPool<HL7DbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<HL7Processor.Infrastructure.Repositories.IMessageRepository, HL7Processor.Infrastructure.Repositories.MessageRepository>();

var retentionSettings = builder.Configuration.GetSection(RetentionSettings.SectionName).Get<RetentionSettings>() ?? new RetentionSettings();
builder.Services.AddSingleton(retentionSettings);
builder.Services.AddScoped<IDataRetentionService, DataRetentionService>();
builder.Services.AddHostedService<DataRetentionBackgroundService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MessageHub>("/hub/messages");

app.Run();

public class MessageHub : Hub { } 