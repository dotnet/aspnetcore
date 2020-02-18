// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Blazor.Build
{
    public class GenerateServiceWorkerAssetsManifest : Task
    {
        [Required]
        public string Version { get; set; }

        [Required]
        public ITaskItem[] AssetsWithHashes { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            using var fileStream = File.Create(OutputPath);
            WriteFile(fileStream);
            return true;
        }

        internal void WriteFile(Stream stream)
        {
            var data = new AssetsManifestFile
            {
                version = Version,
                assets = AssetsWithHashes.Select(item => new AssetsManifestFileEntry
                {
                    url = item.GetMetadata("AssetUrl"),
                    hash = $"sha256-{item.GetMetadata("FileHash")}",
                }).ToArray()
            };

            using var streamWriter = new StreamWriter(stream, Encoding.UTF8, bufferSize: 50, leaveOpen: true);
            streamWriter.Write("self.assetsManifest = ");
            streamWriter.Flush();

            using var jsonWriter = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, ownsStream: false, indent: true);
            new DataContractJsonSerializer(typeof(AssetsManifestFile)).WriteObject(jsonWriter, data);
            jsonWriter.Flush();

            streamWriter.WriteLine(";");
        }

#pragma warning disable IDE1006 // Naming Styles
        public class AssetsManifestFile
        {
            /// <summary>
            /// Gets or sets a version string.
            /// </summary>
            public string version { get; set; }

            /// <summary>
            /// Gets or sets the assets. Keys are URLs; values are base-64-formatted SHA256 content hashes.
            /// </summary>
            public AssetsManifestFileEntry[] assets { get; set; }
        }

        public class AssetsManifestFileEntry
        {
            /// <summary>
            /// Gets or sets the asset URL. Normally this will be relative to the application's base href.
            /// </summary>
            public string url { get; set; }

            /// <summary>
            /// Gets or sets the file content hash. This should be the base-64-formatted SHA256 value.
            /// </summary>
            public string hash { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles
    }
}
