// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.Server
{
    internal class BlazorConfig
    {
        public string SourceMSBuildPath { get; }
        public string SourceOutputAssemblyPath { get; }
        public string WebRootPath { get; }
        public string ReloadUri { get; }
        public string DistPath
            => Path.Combine(Path.GetDirectoryName(SourceOutputAssemblyPath), "dist");

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

            var sourceMsBuildDir = Path.GetDirectoryName(SourceMSBuildPath);
            SourceOutputAssemblyPath = Path.Combine(sourceMsBuildDir, configLines[1]);

            var webRootPath = Path.Combine(sourceMsBuildDir, "wwwroot");
            if (Directory.Exists(webRootPath))
            {
                WebRootPath = webRootPath;
            }

            const string reloadMarker = "reload:";
            var reloadUri = configLines
                .Where(line => line.StartsWith(reloadMarker, StringComparison.Ordinal))
                .Select(line => line.Substring(reloadMarker.Length))
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(reloadUri))
            {
                ReloadUri = reloadUri;
            }
        }
    }
}
