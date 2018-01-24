// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.Server
{
    internal class BlazorConfig
    {
        public string SourceMSBuildPath { get; }
        public string SourceOutputAssemblyPath { get; }
        public string WebRootPath { get; }

        public static BlazorConfig Read(string assemblyPath)
            => new BlazorConfig(assemblyPath);

        private BlazorConfig(string assemblyPath)
        {
            var configFilePath = Path.ChangeExtension(assemblyPath, ".blazor.config");
            var configLines = File.ReadLines(configFilePath).ToList();
            SourceMSBuildPath = configLines[0];

            var sourceMsBuildDir = Path.GetDirectoryName(SourceMSBuildPath);
            SourceOutputAssemblyPath = Path.Combine(sourceMsBuildDir, configLines[1]);

            var webRootPath = Path.Combine(sourceMsBuildDir, "wwwroot");
            if (Directory.Exists(webRootPath))
            {
                WebRootPath = webRootPath;
            }
        }
    }
}
