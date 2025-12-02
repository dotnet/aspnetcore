// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateSlimBuilder(args);

var certPath = "localhost.pfx";
X509Certificate2 cert;
if (File.Exists(certPath))
{
    cert = new X509Certificate2(certPath, "password");
    Console.WriteLine($"✓ Loaded certificate from {certPath}");
}
else
{
    throw new NotSupportedException("Startup error does not have cert!");
}

// Configure Kestrel to use the Direct Socket Transport with native OpenSSL integration
builder.WebHost.UseKestrelDirectSocket(cert);

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint on port 5000
    options.ListenAnyIP(5000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });

    // HTTPS endpoint on port 5001 with DirectSocket + OpenSSL
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
});

var app = builder.Build();

// pack into nice extension?
ConfigureSslContext(app.Services, cert);

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index => new
    {
        date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)).ToString("O"),
        temperatureC = Random.Shared.Next(-20, 55),
        summary = summaries[Random.Shared.Next(summaries.Length)]
    }).ToArray();
    return forecast;
});

Console.WriteLine("Starting NativeOpenSslSocketsSampleApp with DirectSocket Transport");
Console.WriteLine();
Console.WriteLine("HTTP endpoint:  http://localhost:5000/weatherforecast");
Console.WriteLine("HTTPS endpoint: https://localhost:5001/weatherforecast");
Console.WriteLine();
Console.WriteLine("DirectSocket Transport Features:");
Console.WriteLine("  ✓ Direct socket to application path (no SslStream wrapper)");
Console.WriteLine("  ✓ Bypasses HttpsConnectionMiddleware");
Console.WriteLine("  ✓ Native OpenSSL integration");
Console.WriteLine("  ✓ Foundation for zero-copy TLS processing");
Console.WriteLine("  ✓ Reduced memory allocations");
Console.WriteLine("  ✓ Lower latency connection handling");
Console.WriteLine();

await app.RunAsync();

void ConfigureSslContext(IServiceProvider services, X509Certificate2 certificate)
{
    var factories = services.GetServices<IConnectionListenerFactory>();
    foreach (var factory in factories)
    {
        if (factory is DirectSocketTransportFactory directFactory)
        {
            directFactory.InitializeSslContext(certificate);
            return;
        }
    }

    // if we are here, we dont have directsockettransportfactory, so throw
    throw new NotSupportedException($"Did not register {nameof(DirectSocketTransportFactory)}. please reconfigure");
}
