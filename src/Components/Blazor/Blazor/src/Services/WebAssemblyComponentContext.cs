// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Services;

namespace Microsoft.AspNetCore.Blazor.Services
{
    internal class WebAssemblyComponentContext : IComponentContext
    {
        public bool IsConnected => true;
    }
}
