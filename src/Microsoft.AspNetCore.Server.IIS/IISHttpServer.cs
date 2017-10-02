// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Server.IIS
{
    public class IISHttpServer : IServer
    {
        private static NativeMethods.PFN_REQUEST_HANDLER _requestHandler = HandleRequest;
        private static NativeMethods.PFN_SHUTDOWN_HANDLER _shutdownHandler = HandleShutdown;

        private IISContextFactory _iisContextFactory;

        private PipeFactory _pipeFactory = new PipeFactory();
        private GCHandle _httpServerHandle;
        private IApplicationLifetime _applicationLifetime;

        public IFeatureCollection Features { get; } = new FeatureCollection();
        public IISHttpServer(IApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            _httpServerHandle = GCHandle.Alloc(this);

            _iisContextFactory = new IISContextFactory<TContext>(_pipeFactory, application);

            // Start the server by registering the callback
            // TODO the context may change here for shutdown.
            NativeMethods.register_callbacks(_requestHandler, _shutdownHandler, (IntPtr)_httpServerHandle, (IntPtr)_httpServerHandle);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // TODO: Drain pending requests

            // Stop all further calls back into managed code by unhooking the callback

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_httpServerHandle.IsAllocated)
            {
                _httpServerHandle.Free();
            }

            _pipeFactory.Dispose();
        }

        private static NativeMethods.REQUEST_NOTIFICATION_STATUS HandleRequest(IntPtr pHttpContext, IntPtr pvRequestContext)
        {
            // Unwrap the server so we can create an http context and process the request
            var server = (IISHttpServer)GCHandle.FromIntPtr(pvRequestContext).Target;

            var context = server._iisContextFactory.CreateHttpContext(pHttpContext);

            var task = context.ProcessRequestAsync();

            // This should never fail
            if (task.IsCompleted)
            {
                context.Dispose();
                return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_CONTINUE;
            }

            task.ContinueWith((t, state) => CompleteRequest((HttpProtocol)state), context);

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        }

        private static bool HandleShutdown(IntPtr pvRequestContext)
        {
            var server = (IISHttpServer)GCHandle.FromIntPtr(pvRequestContext).Target;
            server._applicationLifetime.StopApplication();
            return true;
        }

        private static void CompleteRequest(HttpProtocol context)
        {
            // Post completion after completing the request to resume the state machine
            context.PostCompletion();

            // Dispose the context
            context.Dispose();
        }

        private class IISContextFactory<T> : IISContextFactory
        {
            private readonly IHttpApplication<T> _application;
            private readonly PipeFactory _pipeFactory;

            public IISContextFactory(PipeFactory pipeFactory, IHttpApplication<T> application)
            {
                _application = application;
                _pipeFactory = pipeFactory;
            }

            public HttpProtocol CreateHttpContext(IntPtr pHttpContext)
            {
                return new IISHttpContextOfT<T>(_pipeFactory, _application, pHttpContext);
            }
        }
    }

    // Over engineering to avoid allocations...
    internal interface IISContextFactory
    {
        HttpProtocol CreateHttpContext(IntPtr pHttpContext);
    }

    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseNativeIIS(this IWebHostBuilder builder)
        {
            if (NativeMethods.is_ancm_loaded())
            {
                // TODO put this in options and use path.
                var path = NativeMethods.http_get_application_full_path();
                builder.UseContentRoot(path);
                return builder.ConfigureServices(services =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        services.AddSingleton<IServer, IISHttpServer>();
                    }
                });
            }
            return builder;
        }
    }
}
