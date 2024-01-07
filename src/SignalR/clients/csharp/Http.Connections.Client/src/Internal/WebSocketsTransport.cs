// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static System.IO.Pipelines.DuplexPipe;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

#pragma warning disable CA2252 // This API requires opting into preview features
internal sealed partial class WebSocketsTransport : ITransport, IStatefulReconnectFeature
#pragma warning restore CA2252 // This API requires opting into preview features
{
    private WebSocket? _webSocket;
    private IDuplexPipe? _application;
    private WebSocketMessageType _webSocketMessageType;
    private readonly ILogger _logger;
    private readonly TimeSpan _closeTimeout;
    private volatile bool _aborted;
    private readonly HttpConnectionOptions _httpConnectionOptions;
    private readonly HttpClient? _httpClient;
    private CancellationTokenSource _stopCts = default!;
    private bool _useStatefulReconnect;

    private IDuplexPipe? _transport;
    // Used for reconnect (when enabled) to determine if the close was ungraceful or not, reconnect only happens on ungraceful disconnect
    // The assumption is that a graceful close was triggered purposefully by either the client or server and a reconnect shouldn't occur 
    private bool _gracefulClose;
    private Func<PipeWriter, Task>? _notifyOnReconnect;

    internal Task Running { get; private set; } = Task.CompletedTask;

    public PipeReader Input => _transport!.Input;

    public PipeWriter Output => _transport!.Output;

#pragma warning disable CA2252 // This API requires opting into preview features
    public void OnReconnected(Func<PipeWriter, Task> notifyOnReconnect)
    {
        if (_notifyOnReconnect is null)
        {
            _notifyOnReconnect = notifyOnReconnect;
        }
        else
        {
            var localNotifyOnReconnect = _notifyOnReconnect;
            _notifyOnReconnect = async writer =>
            {
                await localNotifyOnReconnect(writer).ConfigureAwait(false);
                await notifyOnReconnect(writer).ConfigureAwait(false);
            };
        }
    }
#pragma warning restore CA2252 // This API requires opting into preview features

    public WebSocketsTransport(HttpConnectionOptions httpConnectionOptions, ILoggerFactory loggerFactory, Func<Task<string?>> accessTokenProvider, HttpClient? httpClient,
        bool useStatefulReconnect = false)
    {
        _useStatefulReconnect = useStatefulReconnect;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(typeof(WebSocketsTransport));
        _httpConnectionOptions = httpConnectionOptions ?? new HttpConnectionOptions();

        _closeTimeout = _httpConnectionOptions.CloseTimeout;

        // We were given an updated delegate from the HttpConnection and we are updating what we have in httpOptions
        // options itself is copied object of user's options
        _httpConnectionOptions.AccessTokenProvider = accessTokenProvider;

        _httpClient = httpClient;
    }

    private async ValueTask<WebSocket> DefaultWebSocketFactory(WebSocketConnectionContext context, CancellationToken cancellationToken)
    {
        var webSocket = new ClientWebSocket();
        var url = context.Uri;

        var isBrowser = OperatingSystem.IsBrowser();
        if (!isBrowser)
        {
            // Full Framework will throw when trying to set the User-Agent header
            // So avoid setting it in netstandard2.0 and only set it in netstandard2.1 and higher
#if !NETSTANDARD2_0 && !NETFRAMEWORK
            webSocket.Options.SetRequestHeader("User-Agent", Constants.UserAgentHeader.ToString());
#else
            // Set an alternative user agent header on Full framework
            webSocket.Options.SetRequestHeader("X-SignalR-User-Agent", Constants.UserAgentHeader.ToString());
#endif

            // Set this header so the server auth middleware will set an Unauthorized instead of Redirect status code
            // See: https://github.com/aspnet/Security/blob/ff9f145a8e89c9756ea12ff10c6d47f2f7eb345f/src/Microsoft.AspNetCore.Authentication.Cookies/Events/CookieAuthenticationEvents.cs#L42
            webSocket.Options.SetRequestHeader("X-Requested-With", "XMLHttpRequest");
        }

        if (context.Options != null)
        {
            if (context.Options.Headers.Count > 0)
            {
                if (isBrowser)
                {
                    Log.HeadersNotSupported(_logger);
                }
                else
                {
                    foreach (var header in context.Options.Headers)
                    {
                        webSocket.Options.SetRequestHeader(header.Key, header.Value);
                    }
                }
            }

#if NET7_0_OR_GREATER
            var allowHttp2 = true;
#endif

            if (!isBrowser)
            {
                if (context.Options.Cookies != null)
                {
                    webSocket.Options.Cookies = context.Options.Cookies;
                }

                if (context.Options.ClientCertificates is { Count: > 0 })
                {
                    webSocket.Options.ClientCertificates.AddRange(context.Options.ClientCertificates);
                }

                if (context.Options.Credentials != null)
                {
                    webSocket.Options.Credentials = context.Options.Credentials;
                    // Negotiate Auth isn't supported over HTTP/2 and HttpClient does not gracefully fallback to HTTP/1.1 in that case
                    // https://github.com/dotnet/runtime/issues/1582
#if NET7_0_OR_GREATER
                    allowHttp2 = false;
#endif
                }

                var originalProxy = webSocket.Options.Proxy;
                if (context.Options.Proxy != null)
                {
                    webSocket.Options.Proxy = context.Options.Proxy;
                }

                if (context.Options.UseDefaultCredentials != null)
                {
                    webSocket.Options.UseDefaultCredentials = context.Options.UseDefaultCredentials.Value;
                    if (context.Options.UseDefaultCredentials.Value)
                    {
                        // Negotiate Auth isn't supported over HTTP/2 and HttpClient does not gracefully fallback to HTTP/1.1 in that case
                        // https://github.com/dotnet/runtime/issues/1582
#if NET7_0_OR_GREATER
                        allowHttp2 = false;
#endif
                    }
                }

                context.Options.WebSocketConfiguration?.Invoke(webSocket.Options);

#if NET7_0_OR_GREATER
                if (webSocket.Options.HttpVersion >= HttpVersion.Version20 && allowHttp2)
                {
                    // Reset options we set on the users' behalf since they are already on the HttpClient that we're passing to ConnectAsync
                    // And ConnectAsync will throw if these options are set on the ClientWebSocketOptions
                    if (ReferenceEquals(webSocket.Options.Cookies, context.Options.Cookies))
                    {
                        webSocket.Options.Cookies = null;
                    }
                    if (IsX509CertificateCollectionEqual(webSocket.Options.ClientCertificates, context.Options.ClientCertificates))
                    {
                        webSocket.Options.ClientCertificates.Clear();
                    }
                    if (ReferenceEquals(webSocket.Options.Credentials, context.Options.Credentials))
                    {
                        webSocket.Options.Credentials = null;
                    }
                    if (webSocket.Options.UseDefaultCredentials == (context.Options.UseDefaultCredentials ?? false))
                    {
                        webSocket.Options.UseDefaultCredentials = false;
                    }
                    if (ReferenceEquals(webSocket.Options.Proxy, context.Options.Proxy))
                    {
                        webSocket.Options.Proxy = originalProxy;
                    }
                }

                if (!allowHttp2 && webSocket.Options.HttpVersion >= HttpVersion.Version20)
                {
                    // We shouldn't fallback to HTTP/1.1 if the user explicitly states
                    if (webSocket.Options.HttpVersionPolicy == HttpVersionPolicy.RequestVersionOrLower)
                    {
                        webSocket.Options.HttpVersion = HttpVersion.Version11;
                    }
                    else
                    {
                        throw new InvalidOperationException("Negotiate Authentication doesn't work with HTTP/2 or higher.");
                    }
                }

                static bool IsX509CertificateCollectionEqual(X509CertificateCollection? left, X509CertificateCollection? right)
                {
                    var leftCount = left?.Count ?? 0;
                    var rightCount = right?.Count ?? 0;
                    if (leftCount == rightCount)
                    {
                        for (var i = 0; i < rightCount; ++i)
                        {
                            if (!ReferenceEquals(left![i], right![i]))
                            {
                                return false;
                            }
                        }
                        return true;
                    }

                    return false;
                }
#endif
            }
        }

        if (_httpConnectionOptions.AccessTokenProvider != null
#if NET7_0_OR_GREATER
            && webSocket.Options.HttpVersion < HttpVersion.Version20
#endif
            )
        {
            // Apply access token logic when using HTTP/1.1 because we don't use the AccessTokenHttpMessageHandler via HttpClient unless the user specifies HTTP/2.0 or higher
            var accessToken = await _httpConnectionOptions.AccessTokenProvider().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // We can't use request headers in the browser, so instead append the token as a query string in that case
                if (OperatingSystem.IsBrowser())
                {
                    var accessTokenEncoded = UrlEncoder.Default.Encode(accessToken);
                    accessTokenEncoded = "access_token=" + accessTokenEncoded;
                    url = Utils.AppendQueryString(url, accessTokenEncoded);
                }
                else
                {
                    webSocket.Options.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                }
            }
        }

        try
        {
#if NET7_0_OR_GREATER
            // Only share the HttpClient if the user opts-in to HTTP/2 (or higher)
            // This is because there is some non-obvious behavior changes when passing in an invoker to ConnectAsync
            // and there isn't really any benefit to sharing the HttpClient in HTTP/1.1
            if (webSocket.Options.HttpVersion > HttpVersion.Version11)
            {
                await webSocket.ConnectAsync(url, invoker: _httpClient, cancellationToken).ConfigureAwait(false);
            }
            else
#endif
            {
                await webSocket.ConnectAsync(url, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            webSocket.Dispose();
            throw;
        }

        return webSocket;
    }

    public async Task StartAsync(Uri url, TransferFormat transferFormat, CancellationToken cancellationToken = default)
    {
        ArgumentNullThrowHelper.ThrowIfNull(url);

        if (transferFormat != TransferFormat.Binary && transferFormat != TransferFormat.Text)
        {
            throw new ArgumentException($"The '{transferFormat}' transfer format is not supported by this transport.", nameof(transferFormat));
        }

        _webSocketMessageType = transferFormat == TransferFormat.Binary
            ? WebSocketMessageType.Binary
            : WebSocketMessageType.Text;

        var resolvedUrl = ResolveWebSocketsUrl(url);

        Log.StartTransport(_logger, transferFormat, resolvedUrl);

        var context = new WebSocketConnectionContext(resolvedUrl, _httpConnectionOptions);
        var factory = _httpConnectionOptions.WebSocketFactory ?? DefaultWebSocketFactory;
        _webSocket = await factory(context, cancellationToken).ConfigureAwait(false);

        if (_webSocket == null)
        {
            throw new InvalidOperationException("Configured WebSocketFactory did not return a value.");
        }

        Log.StartedTransport(_logger);

        _stopCts = new CancellationTokenSource();

        var isReconnect = false;

        if (_transport is null)
        {
            // Create the pipe pair (Application's writer is connected to Transport's reader, and vice versa)
            var pair = CreateConnectionPair(_httpConnectionOptions.TransportPipeOptions, _httpConnectionOptions.AppPipeOptions);

            _transport = pair.Transport;
            _application = pair.Application;
        }
        else
        {
            isReconnect = true;
        }

        // TODO: Handle TCP connection errors
        // https://github.com/SignalR/SignalR/blob/1fba14fa3437e24c204dfaf8a18db3fce8acad3c/src/Microsoft.AspNet.SignalR.Core/Owin/WebSockets/WebSocketHandler.cs#L248-L251
        Running = ProcessSocketAsync(_webSocket, url, isReconnect);
    }

    private async Task ProcessSocketAsync(WebSocket socket, Uri url, bool isReconnect)
    {
        Debug.Assert(_application != null);

        using (socket)
        {
            // Begin sending and receiving.
            var receiving = StartReceiving(socket);
            var sending = StartSending(socket, ignoreFirstCanceled: isReconnect);

            if (isReconnect)
            {
                Debug.Assert(_notifyOnReconnect is not null);
                await _notifyOnReconnect.Invoke(_transport!.Output).ConfigureAwait(false);
            }

            // Wait for send or receive to complete
            var trigger = await Task.WhenAny(receiving, sending).ConfigureAwait(false);

            _stopCts.CancelAfter(_closeTimeout);

            if (trigger == receiving)
            {
                // We're waiting for the application to finish and there are 2 things it could be doing
                // 1. Waiting for application data
                // 2. Waiting for a websocket send to complete

                // Cancel the application so that ReadAsync yields
                _application.Input.CancelPendingRead();

                var resultTask = await Task.WhenAny(sending, Task.Delay(_closeTimeout, _stopCts.Token)).ConfigureAwait(false);

                if (resultTask != sending)
                {
                    _aborted = true;

                    // Abort the websocket if we're stuck in a pending send to the client
                    socket.Abort();

                    // Should not throw
                    await sending.ConfigureAwait(false);
                }
            }
            else
            {
                // We're waiting on the websocket to close and there are 2 things it could be doing
                // 1. Waiting for websocket data
                // 2. Waiting on a flush to complete (backpressure being applied)

                _aborted = true;

                // Abort the websocket if we're stuck in a pending receive from the client
                socket.Abort();

                // Cancel any pending flush so that we can quit
                if (_gracefulClose)
                {
                    _application.Output.CancelPendingFlush();
                }

                // Should not throw
                await receiving.ConfigureAwait(false);
            }
        }

        var cleanup = true;
        try
        {
            if (!_gracefulClose && UpdateConnectionPair())
            {
                try
                {
                    await StartAsync(url, _webSocketMessageType == WebSocketMessageType.Binary ? TransferFormat.Binary : TransferFormat.Text,
                        cancellationToken: default).ConfigureAwait(false);
                    cleanup = false;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Reconnect attempt failed.", innerException: ex);
                }
            }
        }
        finally
        {
            if (cleanup)
            {
                // Pipes will usually already be completed.
                // If stateful reconnect fails we want to make sure the Pipes are cleaned up.
                // And in rare cases where the websocket is closing at the same time StopAsync is called
                // It's possible a Pipe won't be completed so let's be safe and call Complete again.
                _application.Output.Complete();
                _application.Input.Complete();
            }
        }
    }

    private async Task StartReceiving(WebSocket socket)
    {
        Debug.Assert(_application != null);

        try
        {
            while (true)
            {
#if NETSTANDARD2_1 || NETCOREAPP
                // Do a 0 byte read so that idle connections don't allocate a buffer when waiting for a read
                var result = await socket.ReceiveAsync(Memory<byte>.Empty, _stopCts.Token).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _gracefulClose = true;
                    Log.WebSocketClosed(_logger, socket.CloseStatus);

                    if (socket.CloseStatus != WebSocketCloseStatus.NormalClosure)
                    {
                        throw new InvalidOperationException($"Websocket closed with error: {socket.CloseStatus}.");
                    }

                    return;
                }
#endif
                var memory = _application.Output.GetMemory();
#if NETSTANDARD2_1 || NETCOREAPP
                // Because we checked the CloseStatus from the 0 byte read above, we don't need to check again after reading
                var receiveResult = await socket.ReceiveAsync(memory, _stopCts.Token).ConfigureAwait(false);
#elif NETSTANDARD2_0 || NETFRAMEWORK
                var isArray = MemoryMarshal.TryGetArray<byte>(memory, out var arraySegment);
                Debug.Assert(isArray);

                // Exceptions are handled above where the send and receive tasks are being run.
                var receiveResult = await socket.ReceiveAsync(arraySegment, _stopCts.Token).ConfigureAwait(false);
#else
#error TFMs need to be updated
#endif
                // Need to check again for netstandard2.1 because a close can happen between a 0-byte read and the actual read
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    _gracefulClose = true;
                    Log.WebSocketClosed(_logger, socket.CloseStatus);

                    if (socket.CloseStatus != WebSocketCloseStatus.NormalClosure)
                    {
                        throw new InvalidOperationException($"Websocket closed with error: {socket.CloseStatus}.");
                    }

                    return;
                }

                Log.MessageReceived(_logger, receiveResult.MessageType, receiveResult.Count, receiveResult.EndOfMessage);

                _application.Output.Advance(receiveResult.Count);

                var flushResult = await _application.Output.FlushAsync().ConfigureAwait(false);

                // We canceled in the middle of applying back pressure
                // or if the consumer is done
                if (flushResult.IsCanceled || flushResult.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.ReceiveCanceled(_logger);
        }
        catch (Exception ex)
        {
            if (!_aborted)
            {
                if (_gracefulClose || !_useStatefulReconnect)
                {
                    _application.Output.Complete(ex);
                }
                else
                {
                    // only logging in this case because the other case gets the exception flowed to application code
                    Log.ReceiveErrored(_logger, ex);
                }
            }
        }
        finally
        {
            // We're done writing
            if (_gracefulClose || !_useStatefulReconnect)
            {
                _application.Output.Complete();
            }

            Log.ReceiveStopped(_logger);
        }
    }

    private async Task StartSending(WebSocket socket, bool ignoreFirstCanceled)
    {
        Debug.Assert(_application != null);

        Exception? error = null;

        try
        {
            while (true)
            {
                var result = await _application.Input.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;

                // Get a frame from the application

                try
                {
                    if (result.IsCanceled && !ignoreFirstCanceled)
                    {
                        break;
                    }

                    ignoreFirstCanceled = false;

                    if (!buffer.IsEmpty)
                    {
                        try
                        {
                            Log.ReceivedFromApp(_logger, buffer.Length);

                            if (WebSocketCanSend(socket))
                            {
                                await socket.SendAsync(buffer, _webSocketMessageType, _stopCts.Token).ConfigureAwait(false);
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!_aborted)
                            {
                                Log.ErrorSendingMessage(_logger, ex);
                            }
                            break;
                        }
                    }
                    else if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    _application.Input.AdvanceTo(buffer.End);
                }
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            if (WebSocketCanSend(socket))
            {
                try
                {
                    if (!OperatingSystem.IsBrowser())
                    {
                        // We're done sending, send the close frame to the client if the websocket is still open
                        await socket.CloseOutputAsync(error != null ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure, "", _stopCts.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        // WebSocket in the browser doesn't have an equivalent to CloseOutputAsync, it just calls CloseAsync and logs a warning
                        // So let's just call CloseAsync to avoid the warning
                        await socket.CloseAsync(error != null ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure, "", _stopCts.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.ClosingWebSocketFailed(_logger, ex);
                }
            }

            if (_gracefulClose || !_useStatefulReconnect)
            {
                _application.Input.Complete();
            }
            else
            {
                if (error is not null)
                {
                    Log.SendErrored(_logger, error);
                }
            }

            Log.SendStopped(_logger);
        }
    }

    private static bool WebSocketCanSend(WebSocket ws)
    {
        return !(ws.State == WebSocketState.Aborted ||
               ws.State == WebSocketState.Closed ||
               ws.State == WebSocketState.CloseSent);
    }

    private static Uri ResolveWebSocketsUrl(Uri url)
    {
        var uriBuilder = new UriBuilder(url);
        if (url.Scheme == "http")
        {
            uriBuilder.Scheme = "ws";
        }
        else if (url.Scheme == "https")
        {
            uriBuilder.Scheme = "wss";
        }

        return uriBuilder.Uri;
    }

    public async Task StopAsync()
    {
        _gracefulClose = true;
        Log.TransportStopping(_logger);

        if (_application == null)
        {
            // We never started
            return;
        }

        _transport!.Output.Complete();
        _transport!.Input.Complete();

        // Cancel any pending reads from the application, this should start the entire shutdown process
        _application.Input.CancelPendingRead();

        // Start ungraceful close timer
        _stopCts.CancelAfter(_closeTimeout);

        try
        {
            await Running.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.TransportStopped(_logger, ex);
            // exceptions have been handled in the Running task continuation by closing the channel with the exception
            return;
        }
        finally
        {
            _webSocket?.Dispose();
            _stopCts.Dispose();
        }

        Log.TransportStopped(_logger, null);
    }

    private bool UpdateConnectionPair()
    {
        lock (this)
        {
            // Lock and check _useStatefulReconnect, we want to swap the Pipe completely before DisableReconnect returns if there is contention there.
            // The calling code will start completing the transport after DisableReconnect
            // so we want to avoid any possibility of the new Pipe staying alive or even worse a new WebSocket connection being open when the transport
            // might think it's closed.
            if (_useStatefulReconnect == false)
            {
                return false;
            }

            var input = new Pipe(_httpConnectionOptions.TransportPipeOptions);

            // Add new pipe for reading from and writing to transport from app code
            var transportToApplication = new DuplexPipe(_transport!.Input, input.Writer);
            var applicationToTransport = new DuplexPipe(input.Reader, _application!.Output);

            _application = applicationToTransport;
            _transport = transportToApplication;
        }

        return true;
    }

#pragma warning disable CA2252 // This API requires opting into preview features
    public void DisableReconnect()
#pragma warning restore CA2252 // This API requires opting into preview features
    {
        lock (this)
        {
            _useStatefulReconnect = false;
        }
    }
}
