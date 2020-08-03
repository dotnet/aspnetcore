// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    internal class TestWebAssemblyJSRuntimeInvoker : WebAssemblyJSRuntimeInvoker
    {
        private readonly string _environment;

        public TestWebAssemblyJSRuntimeInvoker(string environment = "Production")
        {
            _environment = environment;
        }

        public override TResult InvokeUnmarshalled<T0, T1, T2, TResult>(string identifier, T0 arg0, T1 arg1, T2 arg2)
        {
            switch (identifier)
            {
                case "Blazor._internal.getApplicationEnvironment":
                    return (TResult)(object)_environment;
                case "Blazor._internal.getConfig":
                    return (TResult)(object)null;
                case "Blazor._internal.navigationManager.getUnmarshalledBaseURI":
                    var testUri = "https://www.example.com/awesome-part-that-will-be-truncated-in-tests";
                    return (TResult)(object)testUri;
                case "Blazor._internal.navigationManager.getUnmarshalledLocationHref":
                    var testHref = "https://www.example.com/awesome-part-that-will-be-truncated-in-tests/cool";
                    return (TResult)(object)testHref;
                default:
                    throw new NotImplementedException($"{nameof(TestWebAssemblyJSRuntimeInvoker)} has no implementation for '{identifier}'.");
            }
        }
    }
}
