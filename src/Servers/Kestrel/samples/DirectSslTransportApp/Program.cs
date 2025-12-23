// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var withCustomDirectTransport = true;
var logFilePath = "directssl.log";

// Clear log file on startup
File.WriteAllText(logFilePath, $"=== DirectSslTransportApp started at {DateTime.Now} ===\n");

var builder = WebApplication.CreateSlimBuilder(args);

// Add simple file logging
builder.Logging.AddProvider(new FileLoggerProvider(logFilePath));

if (withCustomDirectTransport)
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug); // disable otherwise bad perf

    // Configure Kestrel to use the Direct Socket Transport. It by-passes the HttpsMiddleware and SslStream
    builder.WebHost.UseKestrelDirectSslTransport();

    builder.WebHost.UseDirectSslSockets(options =>
    {
        options.CertificatePath = "server-p384.crt";
        options.PrivateKeyPath = "server-p384.key";

        options.WorkerCount = 1;
    });

    builder.WebHost.ConfigureKestrel(options =>
    {
        // HTTPS endpoint on port 5001 with DirectSocket + OpenSSL
        options.ListenAnyIP(5001, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });
    });
}
else
{
    // Disable verbose logging for better benchmark performance
    builder.Logging.SetMinimumLevel(LogLevel.Warning);

    // Configure Kestrel to use the default Sockets Transport with SslStream
    builder.WebHost.UseKestrel(options =>
    {
        options.ListenAnyIP(5001, listenOptions =>
        {
            listenOptions.UseHttps(new X509Certificate2("server-p384.pfx", "testpassword"));
        });
    });
}

var app = builder.Build();

app.MapGet("/", (HttpContext ctx) =>
{
    return "Hello world";
});

await app.RunAsync();

// Simple file logger implementation
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public FileLoggerProvider(string filePath) => _filePath = filePath;

    public ILogger CreateLogger(string categoryName) => new FileLogger(_filePath, categoryName, _lock);

    public void Dispose() { }
}

public class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly string _categoryName;
    private readonly object _lock;

    public FileLogger(string filePath, string categoryName, object lockObj)
    {
        _filePath = filePath;
        _categoryName = categoryName;
        _lock = lockObj;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var message = $"{DateTime.Now:HH:mm:ss.fff} [{logLevel}] {_categoryName}: {formatter(state, exception)}";
        if (exception != null)
        {
            message += $"\n{exception}";
        }

        lock (_lock)
        {
            File.AppendAllText(_filePath, message + "\n");
        }
    }
}