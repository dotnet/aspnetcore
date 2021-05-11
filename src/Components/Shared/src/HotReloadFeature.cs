// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.HotReload
{
    internal static class HotReloadFeature
    {
        /// <summary>
        /// Gets a value that determines if hot reload is supported. Currently, the <c>Debugger.IsSupported</c> feature switch is used as a proxy for this.
        /// Changing to a dedicated feature switch is tracked by https://github.com/dotnet/runtime/issues/51159.
        /// </summary>
        public static bool IsSupported { get; } = AppContext.TryGetSwitch("System.Diagnostics.Debugger.IsSupported", out var isSupported) ? isSupported : true;
    }
}
