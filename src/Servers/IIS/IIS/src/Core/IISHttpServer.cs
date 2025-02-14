// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class IISHttpServer : IServer
{
    private const string WebSocketVersionString = "WEBSOCKET_VERSION";

    private IISContextFactory? _iisContextFactory;
    private readonly MemoryPool<byte> _memoryPool = new PinnedBlockMemoryPool();
    private GCHandle _httpServerHandle;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<IISHttpServer> _logger;
    private readonly IISServerOptions _options;
    private readonly IISNativeApplication _nativeApplication;
    private readonly ServerAddressesFeature _serverAddressesFeature;
    private readonly string? _virtualPath;

    private readonly TaskCompletionSource _shutdownSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool? _websocketAvailable;
    private CancellationTokenRegistration _cancellationTokenRegistration;
    private bool _disposed;

    public IFeatureCollection Features { get; } = new FeatureCollection();

    // TODO: Remove pInProcessHandler argument
    public bool IsWebSocketAvailable(NativeSafeHandle pInProcessHandler)
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
        IHostApplicationLifetime applicationLifetime,
        IAuthenticationSchemeProvider authentication,
        IConfiguration configuration,
        IOptions<IISServerOptions> options,
        ILogger<IISHttpServer> logger
        )
    {
        _nativeApplication = nativeApplication;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _options = options.Value;
        _serverAddressesFeature = new ServerAddressesFeature();
        var iisConfigData = NativeMethods.HttpGetApplicationProperties();
        _virtualPath = iisConfigData.pwzVirtualApplicationPath;

        if (_options.ForwardWindowsAuthentication)
        {
            authentication.AddScheme(new AuthenticationScheme(IISServerDefaults.AuthenticationScheme, _options.AuthenticationDisplayName, typeof(IISServerAuthenticationHandlerInternal)));
        }

        Features.Set<IServerAddressesFeature>(_serverAddressesFeature);

        if (IISEnvironmentFeature.TryCreate(configuration, out var iisEnvFeature))
        {
            Features.Set<IIISEnvironmentFeature>(iisEnvFeature);
        }

        if (_options.MaxRequestBodySize > _options.IisMaxRequestSizeLimit)
        {
            _logger.LogWarning(CoreStrings.MaxRequestLimitWarning);
        }
    }

    public string? VirtualPath => _virtualPath;

    public unsafe Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        _httpServerHandle = GCHandle.Alloc(this);

        _iisContextFactory = new IISContextFactory<TContext>(_memoryPool, application, _options, this, _logger);
        _nativeApplication.RegisterCallbacks(
            &HandleRequest,
            &HandleShutdown,
            &OnDisconnect,
            &OnAsyncCompletion,
            &OnRequestsDrained,
            (IntPtr)_httpServerHandle,
            (IntPtr)_httpServerHandle);

        _serverAddressesFeature.Addresses = _options.ServerAddresses;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _nativeApplication.StopIncomingRequests();
        _cancellationTokenRegistration = cancellationToken.Register((shutdownSignal) =>
        {
            ((TaskCompletionSource)shutdownSignal!).TrySetResult();
        },
        _shutdownSignal);

        return _shutdownSignal.Task;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        // Block any more calls into managed from native as we are unloading.
        _nativeApplication.Stop();
        _shutdownSignal.TrySetResult();

        if (_httpServerHandle.IsAllocated)
        {
            _httpServerHandle.Free();
        }

        _memoryPool.Dispose();
    }

    [UnmanagedCallersOnly]
    private static NativeMethods.REQUEST_NOTIFICATION_STATUS HandleRequest(IntPtr pInProcessHandler, IntPtr pvRequestContext)
    {
        IISHttpServer? server = null;
        try
        {
            // Unwrap the server so we can create an http context and process the request
            server = (IISHttpServer?)GCHandle.FromIntPtr(pvRequestContext).Target;

            // server can be null if ungraceful shutdown.
            if (server == null)
            {
                return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_FINISH_REQUEST;
            }

            var safehandle = new NativeSafeHandle(pInProcessHandler);

            Debug.Assert(server._iisContextFactory != null, "StartAsync must be called first.");
            var context = server._iisContextFactory.CreateHttpContext(safehandle);

            ThreadPool.UnsafeQueueUserWorkItem(context, preferLocal: false);

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        }
        catch (Exception ex)
        {
            server?._logger.LogError(0, ex, $"Unexpected exception in static {nameof(IISHttpServer)}.{nameof(HandleRequest)}.");

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_FINISH_REQUEST;
        }
    }

    [UnmanagedCallersOnly]
    private static int HandleShutdown(IntPtr pvRequestContext)
    {
        IISHttpServer? server = null;
        try
        {
            server = (IISHttpServer?)GCHandle.FromIntPtr(pvRequestContext).Target;

            // server can be null if ungraceful shutdown.
            if (server == null)
            {
                // return value isn't checked.
                return 1;
            }

            server._applicationLifetime.StopApplication();
        }
        catch (Exception ex)
        {
            server?._logger.LogError(0, ex, $"Unexpected exception in {nameof(IISHttpServer)}.{nameof(HandleShutdown)}.");
        }
        return 1;
    }

    [UnmanagedCallersOnly]
    private static void OnDisconnect(IntPtr pvManagedHttpContext)
    {
        IISHttpContext? context = null;
        try
        {
            context = (IISHttpContext?)GCHandle.FromIntPtr(pvManagedHttpContext).Target;

            // Context can be null if ungraceful shutdown.
            if (context == null)
            {
                return;
            }

            context.AbortIO(clientDisconnect: true);
        }
        catch (Exception ex)
        {
            context?.Server._logger.LogError(0, ex, $"Unexpected exception in {nameof(IISHttpServer)}.{nameof(OnDisconnect)}.");
        }
    }

    [UnmanagedCallersOnly]
    private static NativeMethods.REQUEST_NOTIFICATION_STATUS OnAsyncCompletion(IntPtr pvManagedHttpContext, int hr, int bytes)
    {
        IISHttpContext? context = null;
        try
        {
            context = (IISHttpContext?)GCHandle.FromIntPtr(pvManagedHttpContext).Target;

            // Context can be null if ungraceful shutdown.
            if (context == null)
            {
                return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_FINISH_REQUEST;
            }

            context.OnAsyncCompletion(hr, bytes);
            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING;
        }
        catch (Exception ex)
        {
            context?.Server._logger.LogError(0, ex, $"Unexpected exception in {nameof(IISHttpServer)}.{nameof(OnAsyncCompletion)}.");

            return NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_FINISH_REQUEST;
        }
    }

    [UnmanagedCallersOnly]
    private static void OnRequestsDrained(IntPtr serverContext)
    {
        IISHttpServer? server = null;
        try
        {
            server = (IISHttpServer?)GCHandle.FromIntPtr(serverContext).Target;

            // server can be null if ungraceful shutdown.
            if (server == null)
            {
                return;
            }

            server._nativeApplication.Stop();
            server._shutdownSignal.TrySetResult();
            server._cancellationTokenRegistration.Dispose();
        }
        catch (Exception ex)
        {
            server?._logger.LogError(0, ex, $"Unexpected exception in {nameof(IISHttpServer)}.{nameof(OnRequestsDrained)}.");
        }
    }

    private sealed class IISContextFactory<T> : IISContextFactory where T : notnull
    {
        private const string Latin1Suppport = "Microsoft.AspNetCore.Server.IIS.Latin1RequestHeaders";

        private readonly IHttpApplication<T> _application;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly IISServerOptions _options;
        private readonly IISHttpServer _server;
        private readonly ILogger _logger;
        private readonly bool _useLatin1;

        public IISContextFactory(MemoryPool<byte> memoryPool, IHttpApplication<T> application, IISServerOptions options, IISHttpServer server, ILogger logger)
        {
            _application = application;
            _memoryPool = memoryPool;
            _options = options;
            _server = server;
            _logger = logger;
            AppContext.TryGetSwitch(Latin1Suppport, out _useLatin1);
        }

        public IISHttpContext CreateHttpContext(NativeSafeHandle pInProcessHandler)
        {
            return new IISHttpContextOfT<T>(_memoryPool, _application, pInProcessHandler, _options, _server, _logger, _useLatin1);
        }
    }
}

// Over engineering to avoid allocations...
internal interface IISContextFactory
{
    IISHttpContext CreateHttpContext(NativeSafeHandle pInProcessHandler);
}
