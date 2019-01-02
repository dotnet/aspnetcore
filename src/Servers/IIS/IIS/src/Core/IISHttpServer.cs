
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal class IISHttpServer : IServer
    {
        private const string WebSocketVersionString = "WEBSOCKET_VERSION";

        private static readonly NativeMethods.PFN_REQUEST_HANDLER _requestHandler = HandleRequest;
        private static readonly NativeMethods.PFN_SHUTDOWN_HANDLER _shutdownHandler = HandleShutdown;
        private static readonly NativeMethods.PFN_DISCONNECT_HANDLER _onDisconnect = OnDisconnect;
        private static readonly NativeMethods.PFN_ASYNC_COMPLETION _onAsyncCompletion = OnAsyncCompletion;

        private IISContextFactory _iisContextFactory;
        private readonly MemoryPool<byte> _memoryPool = new SlabMemoryPool();
        private GCHandle _httpServerHandle;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ILogger<IISHttpServer> _logger;
        private readonly IISServerOptions _options;
        private readonly IISNativeApplication _nativeApplication;
        private readonly ServerAddressesFeature _serverAddressesFeature;

        private volatile int _stopping;
        private bool Stopping => _stopping == 1;
        private int _outstandingRequests;
        private readonly TaskCompletionSource<object> _shutdownSignal = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool? _websocketAvailable;

        public IFeatureCollection Features { get; } = new FeatureCollection();

        // TODO: Remove pInProcessHandler argument
        public bool IsWebSocketAvailable(IntPtr pInProcessHandler)
        {
            // Check if the Http upgrade feature is available in IIS.
            // To check this, we can look at the server variable WEBSOCKET_VERSION
            // And see if there is a version. Same check that Katana did:
            // https://github.com/aspnet/AspNetKatana/blob/9f6e09af6bf203744feb5347121fe25f6eec06d8/src/Microsoft.Owin.Host.SystemWeb/OwinAppContext.cs#L125
            // Actively not locking here as acquiring a lock on every request will hurt perf more than checking the
            // server variables a few extra times if a bunch of requests hit the server at the same time.
            if (!_websocketAvailable.HasValue)
            {
                _websocketAvailable = NativeMethods.HttpTryGetServerVariable(pInProcessHandler, WebSocketVersionString, out var webSocketsSupported)
                    && !string.IsNullOrEmpty(webSocketsSupported);
            }

            return _websocketAvailable.Value;
        }

        public IISHttpServer(
            IISNativeApplication nativeApplication,
            IApplicationLifetime applicationLifetime,
            IAuthenticationSchemeProvider authentication,
            IOptions<IISServerOptions> options,
            ILogger<IISHttpServer> logger
            )
        {
            _nativeApplication = nativeApplication;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
            _options = options.Value;
            _serverAddressesFeature = new ServerAddressesFeature();

            if (_options.ForwardWindowsAuthentication)
            {
                authentication.AddScheme(new AuthenticationScheme(IISServerDefaults.AuthenticationScheme, _options.AuthenticationDisplayName, typeof(IISServerAuthenticationHandler)));
            }

            Features.Set<IServerAddressesFeature>(_serverAddressesFeature);
        }

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            _httpServerHandle = GCHandle.Alloc(this);

            _iisContextFactory = new IISContextFactory<TContext>(_memoryPool, application, _options, this, _logger);
            _nativeApplication.RegisterCallbacks(_requestHandler, _shutdownHandler, _onDisconnect, _onAsyncCompletion, (IntPtr)_httpServerHandle, (IntPtr)_httpServerHandle);

            _serverAddressesFeature.Addresses = _options.ServerAddresses;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            void RegisterCancelation()
            {
                cancellationToken.Register(() =>
                {
                    _nativeApplication.StopCallsIntoManaged();
                    _shutdownSignal.TrySetResult(null);
                });
            }
            if (Interlocked.Exchange(ref _stopping, 1) == 1)
            {
                RegisterCancelation();

                return _shutdownSignal.Task;
            }

            // First call back into native saying "DON'T SEND ME ANY MORE REQUESTS"
            _nativeApplication.StopIncomingRequests();

            try
            {
                // Wait for active requests to drain
                if (_outstandingRequests > 0)
                {
                    RegisterCancelation();
                }
                else
                {
                    // We have drained all requests. Block any callbacks into managed at this point.
                    _nativeApplication.StopCallsIntoManaged();
                    _shutdownSignal.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                _shutdownSignal.TrySetException(ex);
            }

            return _shutdownSignal.Task;
        }

        public void Dispose()
        {
            _stopping = 1;

            // Block any more calls into managed from native as we are unloading.
            _nativeApplication.StopCallsIntoManaged();
            _shutdownSignal.TrySetResult(null);

            if (_httpServerHandle.IsAllocated)
            {
                _httpServerHandle.Free();
            }

            _memoryPool.Dispose();
            _nativeApplication.Dispose();
        }

        private void IncrementRequests()
        {
            Interlocked.Increment(ref _outstandingRequests);
        }

        internal void DecrementRequests()
        {
            if (Interlocked.Decrement(ref _outstandingRequests) == 0 && Stopping)
            {
                // All requests have been drained.
                _nativeApplication.StopCallsIntoManaged();
                _shutdownSignal.TrySetResult(null);
            }
        }

        private static NativeMethods.REQUEST_NOTIFICATION_STATUS HandleRequest(IntPtr pInProcessHandler, IntPtr pvRequestContext)
        {
            IISHttpServer server = null;
            try
            {
                // Unwrap the server so we can create an http context and process the request
                server = (IISHttpServer)GCHandle.FromIntPtr(pvRequestContext).Target;
                server.IncrementRequests();

                var context = server._iisContextFactory.CreateHttpContext(pInProcessHandler);

                ThreadPool.UnsafeQueueUserWorkItem(context, preferLocal: false);

                return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
            }
            catch (Exception ex)
            {
                server?._logger.LogError(0, ex, $"Unexpected exception in static {nameof(IISHttpServer)}.{nameof(HandleRequest)}.");

                return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_FINISH_REQUEST;
            }
        }

        private static bool HandleShutdown(IntPtr pvRequestContext)
        {
            IISHttpServer server = null;
            try
            {
                server = (IISHttpServer)GCHandle.FromIntPtr(pvRequestContext).Target;
                server._applicationLifetime.StopApplication();
            }
            catch (Exception ex)
            {
                server?._logger.LogError(0, ex, $"Unexpected exception in {nameof(IISHttpServer)}.{nameof(HandleShutdown)}.");
            }
            return true;
        }


        private static void OnDisconnect(IntPtr pvManagedHttpContext)
        {
            IISHttpContext context = null;
            try
            {
                context = (IISHttpContext)GCHandle.FromIntPtr(pvManagedHttpContext).Target;
                context.ConnectionReset();
            }
            catch (Exception ex)
            {
                context?.Server._logger.LogError(0, ex, $"Unexpected exception in {nameof(IISHttpServer)}.{nameof(OnDisconnect)}.");
            }
        }

        private static NativeMethods.REQUEST_NOTIFICATION_STATUS OnAsyncCompletion(IntPtr pvManagedHttpContext, int hr, int bytes)
        {
            IISHttpContext context = null;
            try
            {
                context = (IISHttpContext)GCHandle.FromIntPtr(pvManagedHttpContext).Target;
                context.OnAsyncCompletion(hr, bytes);
                return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
            }
            catch (Exception ex)
            {
                context?.Server._logger.LogError(0, ex, $"Unexpected exception in {nameof(IISHttpServer)}.{nameof(OnAsyncCompletion)}.");

                return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_FINISH_REQUEST;
            }
        }

        private class IISContextFactory<T> : IISContextFactory
        {
            private readonly IHttpApplication<T> _application;
            private readonly MemoryPool<byte> _memoryPool;
            private readonly IISServerOptions _options;
            private readonly IISHttpServer _server;
            private readonly ILogger _logger;

            public IISContextFactory(MemoryPool<byte> memoryPool, IHttpApplication<T> application, IISServerOptions options, IISHttpServer server, ILogger logger)
            {
                _application = application;
                _memoryPool = memoryPool;
                _options = options;
                _server = server;
                _logger = logger;
            }

            public IISHttpContext CreateHttpContext(IntPtr pInProcessHandler)
            {
                return new IISHttpContextOfT<T>(_memoryPool, _application, pInProcessHandler, _options, _server, _logger);
            }
        }
    }

    // Over engineering to avoid allocations...
    internal interface IISContextFactory
    {
        IISHttpContext CreateHttpContext(IntPtr pInProcessHandler);
    }
}
