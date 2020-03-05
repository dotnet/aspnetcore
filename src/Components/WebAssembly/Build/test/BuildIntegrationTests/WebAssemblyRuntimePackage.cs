// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    internal static class WebAssemblyRuntimePackage
    {
        public static readonly string ComponentsWebAssemblyRuntimePackageVersion;
        public static readonly string DotNetJsFileName;

        static WebAssemblyRuntimePackage()
        {
            ComponentsWebAssemblyRuntimePackageVersion = typeof(WebAssemblyRuntimePackage)
                .Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(f => f.Key == "Testing.MicrosoftAspNetCoreComponentsWebAssemblyRuntimePackageVersion")
                ?.Value
                ?? throw new InvalidOperationException("Testing.MicrosoftAspNetCoreComponentsWebAssemblyRuntimePackageVersion was not found");

            DotNetJsFileName = $"dotnet.{ComponentsWebAssemblyRuntimePackageVersion}.js";
        }
    }
}
