// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class BrotliCompress : DotNetToolTask
    {
        private static readonly char[] InvalidPathChars = Path.GetInvalidFileNameChars();

        [Required]
        public ITaskItem[] FilesToCompress { get; set; }

        [Output]
        public ITaskItem[] CompressedFiles { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        public string CompressionLevel { get; set; }

        public bool SkipIfOutputIsNewer { get; set; }

        internal override string Command => "brotli";

        protected override string GenerateResponseFileCommands()
        {
            var builder = new StringBuilder();

            builder.AppendLine(Command);

            if (!string.IsNullOrEmpty(CompressionLevel))
            {
                builder.AppendLine("-c");
                builder.AppendLine(CompressionLevel);
            }

            CompressedFiles = new ITaskItem[FilesToCompress.Length];

            for (var i = 0; i < FilesToCompress.Length; i++)
            {
                var input = FilesToCompress[i];
                var inputFullPath = input.GetMetadata("FullPath");
                var relativePath = input.GetMetadata("RelativePath");
                var outputRelativePath = Path.Combine(OutputDirectory, CalculateTargetPath(relativePath));

                var outputItem = new TaskItem(outputRelativePath);
                input.CopyMetadataTo(outputItem);
                // Relative path in the publish dir
                outputItem.SetMetadata("RelativePath", relativePath + ".br");
                CompressedFiles[i] = outputItem;

                var outputFullPath = Path.GetFullPath(outputRelativePath);

                if (SkipIfOutputIsNewer && File.Exists(outputFullPath) && File.GetLastWriteTimeUtc(inputFullPath) < File.GetLastWriteTimeUtc(outputFullPath))
                {
                    Log.LogMessage(MessageImportance.Low, $"Skipping compression for '{input.ItemSpec}' because '{outputRelativePath}' is newer than '{input.ItemSpec}'.");
                    continue;
                }

                builder.AppendLine("-s");
                builder.AppendLine(inputFullPath);

                builder.AppendLine("-o");
                builder.AppendLine(outputFullPath);
            }

            return builder.ToString();
        }

        private static string CalculateTargetPath(string relativePath)
        {
            // RelativePath can be long and if used as-is to write the output, might result in long path issues on Windows.
            // Instead we'll calculate a fixed length path by hashing the input file name. This uses SHA1 similar to the Hash task in MSBuild
            // since it has no crytographic significance.
            using var hash = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(relativePath);
            var hashString = Convert.ToBase64String(hash.ComputeHash(bytes));

            var builder = new StringBuilder();

            for (var i = 0; i < 8; i++)
            {
                var c = hashString[i];
                builder.Append(InvalidPathChars.Contains(c) ? '+' : c);
            }

            builder.Append(".br");
            return builder.ToString();
        }
    }
}
