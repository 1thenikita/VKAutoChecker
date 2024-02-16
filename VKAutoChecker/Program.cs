using Serilog;
using VKAutoChecker;
using VKAutoChecker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog();
// builder.Services.AddHostedService<Worker>();

builder.Services.AddHostedService<VkService>();

var host = builder.Build();

host.Run();