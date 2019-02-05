// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class BlazorConfig
    {
        public string SourceMSBuildPath { get; }
        public string SourceOutputAssemblyPath { get; }
        public string WebRootPath { get; }
        public string DistPath
            => Path.Combine(Path.GetDirectoryName(SourceOutputAssemblyPath), "dist");
        public bool EnableAutoRebuilding { get; }
        public bool EnableDebugging { get; }

        public static BlazorConfig Read(string assemblyPath)
            => new BlazorConfig(assemblyPath);

        private BlazorConfig(string assemblyPath)
        {
            // TODO: Instead of assuming the lines are in a specific order, either JSON-encode
            // the whole thing, or at least give the lines key prefixes (e.g., "reload:<someuri>")
            // so we're not dependent on order and all lines being present.

            var configFilePath = Path.ChangeExtension(assemblyPath, ".blazor.config");
            var configLines = File.ReadLines(configFilePath).ToList();
            SourceMSBuildPath = configLines[0];

            if (SourceMSBuildPath == ".")
            {
                SourceMSBuildPath = assemblyPath;
            }

            var sourceMsBuildDir = Path.GetDirectoryName(SourceMSBuildPath);
            SourceOutputAssemblyPath = Path.Combine(sourceMsBuildDir, configLines[1]);

            var webRootPath = Path.Combine(sourceMsBuildDir, "wwwroot");
            if (Directory.Exists(webRootPath))
            {
                WebRootPath = webRootPath;
            }

            EnableAutoRebuilding = configLines.Contains("autorebuild:true", StringComparer.Ordinal);
            EnableDebugging = configLines.Contains("debug:true", StringComparer.Ordinal);
        }
    }
}
