var builder = Host.CreateApplicationBuilder();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();