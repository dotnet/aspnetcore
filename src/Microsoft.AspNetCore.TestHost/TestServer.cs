// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Context = Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context;

namespace Microsoft.AspNetCore.TestHost
{
    public class TestServer : IServer
    {
        private const string DefaultEnvironmentName = "Development";
        private const string ServerName = nameof(TestServer);
        private IWebHost _hostInstance;
        private bool _disposed = false;
        private IHttpApplication<Context> _application;

        public TestServer(IWebHostBuilder builder)
            : this(builder, new FeatureCollection())
        {
        }

        public TestServer(IWebHostBuilder builder, IFeatureCollection featureCollection)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (featureCollection == null)
            {
                throw new ArgumentNullException(nameof(featureCollection));
            }        
        
            Features = featureCollection;

            var host = builder.UseServer(this).Build();
            host.Start();
            _hostInstance = host;
        }

        public Uri BaseAddress { get; set; } = new Uri("http://localhost/");

        public IWebHost Host
        {
            get
            {
                return _hostInstance;
            }
        }

        public IFeatureCollection Features { get; }

        public HttpMessageHandler CreateHandler()
        {
            var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
            return new ClientHandler(pathBase, _application);
        }

        public HttpClient CreateClient()
        {
            return new HttpClient(CreateHandler()) { BaseAddress = BaseAddress };
        }

        public WebSocketClient CreateWebSocketClient()
        {
            var pathBase = BaseAddress == null ? PathString.Empty : PathString.FromUriComponent(BaseAddress);
            return new WebSocketClient(pathBase, _application);
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _hostInstance.Dispose();
            }
        }

        void IServer.Start<TContext>(IHttpApplication<TContext> application)
        {
            _application = new ApplicationWrapper<Context>((IHttpApplication<Context>)application, () =>
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            });
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
