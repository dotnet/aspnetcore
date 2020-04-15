// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class GzipCompressBlazorApplicationFiles : Task
    {
        [Required]
        public string ManifestPath { get; set; }

        public override bool Execute()
        {
            var serializer = new DataContractJsonSerializer(typeof(ManifestData));

            ManifestData manifest = null;
            using (var tempFile = File.OpenRead(ManifestPath))
            {
                manifest = (ManifestData)serializer.ReadObject(tempFile);
            }

            System.Threading.Tasks.Parallel.ForEach(manifest.FilesToCompress, (file) =>
            {
                var inputPath = file.Source;
                var inputSource = file.InputSource;
                var targetCompressionPath = file.Target;

                if (!File.Exists(inputSource))
                {
                    Log.LogMessage($"Skipping '{inputPath}' because '{inputSource}' does not exist.");
                    return;
                }

                if (File.Exists(targetCompressionPath) && File.GetLastWriteTimeUtc(inputSource) < File.GetLastWriteTimeUtc(targetCompressionPath))
                {
                    // Incrementalism. If input source doesn't exist or it exists and is not newer than the expected output, do nothing.
                    Log.LogMessage($"Skipping '{inputPath}' because '{targetCompressionPath}' is newer than '{inputSource}'.");
                    return;
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetCompressionPath));

                    using var sourceStream = File.OpenRead(inputPath);
                    using var fileStream = new FileStream(targetCompressionPath, FileMode.Create);
                    using var stream = new GZipStream(fileStream, CompressionLevel.Optimal);

                    sourceStream.CopyTo(stream);
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e);
                    throw;
                }
            });

            return !Log.HasLoggedErrors;
        }

        [DataContract]
        private class ManifestData
        {
            [DataMember]
            public CompressedFile[] FilesToCompress { get; set; }
        }

        [DataContract]
        private class CompressedFile
        {
            [DataMember]
            public string Source { get; set; }

            [DataMember]
            public string InputSource { get; set; }

            [DataMember]
            public string Target { get; set; }
        }
    }
}

