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

        public string FindIndexHtmlFile()
        {
            // Before publishing, the client project may have a wwwroot directory.
            // If so, and if it contains index.html, use that.
            if (!string.IsNullOrEmpty(WebRootPath))
            {
                var wwwrootIndexHtmlPath = Path.Combine(WebRootPath, "index.html");
                if (File.Exists(wwwrootIndexHtmlPath))
                {
                    return wwwrootIndexHtmlPath;
                }
            }

            // After publishing, the client project won't have a wwwroot directory.
            // The contents from that dir will have been copied to "dist" during publish.
            // So if "dist/index.html" now exists, use that.
            var distIndexHtmlPath = Path.Combine(DistPath, "index.html");
            if (File.Exists(distIndexHtmlPath))
            {
                return distIndexHtmlPath;
            }

            // Since there's no index.html, we'll use the default DefaultPageStaticFileOptions,
            // hence we'll look for index.html in the host server app's wwwroot.
            return null;
        }
    }
}
