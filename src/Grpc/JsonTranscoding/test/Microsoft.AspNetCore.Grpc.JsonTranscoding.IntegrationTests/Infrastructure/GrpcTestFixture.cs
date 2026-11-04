// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests.Infrastructure;

public class GrpcTestFixture<TStartup> : IDisposable where TStartup : class
{
    private TestServer? _server;
    private IHost? _host;
    private HttpMessageHandler? _handler;
    private Action<IWebHostBuilder>? _configureWebHost;
    private DynamicGrpcServiceRegistry? _dynamicGrpc;

    public event LogMessage? LoggedMessage;
    public DynamicGrpcServiceRegistry DynamicGrpc
    {
        get
        {
            EnsureServer();
            return _dynamicGrpc!;
        }
    }

    public GrpcTestFixture()
    {
        LoggerFactory = new LoggerFactory();
        LoggerFactory.AddProvider(new ForwardingLoggerProvider((logLevel, category, eventId, message, exception) =>
        {
            LoggedMessage?.Invoke(logLevel, category, eventId, message, exception);
        }));
    }

    public void ConfigureWebHost(Action<IWebHostBuilder> configure)
    {
        _configureWebHost = configure;
    }

    private void EnsureServer()
    {
        if (_host == null)
        {
            var builder = new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ILoggerFactory>(LoggerFactory);
                    // Registers a service for tests to add new methods
                    services.AddSingleton<DynamicGrpcServiceRegistry>();
                })
                .ConfigureWebHostDefaults(webHost =>
                {
                    webHost
                        .UseTestServer()
                        .UseStartup<TStartup>();

                    _configureWebHost?.Invoke(webHost);
                });
            _host = builder.Start();
            _server = _host.GetTestServer();
            _handler = _server.CreateHandler();
            _dynamicGrpc = _server.Services.GetRequiredService<DynamicGrpcServiceRegistry>();
        }
    }

    public LoggerFactory LoggerFactory { get; }

    public HttpMessageHandler Handler
    {
        get
        {
            EnsureServer();
            return _handler!;
        }
    }

    public void Dispose()
    {
        _handler?.Dispose();
        _host?.Dispose();
        _server?.Dispose();
    }

    public IDisposable GetTestContext(ITestOutputHelper outputHelper)
    {
        return new GrpcTestContext<TStartup>(this, outputHelper);
    }
}
