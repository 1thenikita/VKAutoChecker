using Serilog;
using VKAutoChecker;
using VKAutoChecker.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddSerilog();
builder.Services.AddSerilog();
// builder.Services.AddHostedService<Worker>();

builder.Services.AddHostedService<VkService>();

var host = builder.Build();

host.Run();