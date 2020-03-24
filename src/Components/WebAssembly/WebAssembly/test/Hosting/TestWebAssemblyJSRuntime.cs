// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop.WebAssembly;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    public class TestWebAssemblyJSRuntime
    {
        public static WebAssemblyJSRuntime Create(string environment = "Production")
        {
            var jsRuntime = new Mock<WebAssemblyJSRuntime>();
            jsRuntime.Setup(j => j.InvokeUnmarshalled<object, object, object, string>("Blazor._internal.getApplicationEnvironment", null, null, null))
                .Returns(environment)
                .Verifiable();

            jsRuntime.Setup(j => j.InvokeUnmarshalled<string, object, object, byte[]>("Blazor._internal.getConfig", It.IsAny<string>(), null, null))
                .Returns((byte[])null)
                .Verifiable();

            return jsRuntime.Object;
        }
    }
}
