// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class UnsupportedJavaScriptRuntime : IJSRuntime
    {
        public Task<T> InvokeAsync<T>(string identifier, params object[] args)
        {
            throw new InvalidOperationException("JavaScript interoperability is not supported in the current environment.");
        }

        public void UntrackObjectRef(DotNetObjectRef dotNetObjectRef)
        {
            throw new InvalidOperationException("JavaScript interoperability is not supported in the current environment.");
        }
    }
}
