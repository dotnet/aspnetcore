// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public partial class GenerateServiceWorkerAssetsManifest : Task
    {
        [Required]
        public ITaskItem[] Assets { get; set; }

        public string Version { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Output]
        public string CalculatedVersion { get; set; }

        public override bool Execute()
        {
            using var fileStream = File.Create(OutputPath);
            CalculatedVersion = GenerateAssetManifest(fileStream);

            return true;
        }

        internal string GenerateAssetManifest(Stream stream)
        {
            var assets = new AssetsManifestFileEntry[Assets.Length];
            System.Threading.Tasks.Parallel.For(0, assets.Length, i =>
            {
                var item = Assets[i];
                var hash = item.GetMetadata("FileHash");
                var url = item.GetMetadata("AssetUrl");

                if (string.IsNullOrEmpty(hash))
                {
                    // Some files that are part of the service worker manifest may not have their hashes previously
                    // calcualted. Calculate them at this time.
                    using var sha = SHA256.Create();
                    using var file = File.OpenRead(item.ItemSpec);
                    var bytes = sha.ComputeHash(file);

                    hash = Convert.ToBase64String(bytes);
                }

                assets[i] = new AssetsManifestFileEntry
                {
                    hash = "sha256-" + hash,
                    url = url,
                };
            });

            var version = Version;
            if (string.IsNullOrEmpty(version))
            {
                // If a version isn't specified (which is likely the most common case), construct a Version by combining
                // the file names + hashes of all the inputs.

                var combinedHash = string.Join(
                    Environment.NewLine,
                    assets.OrderBy(f => f.url, StringComparer.Ordinal).Select(f => f.hash));
                
                using var sha = SHA256.Create();
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(combinedHash));
                version = Convert.ToBase64String(bytes).Substring(0, 8);
            }

            var data = new AssetsManifestFile
            {
                version = version,
                assets = assets,
            };

            using var streamWriter = new StreamWriter(stream, Encoding.UTF8, bufferSize: 50, leaveOpen: true);
            streamWriter.Write("self.assetsManifest = ");
            streamWriter.Flush();

            using var jsonWriter = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, ownsStream: false, indent: true);
            new DataContractJsonSerializer(typeof(AssetsManifestFile)).WriteObject(jsonWriter, data);
            jsonWriter.Flush();

            streamWriter.WriteLine(";");

            return version;
        }
    }
}
