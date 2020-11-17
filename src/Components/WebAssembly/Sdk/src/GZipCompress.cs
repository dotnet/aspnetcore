// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.BlazorWebAssembly
{
    public class GZipCompress : Task
    {
        [Required]
        public ITaskItem[] FilesToCompress { get; set; }

        [Output]
        public ITaskItem[] CompressedFiles { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        public override bool Execute()
        {
            CompressedFiles = new ITaskItem[FilesToCompress.Length];

            Directory.CreateDirectory(OutputDirectory);

            System.Threading.Tasks.Parallel.For(0, FilesToCompress.Length, i =>
            {
                var file = FilesToCompress[i];
                var inputFullPath = file.GetMetadata("FullPath");
                var relativePath = file.GetMetadata("RelativePath");
                var outputRelativePath = Path.Combine(
                    OutputDirectory,
                    BrotliCompress.CalculateTargetPath(inputFullPath, ".gz"));

                var outputItem = new TaskItem(outputRelativePath);
                outputItem.SetMetadata("RelativePath", relativePath + ".gz");
                CompressedFiles[i] = outputItem;

                if (File.Exists(outputRelativePath) && File.GetLastWriteTimeUtc(inputFullPath) < File.GetLastWriteTimeUtc(outputRelativePath))
                {
                    // Incrementalism. If input source doesn't exist or it exists and is not newer than the expected output, do nothing.
                    Log.LogMessage(MessageImportance.Low, $"Skipping '{file.ItemSpec}' because '{outputRelativePath}' is newer than '{file.ItemSpec}'.");
                    return;
                }

                try
                {
                    using var sourceStream = File.OpenRead(file.ItemSpec);
                    using var fileStream = File.Create(outputRelativePath);
                    using var stream = new GZipStream(fileStream, CompressionLevel.Optimal);

                    sourceStream.CopyTo(stream);
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e);
                    return;
                }
            });

            return !Log.HasLoggedErrors;
        }
    }
}
