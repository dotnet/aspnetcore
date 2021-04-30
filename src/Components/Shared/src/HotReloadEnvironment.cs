// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.HotReload
{
    internal class HotReloadEnvironment
    {
        public static readonly HotReloadEnvironment Instance = new(Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES") == "debug");

        public HotReloadEnvironment(bool isHotReloadEnabled)
        {
            IsHotReloadEnabled = isHotReloadEnabled;
        }

        /// <summary>
        /// Gets a value that determines if HotReload is configured for this application.
        /// </summary>
        public bool IsHotReloadEnabled { get; }
    }
}
