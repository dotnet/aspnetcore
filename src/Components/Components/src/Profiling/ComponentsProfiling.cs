// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Components.Profiling
{
    internal abstract class ComponentsProfiling
    {
        // For now, this is only intended for use on Blazor WebAssembly, and will have no effect
        // when running on Blazor Server. The reason for having the ComponentsProfiling abstraction
        // is so that if we later have two different implementations (one for WebAssembly, one for
        // Server), the execution characteristics of calling Start/End will be unchanged and historical
        // perf data will still be comparable to newer data.
        public static readonly ComponentsProfiling Instance = PlatformInfo.IsWebAssembly
            ? new WebAssemblyComponentsProfiling()
            : (ComponentsProfiling)new NoOpComponentsProfiling();

        public abstract void Start([CallerMemberName] string? name = null);
        public abstract void End([CallerMemberName] string? name = null);
    }
}
