// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class DefaultJSRuntimeAccessor : IJSRuntimeAccessor
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public IJSRuntime JSRuntime { get; set; }
    }
}
