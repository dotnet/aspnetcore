// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal static partial class BuildVariables
    {
        private static readonly IEnumerable<AssemblyMetadataAttribute> TestAssemblyMetadata = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>();

        public static string MSBuildPath => TestAssemblyMetadata.SingleOrDefault(a => a.Key == "DesktopMSBuildPath").Value;

        public static string MicrosoftNETCoreAppRuntimeVersion => TestAssemblyMetadata.SingleOrDefault(a => a.Key == "MicrosoftNETCoreAppRuntimeVersion").Value;

        public static string MicrosoftNetCompilersToolsetPackageVersion => TestAssemblyMetadata.SingleOrDefault(a => a.Key == "MicrosoftNetCompilersToolsetPackageVersion").Value;

        public static string RazorSdkDirectoryRoot => TestAssemblyMetadata.SingleOrDefault(a => a.Key == "RazorSdkDirectoryRoot").Value;

        public static string RepoRoot => TestAssemblyMetadata.SingleOrDefault(a => a.Key == "Testing.RepoRoot").Value;
        
        public static string DefaultNetCoreTargetFramework => TestAssemblyMetadata.SingleOrDefault(a => a.Key == "DefaultNetCoreTargetFramework").Value;
    }
}
