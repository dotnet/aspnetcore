// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class UnsupportedJavaScriptRuntime : IJSRuntime
    {
        private const string _preRenderScriptError = "JavaScript interop calls cannot be issued during server-side prerendering, because the page has not yet loaded in the browser. Prerendered components must wrap any JavaScript interop calls in conditional logic to ensure those interop calls are not attempted during prerendering.";

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            throw new InvalidOperationException(_preRenderScriptError);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, JsonSerializerOptions jsonSerializerOptions, object[] args)
        {
            throw new InvalidOperationException(_preRenderScriptError);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, JsonSerializerOptions jsonSerializerOptions, object[] args)
        {
            throw new InvalidOperationException(_preRenderScriptError);
        }

        ValueTask<TValue> IJSRuntime.InvokeAsync<TValue>(string identifier, object[] args)
        {
            throw new InvalidOperationException(_preRenderScriptError);
        }
    }
}
