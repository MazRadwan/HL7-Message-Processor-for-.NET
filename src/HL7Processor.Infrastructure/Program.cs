using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HL7Processor.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddEnvironmentVariables();

// Get connection string from environment or configuration
var connectionString = Environment.GetEnvironmentVariable("HL7_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("Hl7Db") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=HL7ProcessorDb;Trusted_Connection=true;MultipleActiveResultSets=true";

// Add DbContext
builder.Services.AddDbContext<HL7DbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// This is just a placeholder for EF migrations
Console.WriteLine("Infrastructure project configured for Entity Framework migrations.");

app.Run();