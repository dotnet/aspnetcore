// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Razor.Tasks;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal static class ServiceWorkerAssert
    {
        internal static void VerifyServiceWorkerFiles(MSBuildResult result, string outputDirectory, string serviceWorkerPath, string serviceWorkerContent, string assetsManifestPath)
        {
            // Check the expected files are there
            var serviceWorkerResolvedPath = Assert.FileExists(result, outputDirectory, serviceWorkerPath);
            var assetsManifestResolvedPath = Assert.FileExists(result, outputDirectory, assetsManifestPath);

            // Check the service worker contains the expected content (which comes from the PublishedContent file)
            Assert.FileContains(result, serviceWorkerResolvedPath, serviceWorkerContent);

            // Check the assets manifest version was added to the published service worker
            var assetsManifest = ReadServiceWorkerAssetsManifest(assetsManifestResolvedPath);
            Assert.FileContains(result, serviceWorkerResolvedPath, $"/* Manifest version: {assetsManifest.version} */");

            // Check the assets manifest contains correct entries for all static content we're publishing
            var resolvedPublishDirectory = Path.Combine(result.Project.DirectoryPath, outputDirectory);
            var outputFiles = Directory.GetFiles(resolvedPublishDirectory, "*", new EnumerationOptions { RecurseSubdirectories = true });
            var assetsManifestHashesByUrl = (IReadOnlyDictionary<string, string>)assetsManifest.assets.ToDictionary(x => x.url, x => x.hash);
            foreach (var filePath in outputFiles)
            {
                var relativePath = Path.GetRelativePath(resolvedPublishDirectory, filePath);

                // We don't list compressed files in the SWAM, as these are transparent to the client,
                // nor do we list the service worker itself or its assets manifest, as these don't need to be fetched in the same way
                if (IsCompressedFile(relativePath)
                    || string.Equals(relativePath, serviceWorkerPath, StringComparison.Ordinal)
                    || string.Equals(relativePath, assetsManifestPath, StringComparison.Ordinal))
                {
                    continue;
                }

                // Verify hash
                var fileUrl = relativePath.Replace('\\', '/');
                var expectedHash = ParseWebFormattedHash(assetsManifestHashesByUrl[fileUrl]);
                Assert.Contains(fileUrl, assetsManifestHashesByUrl);
                Assert.FileHashEquals(result, filePath, expectedHash);
            }
        }

        private static string ParseWebFormattedHash(string webFormattedHash)
        {
            Assert.StartsWith("sha256-", webFormattedHash);
            return webFormattedHash.Substring(7);
        }

        private static bool IsCompressedFile(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".br":
                case ".gz":
                    return true;
                default:
                    return false;
            }
        }

        private static AssetsManifestFile ReadServiceWorkerAssetsManifest(string assetsManifestResolvedPath)
        {
            var jsContents = File.ReadAllText(assetsManifestResolvedPath);
            var jsonStart = jsContents.IndexOf("{");
            var jsonLength = jsContents.LastIndexOf("}") - jsonStart + 1;
            var json = jsContents.Substring(jsonStart, jsonLength);
            return JsonSerializer.Deserialize<AssetsManifestFile>(json);
        }
    }
}
