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

// Use environment variable to switch between DirectSsl and standard TLS
// Set USE_STANDARD_TLS=1 to use standard Kestrel TLS (SslStream)
var useStandardTls = Environment.GetEnvironmentVariable("USE_STANDARD_TLS") == "1";
var logFilePath = "directssl.log";

// Clear log file on startup
File.WriteAllText(logFilePath, $"=== DirectSslTransportApp started at {DateTime.Now} (StandardTLS={useStandardTls}) ===\n");

var builder = WebApplication.CreateSlimBuilder(args);

// Disable file logging for benchmarking (causes I/O contention)
// builder.Logging.AddProvider(new FileLoggerProvider(logFilePath));

// Add global exception handlers to catch crashes
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var msg = $"[FATAL] UnhandledException: {e.ExceptionObject}\n";
    Console.Error.WriteLine(msg);
    File.AppendAllText(logFilePath, msg);
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    var msg = $"[FATAL] UnobservedTaskException: {e.Exception}\n";
    Console.Error.WriteLine(msg);
    File.AppendAllText(logFilePath, msg);
    e.SetObserved();
};

if (!useStandardTls)
{
    Console.WriteLine("Using DirectSsl transport (OpenSSL)");
    builder.Logging.SetMinimumLevel(LogLevel.None); // Disable for benchmarking

    // Configure Kestrel to use the Direct Socket Transport. It by-passes the HttpsMiddleware and SslStream
    builder.WebHost.UseKestrelDirectSslTransport();

    builder.WebHost.UseDirectSslSockets(options =>
    {
        options.CertificatePath = "server-p256.crt";
        options.PrivateKeyPath = "server-p256.key";

        options.WorkerCount = 4;
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
    Console.WriteLine("Using standard Kestrel TLS (SslStream)");
    // Disable verbose logging for better benchmark performance
    builder.Logging.SetMinimumLevel(LogLevel.Warning);

    // Configure Kestrel to use the default Sockets Transport with SslStream
    builder.WebHost.UseKestrel(options =>
    {
        options.ListenAnyIP(5001, listenOptions =>
        {
            listenOptions.UseHttps(new X509Certificate2("server-p256.pfx", "testpassword"));
            listenOptions.Protocols = HttpProtocols.Http1;  // HTTP/1.1 only for fair comparison
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