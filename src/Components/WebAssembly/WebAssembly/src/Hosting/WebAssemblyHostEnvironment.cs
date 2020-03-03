// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    internal sealed class WebAssemblyHostEnvironment : IWebAssemblyHostEnvironment
    {
        public WebAssemblyHostEnvironment(string environment) => Environment = environment;

        public string Environment { get; }
    }
}
