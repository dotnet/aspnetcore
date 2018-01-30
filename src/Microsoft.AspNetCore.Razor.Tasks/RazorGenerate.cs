// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class RazorGenerate : DotNetToolTask
    {
        private const string GeneratedOutput = "GeneratedOutput";
        private const string TargetPath = "TargetPath";
        private const string FullPath = "FullPath";

        [Required]
        public ITaskItem[] Sources { get; set; }

        [Required]
        public string ProjectRoot { get; set; }

        [Required]
        public string TagHelperManifest { get; set; }

        internal override string Command => "generate";

        protected override bool ValidateParameters()
        {
            for (var i = 0; i < Sources.Length; i++)
            {
                if (!EnsureRequiredMetadata(Sources[i], FullPath) ||
                    !EnsureRequiredMetadata(Sources[i], GeneratedOutput) ||
                    !EnsureRequiredMetadata(Sources[i], TargetPath))
                {
                    return false;
                }
            }

            return base.ValidateParameters();
        }

        protected override string GenerateResponseFileCommands()
        {
            var builder = new StringBuilder();

            builder.AppendLine(Command);

            for (var i = 0; i < Sources.Length; i++)
            {
                var input = Sources[i];
                builder.AppendLine("-s");
                builder.AppendLine(input.GetMetadata(FullPath));

                builder.AppendLine("-r");
                builder.AppendLine(input.GetMetadata(TargetPath));

                builder.AppendLine("-o");
                var outputPath = Path.Combine(ProjectRoot, input.GetMetadata(GeneratedOutput));
                builder.AppendLine(outputPath);
            }

            builder.AppendLine("-p");
            builder.AppendLine(ProjectRoot);

            builder.AppendLine("-t");
            builder.AppendLine(TagHelperManifest);

            return builder.ToString();
        }

        private bool EnsureRequiredMetadata(ITaskItem item, string metadataName)
        {
            var value = item.GetMetadata(metadataName);
            if (string.IsNullOrEmpty(value))
            {
                Log.LogError($"Missing required metadata '{metadataName}' for '{item.ItemSpec}.");
                return false;
            }

            return true;
        }
    }
}
