// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal sealed class WebViewJSRuntime : JSRuntime
{
    private IpcSender _ipcSender;
    private bool _isDisposed;

    public ElementReferenceContext ElementReferenceContext { get; }

    /// <summary>
    /// Whether <see cref="MarkAsDisconnected"/> has been called. Once true, subsequent JS interop
    /// calls through <see cref="BeginInvokeJS(in JSInvocationInfo)"/> throw <see cref="JSDisconnectedException"/>
    /// and the runtime no longer forwards .NET-from-JS replies or byte-array transfers to the
    /// <see cref="IpcSender"/>.
    /// </summary>
    internal bool IsDisposed => _isDisposed;

    public WebViewJSRuntime()
    {
        ElementReferenceContext = new WebElementReferenceContext(this);
        JsonSerializerOptions.Converters.Add(
            new ElementReferenceJsonConverter(
                new WebElementReferenceContext(this)));
    }

    public void AttachToWebView(IpcSender ipcSender)
    {
        _ipcSender = ipcSender;
    }

    /// <summary>
    /// Marks the runtime as disconnected from its <see cref="IpcSender"/>. After this call,
    /// <see cref="BeginInvokeJS(in JSInvocationInfo)"/> throws <see cref="JSDisconnectedException"/>
    /// (matching <c>RemoteJSRuntime.MarkPermanentlyDisconnected</c> in Blazor Server) so that
    /// components performing JS interop during <c>DisposeAsync</c> see a recoverable exception
    /// rather than queueing IPC traffic to a defunct page.
    /// </summary>
    internal void MarkAsDisconnected()
    {
        _isDisposed = true;
    }

    public JsonSerializerOptions ReadJsonSerializerOptions() => JsonSerializerOptions;

    protected override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
    {
        var invocationInfo = new JSInvocationInfo
        {
            AsyncHandle = taskId,
            Identifier = identifier,
            ArgsJson = argsJson,
            CallType = JSCallType.FunctionCall,
            ResultType = resultType,
            TargetInstanceId = targetInstanceId,
        };

        BeginInvokeJS(invocationInfo);
    }

    protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
    {
        if (_isDisposed)
        {
            throw new JSDisconnectedException(
                "JavaScript interop calls cannot be issued at this time. This is because the WebView page " +
                "has been disposed (for example, the page navigated away or the WebView itself is being disposed).");
        }

        if (_ipcSender is null)
        {
            throw new InvalidOperationException("Cannot invoke JavaScript outside of a WebView context.");
        }

        _ipcSender.BeginInvokeJS(invocationInfo);
    }

    protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
    {
        if (_isDisposed)
        {
            // The JS side that initiated this call is gone; dropping the reply is the only
            // safe action. Surfacing an exception here would propagate into user code that
            // completed a .NET handler after the page was already torn down.
            return;
        }

        var resultJsonOrErrorMessage = invocationResult.Success
            ? invocationResult.ResultJson
            : invocationResult.Exception.ToString();
        _ipcSender.EndInvokeDotNet(invocationInfo.CallId, invocationResult.Success, resultJsonOrErrorMessage);
    }

    protected override void SendByteArray(int id, byte[] data)
    {
        if (_isDisposed)
        {
            // The receiving JS context is gone; drop the chunk silently.
            return;
        }

        _ipcSender.SendByteArray(id, data);
    }

    protected override Task<Stream> ReadJSDataAsStreamAsync(IJSStreamReference jsStreamReference, long totalLength, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream>(PullFromJSDataStream.CreateJSDataStream(this, jsStreamReference, totalLength, cancellationToken));

    protected override Task TransmitStreamAsync(long streamId, DotNetStreamReference dotNetStreamReference)
    {
        return TransmitDataStreamToJS.TransmitStreamAsync(this, "Blazor._internal.receiveWebViewDotNetDataStream", streamId, dotNetStreamReference);
    }
}
