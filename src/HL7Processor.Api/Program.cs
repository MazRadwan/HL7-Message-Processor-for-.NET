using HL7Processor.Core.Communication.Queue;
using HL7Processor.Core.Communication.MLLP;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<IMessageQueue, InMemoryMessageQueue>();
builder.Services.AddSignalR();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapHub<MessageHub>("/hub/messages");

app.Run();

public class MessageHub : Hub { } 