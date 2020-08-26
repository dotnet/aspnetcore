// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.BlazorWebAssembly
{
    public class BrotliCompress : ToolTask
    {
        private static readonly char[] InvalidPathChars = Path.GetInvalidFileNameChars();
        private string _dotnetPath;

        [Required]
        public ITaskItem[] FilesToCompress { get; set; }

        [Output]
        public ITaskItem[] CompressedFiles { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        public string CompressionLevel { get; set; }

        public bool SkipIfOutputIsNewer { get; set; }

        [Required]
        public string ToolAssembly { get; set; }

        protected override string ToolName => Path.GetDirectoryName(DotNetPath);

        private string DotNetPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_dotnetPath))
                {
                    return _dotnetPath;
                }

                _dotnetPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
                if (string.IsNullOrEmpty(_dotnetPath))
                {
                    throw new InvalidOperationException("DOTNET_HOST_PATH is not set");
                }

                return _dotnetPath;
            }
        }

        private static string Quote(string path)
        {
            if (string.IsNullOrEmpty(path) || (path[0] == '\"' && path[path.Length - 1] == '\"'))
            {
                // it's already quoted
                return path;
            }

            return $"\"{path}\"";
        }

        protected override string GenerateCommandLineCommands() => Quote(ToolAssembly);

        protected override string GenerateResponseFileCommands()
        {
            var builder = new StringBuilder();


            builder.AppendLine("brotli");

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
                var outputRelativePath = Path.Combine(OutputDirectory, CalculateTargetPath(inputFullPath, ".br"));
                var outputFullPath = Path.GetFullPath(outputRelativePath);

                var outputItem = new TaskItem(outputRelativePath);
                outputItem.SetMetadata("RelativePath", relativePath + ".br");
                CompressedFiles[i] = outputItem;

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

        internal static string CalculateTargetPath(string relativePath, string extension)
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

            builder.Append(extension);
            return builder.ToString();
        }

        protected override string GenerateFullPathToTool() => DotNetPath;
    }
}
