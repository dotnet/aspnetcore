// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public static class TestFacts
    {
        public static string DefaultNetCoreTargetFramework =>
            GetAttributeValue(nameof(DefaultNetCoreTargetFramework));

        public static string RazorSdkDirectoryRoot =>
            GetAttributeValue(nameof(RazorSdkDirectoryRoot));

        private static string GetAttributeValue(string name)
        {
            return Assembly
                .GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == $"Testing.{name}")
                .Value;
        }
    }
}
