// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.WebAssembly.Build
{
    public class GenerateBlazorCompressionManifest : Task
    {
        [Required]
        public ITaskItem[] FilesToCompress { get; set; }

        [Required]
        public string ManifestPath { get; set; }

        public override bool Execute()
        {
            try
            {
                WriteCompressionManifest();
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }

            return !Log.HasLoggedErrors;
        }

        private void WriteCompressionManifest()
        {
            var tempFilePath = Path.GetTempFileName();

            var manifest = new ManifestData();
            var filesToCompress = new List<CompressedFile>();

            foreach (var file in FilesToCompress)
            {
                filesToCompress.Add(new CompressedFile
                {
                    Source = file.GetMetadata("FullPath"),
                    InputSource = file.GetMetadata("InputSource"),
                    Target = file.GetMetadata("TargetCompressionPath"),
                });
            }

            manifest.FilesToCompress = filesToCompress.ToArray();

            var serializer = new DataContractJsonSerializer(typeof(ManifestData));

            using (var tempFile = File.OpenWrite(tempFilePath))
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(tempFile, Encoding.UTF8, ownsStream: false, indent: true))
                {
                    serializer.WriteObject(writer, manifest);
                }
            }

            if (!File.Exists(ManifestPath))
            {
                File.Move(tempFilePath, ManifestPath);
                return;
            }

            var originalText = File.ReadAllText(ManifestPath);
            var newManifest = File.ReadAllText(tempFilePath);
            if (!string.Equals(originalText, newManifest, StringComparison.Ordinal))
            {
                // OnlyWriteWhenDifferent
                File.Delete(ManifestPath);
                File.Move(tempFilePath, ManifestPath);
            }
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
