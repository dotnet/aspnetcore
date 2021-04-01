// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
