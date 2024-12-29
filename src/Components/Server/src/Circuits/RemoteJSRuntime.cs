// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

#pragma warning disable CA1852 // Seal internal types
internal partial class RemoteJSRuntime : JSRuntime
#pragma warning restore CA1852 // Seal internal types
{
    private readonly CircuitOptions _options;
    private readonly ILogger<RemoteJSRuntime> _logger;
    private CircuitClientProxy _clientProxy;
    private readonly ConcurrentDictionary<long, CancelableDotNetStreamReference> _pendingDotNetToJSStreams = new();
    private bool _permanentlyDisconnected;
    private readonly long _maximumIncomingBytes;
    private int _byteArraysToBeRevivedTotalBytes;

    internal int RemoteJSDataStreamNextInstanceId;
    internal readonly Dictionary<long, RemoteJSDataStream> RemoteJSDataStreamInstances = new();

    public ElementReferenceContext ElementReferenceContext { get; }

    public bool IsInitialized => _clientProxy is not null;

    internal bool IsPermanentlyDisconnected => _permanentlyDisconnected;

    /// <summary>
    /// Notifies when a runtime exception occurred.
    /// </summary>
    public event EventHandler<Exception>? UnhandledException;

    public RemoteJSRuntime(
        IOptions<CircuitOptions> circuitOptions,
        IOptions<HubOptions<ComponentHub>> componentHubOptions,
        ILogger<RemoteJSRuntime> logger)
    {
        _options = circuitOptions.Value;
        _maximumIncomingBytes = componentHubOptions.Value.MaximumReceiveMessageSize ?? long.MaxValue;
        _logger = logger;
        DefaultAsyncTimeout = _options.JSInteropDefaultCallTimeout;
        ElementReferenceContext = new WebElementReferenceContext(this);
        JsonSerializerOptions.Converters.Add(new ElementReferenceJsonConverter(ElementReferenceContext));
    }

    public JsonSerializerOptions ReadJsonSerializerOptions() => JsonSerializerOptions;

    internal void Initialize(CircuitClientProxy clientProxy)
    {
        _clientProxy = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
    }

    internal void RaiseUnhandledException(Exception ex)
    {
        UnhandledException?.Invoke(this, ex);
    }

    protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
    {
        if (!invocationResult.Success)
        {
            Log.InvokeDotNetMethodException(_logger, invocationInfo, invocationResult.Exception);
            string errorMessage;

            if (_options.DetailedErrors)
            {
                errorMessage = invocationResult.Exception.ToString();
            }
            else
            {
                errorMessage = $"There was an exception invoking '{invocationInfo.MethodIdentifier}'";
                if (invocationInfo.AssemblyName != null)
                {
                    errorMessage += $" on assembly '{invocationInfo.AssemblyName}'";
                }

                errorMessage += $". For more details turn on detailed exceptions in '{nameof(CircuitOptions)}.{nameof(CircuitOptions.DetailedErrors)}'";
            }

            _clientProxy.SendAsync("JS.EndInvokeDotNet",
                invocationInfo.CallId,
                /* success */ false,
                errorMessage);
        }
        else
        {
            Log.InvokeDotNetMethodSuccess(_logger, invocationInfo);
            _clientProxy.SendAsync("JS.EndInvokeDotNet",
                invocationInfo.CallId,
                /* success */ true,
                invocationResult.ResultJson);
        }
    }

    protected override void SendByteArray(int id, byte[] data)
    {
        _clientProxy.SendAsync("JS.ReceiveByteArray", id, data);
    }

    protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
    {
        if (_clientProxy is null)
        {
            if (_permanentlyDisconnected)
            {
                throw new JSDisconnectedException(
               "JavaScript interop calls cannot be issued at this time. This is because the circuit has disconnected " +
               "and is being disposed.");
            }
            else
            {
                throw new InvalidOperationException(
                    "JavaScript interop calls cannot be issued at this time. This is because the component is being " +
                    "statically rendered. When prerendering is enabled, JavaScript interop calls can only be performed " +
                    "during the OnAfterRenderAsync lifecycle method.");
            }
        }

        Log.BeginInvokeJS(_logger, asyncHandle, identifier);

        _clientProxy.SendAsync("JS.BeginInvokeJS", asyncHandle, identifier, argsJson, (int)resultType, targetInstanceId);
    }

    protected override void ReceiveByteArray(int id, byte[] data)
    {
        if (id == 0)
        {
            // Starting a new transfer, clear out number of bytes read.
            _byteArraysToBeRevivedTotalBytes = 0;
        }

        if (_maximumIncomingBytes - data.Length < _byteArraysToBeRevivedTotalBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Exceeded the maximum byte array transfer limit for a call.");
        }

        // We also store the total number of bytes seen so far to compare against
        // the MaximumIncomingBytes limit.
        // We take the larger of the size of the array or 4, to ensure we're not inundated
        // with small/empty arrays.
        _byteArraysToBeRevivedTotalBytes += Math.Max(4, data.Length);

        base.ReceiveByteArray(id, data);
    }

    protected override async Task TransmitStreamAsync(long streamId, DotNetStreamReference dotNetStreamReference)
    {
        var cancelableStreamReference = new CancelableDotNetStreamReference(dotNetStreamReference);
        if (!_pendingDotNetToJSStreams.TryAdd(streamId, cancelableStreamReference))
        {
            throw new ArgumentException($"The stream {streamId} is already pending.");
        }

        // SignalR only supports streaming being initiated from the JS side, so we have to ask it to
        // start the stream. We'll give it a maximum of 10 seconds to do so, after which we give up
        // and discard it.
        CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(10));

        // Store CTS to dispose later.
        cancelableStreamReference.CancellationTokenSource = cancellationTokenSource;

        cancellationTokenSource.Token.Register(() =>
        {
            // If by now the stream hasn't been claimed for sending, stop tracking it
            if (_pendingDotNetToJSStreams.TryRemove(streamId, out var timedOutCancelableStreamReference))
            {
                timedOutCancelableStreamReference.StreamReference.Dispose();
                timedOutCancelableStreamReference.CancellationTokenSource?.Dispose();
            }
        });

        await _clientProxy.SendAsync("JS.BeginTransmitStream", streamId);
    }

    public bool TryClaimPendingStreamForSending(long streamId, out DotNetStreamReference pendingStream)
    {
        if (_pendingDotNetToJSStreams.TryRemove(streamId, out var cancelableStreamReference))
        {
            pendingStream = cancelableStreamReference.StreamReference;

            // Dispose CTS for claimed Stream.
            cancelableStreamReference.CancellationTokenSource?.Dispose();

            return true;
        }

        pendingStream = default;
        return false;
    }

    public void MarkPermanentlyDisconnected()
    {
        _permanentlyDisconnected = true;
        _clientProxy = null;
    }

    protected override async Task<Stream> ReadJSDataAsStreamAsync(IJSStreamReference jsStreamReference, long totalLength, CancellationToken cancellationToken = default)
        => await RemoteJSDataStream.CreateRemoteJSDataStreamAsync(this, jsStreamReference, totalLength, _maximumIncomingBytes, _options.JSInteropDefaultCallTimeout, cancellationToken);

    private class CancelableDotNetStreamReference
    {
        public CancelableDotNetStreamReference(DotNetStreamReference streamReference)
        {
            StreamReference = streamReference;
        }

        public CancellationTokenSource? CancellationTokenSource { get; set; }

        public DotNetStreamReference StreamReference { get; }
    }

    public static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Begin invoke JS interop '{AsyncHandle}': '{FunctionIdentifier}'", EventName = "BeginInvokeJS")]
        internal static partial void BeginInvokeJS(ILogger logger, long asyncHandle, string functionIdentifier);

        [LoggerMessage(2, LogLevel.Debug, "There was an error invoking the static method '[{AssemblyName}]::{MethodIdentifier}' with callback id '{CallbackId}'.", EventName = "InvokeStaticDotNetMethodException")]
        private static partial void InvokeStaticDotNetMethodException(ILogger logger, string assemblyName, string methodIdentifier, string? callbackId, Exception exception);

        [LoggerMessage(4, LogLevel.Debug, "There was an error invoking the instance method '{MethodIdentifier}' on reference '{DotNetObjectReference}' with callback id '{CallbackId}'.", EventName = "InvokeInstanceDotNetMethodException")]
        private static partial void InvokeInstanceDotNetMethodException(ILogger logger, string methodIdentifier, long dotNetObjectReference, string? callbackId, Exception exception);

        [LoggerMessage(3, LogLevel.Debug, "Invocation of '[{AssemblyName}]::{MethodIdentifier}' with callback id '{CallbackId}' completed successfully.", EventName = "InvokeStaticDotNetMethodSuccess")]
        private static partial void InvokeStaticDotNetMethodSuccess(ILogger<RemoteJSRuntime> logger, string assemblyName, string methodIdentifier, string? callbackId);

        [LoggerMessage(5, LogLevel.Debug, "Invocation of '{MethodIdentifier}' on reference '{DotNetObjectReference}' with callback id '{CallbackId}' completed successfully.", EventName = "InvokeInstanceDotNetMethodSuccess")]
        private static partial void InvokeInstanceDotNetMethodSuccess(ILogger<RemoteJSRuntime> logger, string methodIdentifier, long dotNetObjectReference, string? callbackId);

        internal static void InvokeDotNetMethodException(ILogger logger, in DotNetInvocationInfo invocationInfo, Exception exception)
        {
            if (invocationInfo.AssemblyName != null)
            {
                InvokeStaticDotNetMethodException(logger, invocationInfo.AssemblyName, invocationInfo.MethodIdentifier, invocationInfo.CallId, exception);
            }
            else
            {
                InvokeInstanceDotNetMethodException(logger, invocationInfo.MethodIdentifier, invocationInfo.DotNetObjectId, invocationInfo.CallId, exception);
            }
        }

        internal static void InvokeDotNetMethodSuccess(ILogger<RemoteJSRuntime> logger, in DotNetInvocationInfo invocationInfo)
        {
            if (invocationInfo.AssemblyName != null)
            {
                InvokeStaticDotNetMethodSuccess(logger, invocationInfo.AssemblyName, invocationInfo.MethodIdentifier, invocationInfo.CallId);
            }
            else
            {
                InvokeInstanceDotNetMethodSuccess(logger, invocationInfo.MethodIdentifier, invocationInfo.DotNetObjectId, invocationInfo.CallId);
            }
        }
    }
}
