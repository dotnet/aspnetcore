// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

// Measures the per-request overhead of the hosting layer (context creation, diagnostics and disposal)
// as seen by a server that pools the hosting context, like Kestrel.
public class HostingApplicationBenchmark
{
    private IHost _host;
    private BenchmarkServer _server;
    private MeterListener _meterListener;

    [Params(false, true)]
    public bool MetricsEnabled { get; set; }

    [Params(false, true)]
    public bool EndpointMatched { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        if (MetricsEnabled)
        {
            _meterListener = new MeterListener();
            _meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Microsoft.AspNetCore.Hosting")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _meterListener.SetMeasurementEventCallback<long>(static (_, _, _, _) => { });
            _meterListener.SetMeasurementEventCallback<double>(static (_, _, _, _) => { });
            _meterListener.Start();
        }

        _server = new BenchmarkServer();
        _host = new HostBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureWebHost(webHost =>
            {
                webHost.ConfigureServices(services => services.AddSingleton<IServer>(_server));
                webHost.Configure(app =>
                {
                    if (EndpointMatched)
                    {
                        // Simulate routing having selected an endpoint for the request.
                        var endpoint = new Endpoint(static context => Task.CompletedTask, EndpointMetadataCollection.Empty, "Endpoint");
                        app.Use((context, next) =>
                        {
                            context.SetEndpoint(endpoint);
                            return next(context);
                        });
                    }

                    app.Run(static context => Task.CompletedTask);
                });
            })
            .Build();
        _host.Start();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _host.Dispose();
        _meterListener?.Dispose();
    }

    [Benchmark]
    public Task ProcessRequest() => _server.ProcessRequestAsync();

    private sealed class BenchmarkServer : IServer
    {
        private IRequestRunner _runner;

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public Task ProcessRequestAsync() => _runner.ProcessRequestAsync();

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
        {
            _runner = new RequestRunner<TContext>(application);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose()
        {
        }
    }

    private interface IRequestRunner
    {
        Task ProcessRequestAsync();
    }

    private sealed class RequestRunner<TContext> : IRequestRunner where TContext : notnull
    {
        private readonly IHttpApplication<TContext> _application;
        private readonly RequestFeatures<TContext> _features = new();

        public RequestRunner(IHttpApplication<TContext> application)
        {
            _application = application;
        }

        public async Task ProcessRequestAsync()
        {
            // Like Kestrel, reuse the same feature collection (and pooled hosting context) for every request.
            _features.Reset();
            var context = _application.CreateContext(_features);
            try
            {
                await _application.ProcessRequestAsync(context);
                _application.DisposeContext(context, null);
            }
            catch (Exception ex)
            {
                _application.DisposeContext(context, ex);
                throw;
            }
        }
    }

    private sealed class RequestFeatures<TContext> : IFeatureCollection, IHostContextContainer<TContext> where TContext : notnull
    {
        private readonly HttpRequestFeature _request = new() { Method = "GET", Protocol = "HTTP/1.1", Scheme = "http", Path = "/" };
        private readonly HttpResponseFeature _response = new();
        private readonly StreamResponseBodyFeature _responseBody = new(Stream.Null);
        private readonly Dictionary<Type, object> _perRequest = new();
        private int _revision;

        public TContext HostContext { get; set; }

        public bool IsReadOnly => false;

        public int Revision => _revision;

        public void Reset()
        {
            // Features set during the request (e.g. by hosting) must not survive to the next request.
            _response.StatusCode = StatusCodes.Status200OK;
            _response.Headers.Clear();
            _perRequest.Clear();
            _revision++;
        }

        public object this[Type key]
        {
            get => key == typeof(IHttpRequestFeature) ? _request
                : key == typeof(IHttpResponseFeature) ? _response
                : key == typeof(IHttpResponseBodyFeature) ? _responseBody
                : _perRequest.GetValueOrDefault(key);
            set
            {
                if (value is null)
                {
                    _perRequest.Remove(key);
                }
                else
                {
                    _perRequest[key] = value;
                }
                _revision++;
            }
        }

        public TFeature Get<TFeature>() => (TFeature)this[typeof(TFeature)];

        public void Set<TFeature>(TFeature instance) => this[typeof(TFeature)] = instance;

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => _perRequest.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
