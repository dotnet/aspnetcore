// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal sealed class WebViewJSRuntime : JSRuntime
{
    private IpcSender _ipcSender;

    public ElementReferenceContext ElementReferenceContext { get; }

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

    public JsonSerializerOptions ReadJsonSerializerOptions() => JsonSerializerOptions;

    protected override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
    {
        if (_ipcSender is null)
        {
            throw new InvalidOperationException("Cannot invoke JavaScript outside of a WebView context.");
        }

        _ipcSender.BeginInvokeJS(taskId, identifier, argsJson, resultType, targetInstanceId);
    }

    protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
    {
        var resultJsonOrErrorMessage = invocationResult.Success
            ? invocationResult.ResultJson
            : invocationResult.Exception.ToString();
        _ipcSender.EndInvokeDotNet(invocationInfo.CallId, invocationResult.Success, resultJsonOrErrorMessage);
    }

    protected override void SendByteArray(int id, byte[] data)
    {
        _ipcSender.SendByteArray(id, data);
    }

    protected override Task<Stream> ReadJSDataAsStreamAsync(IJSStreamReference jsStreamReference, long totalLength, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream>(PullFromJSDataStream.CreateJSDataStream(this, jsStreamReference, totalLength, cancellationToken));

    protected override Task TransmitStreamAsync(long streamId, DotNetStreamReference dotNetStreamReference)
    {
        return TransmitDataStreamToJS.TransmitStreamAsync(this, "Blazor._internal.receiveWebViewDotNetDataStream", streamId, dotNetStreamReference);
    }
}
