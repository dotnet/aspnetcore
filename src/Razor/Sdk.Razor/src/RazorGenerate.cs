// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class RazorGenerate : DotNetToolTask
    {
        private static readonly string[] SourceRequiredMetadata = new string[]
        {
            FullPath,
            GeneratedOutput,
            TargetPath,
        };

        private const string GeneratedOutput = "GeneratedOutput";
        private const string TargetPath = "TargetPath";
        private const string FullPath = "FullPath";
        private const string Identity = "Identity";
        private const string AssemblyName = "AssemblyName";
        private const string AssemblyFilePath = "AssemblyFilePath";

        [Required]
        public string Version { get; set; }

        [Required]
        public ITaskItem[] Configuration { get; set; }

        [Required]
        public ITaskItem[] Extensions { get;  set; }

        [Required]
        public ITaskItem[] Sources { get; set; }

        [Required]
        public string ProjectRoot { get; set; }

        [Required]
        public string TagHelperManifest { get; set; }

        internal override string Command => "generate";

        protected override bool ValidateParameters()
        {
            if (!Directory.Exists(ProjectRoot))
            {
                Log.LogError("The specified project root directory {0} doesn't exist.", ProjectRoot);
                return false;
            }

            if (Configuration.Length == 0)
            {
                Log.LogError("The project {0} must provide a value for {1}.", ProjectRoot, nameof(Configuration));
                return false;
            }

            for (var i = 0; i < Sources.Length; i++)
            {
                if (!EnsureRequiredMetadata(Sources[i], FullPath) ||
                    !EnsureRequiredMetadata(Sources[i], GeneratedOutput) ||
                    !EnsureRequiredMetadata(Sources[i], TargetPath))
                {
                    Log.LogError("The Razor source item '{0}' is missing a required metadata entry. Required metadata are: '{1}'", Sources[i], SourceRequiredMetadata);
                    return false;
                }
            }

            for (var i = 0; i < Extensions.Length; i++)
            {
                if (!EnsureRequiredMetadata(Extensions[i], Identity) ||
                    !EnsureRequiredMetadata(Extensions[i], AssemblyName) ||
                    !EnsureRequiredMetadata(Extensions[i], AssemblyFilePath))
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

            builder.AppendLine("-v");
            builder.AppendLine(Version);

            builder.AppendLine("-c");
            builder.AppendLine(Configuration[0].GetMetadata(Identity));

            for (var i = 0; i < Extensions.Length; i++)
            {
                builder.AppendLine("-n");
                builder.AppendLine(Extensions[i].GetMetadata(Identity));

                builder.AppendLine("-e");
                builder.AppendLine(Path.GetFullPath(Extensions[i].GetMetadata(AssemblyFilePath)));
            }

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
