// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Context = Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context;

namespace Microsoft.AspNetCore.TestHost
{
    public class TestServer : IServer
    {
        private IWebHost _hostInstance;
        private bool _disposed = false;
        private IHttpApplication<Context> _application;

        /// <summary>
        /// For use with IHostBuilder or IWebHostBuilder.
        /// </summary>
        public TestServer()
            : this(new FeatureCollection())
        {
        }

        /// <summary>
        /// For use with IHostBuilder or IWebHostBuilder.
        /// </summary>
        /// <param name="featureCollection"></param>
        public TestServer(IFeatureCollection featureCollection)
        {
            Features = featureCollection ?? throw new ArgumentNullException(nameof(featureCollection));
        }

        /// <summary>
        /// For use with IWebHostBuilder.
        /// </summary>
        /// <param name="builder"></param>
        public TestServer(IWebHostBuilder builder)
            : this(builder, new FeatureCollection())
        {
        }

        /// <summary>
        /// For use with IWebHostBuilder.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="featureCollection"></param>
        public TestServer(IWebHostBuilder builder, IFeatureCollection featureCollection)
            : this(featureCollection)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var host = builder.UseServer(this).Build();
            host.StartAsync().GetAwaiter().GetResult();
            _hostInstance = host;
        }

        public Uri BaseAddress { get; set; } = new Uri("http://localhost/");

        public IWebHost Host
        {
            get
            {
                return _hostInstance
                    ?? throw new InvalidOperationException("The TestServer constructor was not called with a IWebHostBuilder so IWebHost is not available.");
            }
        }

        public IFeatureCollection Features { get; }

        private IHttpApplication<Context> Application
        {
            get => _application ?? throw new InvalidOperationException("The server has not been started or no web application was configured.");
        }

        public HttpMessageHandler CreateHandler()
        {
            var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
            return new ClientHandler(pathBase, Application);
        }

        public HttpClient CreateClient()
        {
            return new HttpClient(CreateHandler()) { BaseAddress = BaseAddress };
        }

        public WebSocketClient CreateWebSocketClient()
        {
            var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
            return new WebSocketClient(pathBase, Application);
        }

        /// <summary>
        /// Begins constructing a request message for submission.
        /// </summary>
        /// <param name="path"></param>
        /// <returns><see cref="RequestBuilder"/> to use in constructing additional request details.</returns>
        public RequestBuilder CreateRequest(string path)
        {
            return new RequestBuilder(this, path);
        }

        /// <summary>
        /// Creates, configures, sends, and returns a <see cref="HttpContext"/>. This completes as soon as the response is started.
        /// </summary>
        /// <returns></returns>
        public async Task<HttpContext> SendAsync(Action<HttpContext> configureContext, CancellationToken cancellationToken = default)
        {
            if (configureContext == null)
            {
                throw new ArgumentNullException(nameof(configureContext));
            }

            var builder = new HttpContextBuilder(Application);
            builder.Configure(context =>
            {
                var request = context.Request;
                request.Scheme = BaseAddress.Scheme;
                request.Host = HostString.FromUriComponent(BaseAddress);
                if (BaseAddress.IsDefaultPort)
                {
                    request.Host = new HostString(request.Host.Host);
                }
                var pathBase = PathString.FromUriComponent(BaseAddress);
                if (pathBase.HasValue && pathBase.Value.EndsWith("/"))
                {
                    pathBase = new PathString(pathBase.Value.Substring(0, pathBase.Value.Length - 1));
                }
                request.PathBase = pathBase;
            });
            builder.Configure(configureContext);
            return await builder.SendAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _hostInstance.Dispose();
            }
        }

        Task IServer.StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            _application = new ApplicationWrapper<Context>((IHttpApplication<Context>)application, () =>
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            });

            return Task.CompletedTask;
        }

        Task IServer.StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private class ApplicationWrapper<TContext> : IHttpApplication<TContext>
        {
            private readonly IHttpApplication<TContext> _application;
            private readonly Action _preProcessRequestAsync;

            public ApplicationWrapper(IHttpApplication<TContext> application, Action preProcessRequestAsync)
            {
                _application = application;
                _preProcessRequestAsync = preProcessRequestAsync;
            }

            public TContext CreateContext(IFeatureCollection contextFeatures)
            {
                return _application.CreateContext(contextFeatures);
            }

            public void DisposeContext(TContext context, Exception exception)
            {
                _application.DisposeContext(context, exception);
            }

            public Task ProcessRequestAsync(TContext context)
            {
                _preProcessRequestAsync();
                return _application.ProcessRequestAsync(context);
            }
        }
    }
}
