// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    [Obsolete("Resolve the current JavaScript runtime from the dependency injection container instead.")]
    internal interface IJSRuntimeAccessor
    {
        IJSRuntime JSRuntime { get; }
    }
}
