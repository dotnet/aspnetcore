// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    internal class DotNetReferenceAssembliesPathResolver
    {
        public static readonly string DotNetReferenceAssembliesPathEnv = "DOTNET_REFERENCE_ASSEMBLIES_PATH";
        
        internal static string Resolve(IRuntimeEnvironment runtimeEnvironment)
        {
            var path = Environment.GetEnvironmentVariable(DotNetReferenceAssembliesPathEnv);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
            
            return GetDefaultDotNetReferenceAssembliesPath(runtimeEnvironment);
        }
        
        public static string Resolve()
        {
            return Resolve(PlatformServices.Default.Runtime);
        }
                
        private static string GetDefaultDotNetReferenceAssembliesPath(IRuntimeEnvironment runtimeEnvironment)
        {            
            var os = runtimeEnvironment.OperatingSystemPlatform;
            
            if (os == Platform.Windows)
            {
                return null;
            }
            
            if (os == Platform.Darwin && 
                Directory.Exists("/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks"))
            {
                return "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks";
            }
            
            if (Directory.Exists("/usr/local/lib/mono/xbuild-frameworks"))
            {
                return "/usr/local/lib/mono/xbuild-frameworks";
            }
            
            if (Directory.Exists("/usr/lib/mono/xbuild-frameworks"))
            {
                return "/usr/lib/mono/xbuild-frameworks";
            }
            
            return null;
        }
    }
}
