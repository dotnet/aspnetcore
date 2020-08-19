// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class RewriteCss : DotNetToolTask
    {
        [Required]
        public ITaskItem[] FilesToTransform { get; set; }

        public bool SkipIfOutputIsNewer { get; set; } = true;

        internal override string Command => "rewritecss";

        protected override string GenerateResponseFileCommands()
        {
            var builder = new StringBuilder();

            builder.AppendLine(Command);

            for (var i = 0; i < FilesToTransform.Length; i++)
            {
                var input = FilesToTransform[i];
                var inputFullPath = input.GetMetadata("FullPath");
                var relativePath = input.GetMetadata("RelativePath");
                var cssScope = input.GetMetadata("CssScope");
                var outputPath = input.GetMetadata("OutputFile");

                if (SkipIfOutputIsNewer && File.Exists(outputPath) && File.GetLastWriteTimeUtc(inputFullPath) < File.GetLastWriteTimeUtc(outputPath))
                {
                    Log.LogMessage(MessageImportance.Low, $"Skipping scope transformation for '{input.ItemSpec}' because '{outputPath}' is newer than '{input.ItemSpec}'.");
                    continue;
                }

                builder.AppendLine("-s");
                builder.AppendLine(inputFullPath);

                builder.AppendLine("-o");
                builder.AppendLine(outputPath);

                // Create the directory for the output file in case it doesn't exist.
                // Its easier to do it here than on MSBuild. Alternatively the tool could have taken care of it.
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                builder.AppendLine("-c");
                builder.AppendLine(cssScope);
            }

            return builder.ToString();
        }

        internal static string CalculateTargetPath(string relativePath, string extension) =>
            Path.ChangeExtension(relativePath, $"{extension}{Path.GetExtension(relativePath)}");
    }
}
