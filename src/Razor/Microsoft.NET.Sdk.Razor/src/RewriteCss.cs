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
    public class RewriteCss : DotNetToolTask
    {
        private static readonly char[] InvalidPathChars = Path.GetInvalidFileNameChars();

        static char[] Alphabet = new char[16]{ 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p' };

        [Required]
        public ITaskItem[] FilesToProcess { get; set; }

        [Output]
        public ITaskItem[] ProcessedFiles { get; set; }

        [Required]
        public string TargetName { get; set; }

        [Required]
        public string OutputDirectory { get; set; }

        public bool SkipIfOutputIsNewer { get; set; }

        internal override string Command => "rewritecss";

        protected override string GenerateResponseFileCommands()
        {
            var builder = new StringBuilder();

            builder.AppendLine(Command);


            ProcessedFiles = new ITaskItem[FilesToProcess.Length];

            for (var i = 0; i < FilesToProcess.Length; i++)
            {
                var input = FilesToProcess[i];
                var inputFullPath = input.GetMetadata("FullPath");
                var relativePath = input.GetMetadata("RelativePath");
                var scope = input.GetMetadata("CssScope");
                scope = !string.IsNullOrEmpty(scope) ? scope : GenerateScope(TargetName, relativePath);
                var outputRelativePath = Path.Combine(OutputDirectory, CalculateTargetPath(relativePath, scope, ".g.css"));

                var outputItem = new TaskItem(outputRelativePath);
                input.CopyMetadataTo(outputItem);
                scope ??= GenerateScope(TargetName, relativePath);
                outputItem.SetMetadata("RelativePath", relativePath + ".g.css");
                outputItem.SetMetadata("CssScope", scope);
                ProcessedFiles[i] = outputItem;

                var outputFullPath = Path.GetFullPath(outputRelativePath);

                if (SkipIfOutputIsNewer && File.Exists(outputFullPath) && File.GetLastWriteTimeUtc(inputFullPath) < File.GetLastWriteTimeUtc(outputFullPath))
                {
                    Log.LogMessage(MessageImportance.Low, $"Skipping scope transformation for '{input.ItemSpec}' because '{outputRelativePath}' is newer than '{input.ItemSpec}'.");
                    continue;
                }


                builder.AppendLine("-s");
                builder.AppendLine(inputFullPath);

                builder.AppendLine("-o");
                builder.AppendLine(outputFullPath);

                builder.AppendLine("-c");
                builder.AppendLine(scope);
            }

            return builder.ToString();
        }

        private string GenerateScope(string targetName, string relativePath)
        {
            using var hash = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(relativePath + targetName);
            var hashBytes = hash.ComputeHash(bytes);

            var builder = new StringBuilder();

            for (var i = 0; i < 4; i++)
            {
                var currentByte = hashBytes[i];
                builder.Append(Alphabet[currentByte & 0b00001111]);
                builder.Append(Alphabet[currentByte >> 2 & 0b00001111]);
                builder.Append(Alphabet[currentByte >> 4 & 0b00001111]);
                builder.Append(Alphabet[currentByte >> 6 & 0b00001111]);
            }

            return builder.ToString();
        }

        internal static string CalculateTargetPath(string relativePath, string scope, string extension)
        {
            // RelativePath can be long and if used as-is to write the output, might result in long path issues on Windows.
            // Instead we'll calculate a fixed length path by hashing the input file name. This uses SHA1 similar to the Hash task in MSBuild
            // since it has no crytographic significance.
            using var hash = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(relativePath + scope);
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
    }
}
