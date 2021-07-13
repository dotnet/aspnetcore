// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView.Services
{
    internal class WebViewJSRuntime : JSRuntime
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

        protected override Task<Stream> ReadJSDataAsStreamAsync(IJSStreamReference jsStreamReference, long totalLength, long pauseIncomingBytesThreshold = -1, long resumeIncomingBytesThreshold = -1, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(PullFromJSDataStream.CreateJSDataStream(this, jsStreamReference, totalLength, cancellationToken));
    }
}
