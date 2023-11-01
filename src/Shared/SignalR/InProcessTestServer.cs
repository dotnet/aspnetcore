// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Tests;

public abstract class InProcessTestServer : IAsyncDisposable
{
    internal abstract event Action<LogRecord> ServerLogged;

    public abstract string WebSocketsUrl { get; }

    public abstract string Url { get; }

    public abstract ValueTask DisposeAsync();
}

public class InProcessTestServer<TStartup> : InProcessTestServer
    where TStartup : class
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private IHost _host;
    private IHostApplicationLifetime _lifetime;
    private readonly IDisposable _logToken;
    private readonly IDisposable _extraDisposable;
    private readonly Action<KestrelServerOptions> _configureKestrelServerOptions;

    private readonly LogSinkProvider _logSinkProvider;
    private string _url;

    internal override event Action<LogRecord> ServerLogged
    {
        add => _logSinkProvider.RecordLogged += value;
        remove => _logSinkProvider.RecordLogged -= value;
    }

    public override string WebSocketsUrl => Url.Replace("http", "ws");

    public override string Url => _url;

    public static async Task<InProcessTestServer<TStartup>> StartServer(ILoggerFactory loggerFactory, Action<KestrelServerOptions> configureKestrelServerOptions = null, IDisposable disposable = null)
    {
        var server = new InProcessTestServer<TStartup>(loggerFactory, configureKestrelServerOptions, disposable);
        await server.StartServerInner();
        return server;
    }

    private InProcessTestServer() : this(loggerFactory: null, null, null)
    {
    }

    private InProcessTestServer(ILoggerFactory loggerFactory, Action<KestrelServerOptions> configureKestrelServerOptions, IDisposable disposable)
    {
        _configureKestrelServerOptions = configureKestrelServerOptions;
        _extraDisposable = disposable;
        _logSinkProvider = new LogSinkProvider();

        if (loggerFactory == null)
        {
            var testLog = AssemblyTestLog.ForAssembly(typeof(TStartup).Assembly);
            _logToken = testLog.StartTestLog(null, $"{nameof(InProcessTestServer<TStartup>)}_{typeof(TStartup).Name}",
                out _loggerFactory, nameof(InProcessTestServer));
        }
        else
        {
            _loggerFactory = loggerFactory;
        }

        _loggerFactory = new WrappingLoggerFactory(_loggerFactory);
        _loggerFactory.AddProvider(_logSinkProvider);
        _logger = _loggerFactory.CreateLogger<InProcessTestServer<TStartup>>();
    }

    public IList<LogRecord> GetLogs() => _logSinkProvider.GetLogs();

    private async Task StartServerInner()
    {
        // We're using 127.0.0.1 instead of localhost to ensure that we use IPV4 across different OSes
        var url = "http://127.0.0.1:0";

        _host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureLogging(builder => builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(new ForwardingLoggerProvider(_loggerFactory)))
                .UseStartup(typeof(TStartup))
                .UseKestrel(o => _configureKestrelServerOptions?.Invoke(o))
                .UseUrls(url)
                .UseContentRoot(Directory.GetCurrentDirectory());
            }).Build();

        _logger.LogInformation("Starting test server...");
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await _host.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            var logs = _logSinkProvider.GetLogs();
            throw new TimeoutException($"Timed out waiting for application to start.{Environment.NewLine}Startup Logs:{Environment.NewLine}{RenderLogs(logs)}");
        }

        _logger.LogInformation("Test Server started");

        // Get the URL from the server
        _url = _host.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();

        _lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
        _lifetime.ApplicationStopped.Register(() =>
        {
            _logger.LogInformation("Test server shut down");
            _logToken?.Dispose();
        });
    }

    private static string RenderLogs(IList<LogRecord> logs)
    {
        var builder = new StringBuilder();
        foreach (var log in logs)
        {
            builder.AppendLine(FormattableString.Invariant($"{log.Timestamp:O} {log.Write.LoggerName} {log.Write.LogLevel}: {log.Write.Formatter(log.Write.State, log.Write.Exception)}"));
            if (log.Write.Exception != null)
            {
                var message = log.Write.Exception.ToString();
                foreach (var line in message.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    builder.AppendLine(FormattableString.Invariant($"| {line}"));
                }
            }
        }
        return builder.ToString();
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            _extraDisposable?.Dispose();
            _logger.LogInformation("Start shutting down test server");
        }
        finally
        {
            await _host.StopAsync();
            _host.Dispose();
            _loggerFactory.Dispose();
        }
    }

    private sealed class ForwardingLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerFactory _loggerFactory;

        public ForwardingLoggerProvider(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }
    }
}
