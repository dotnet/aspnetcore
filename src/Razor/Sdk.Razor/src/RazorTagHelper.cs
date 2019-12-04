// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class RazorTagHelper : DotNetToolTask
    {
        private const string Identity = "Identity";
        private const string AssemblyName = "AssemblyName";
        private const string AssemblyFilePath = "AssemblyFilePath";

        [Required]
        public string Version { get; set; }

        [Required]
        public ITaskItem[] Configuration { get; set; }

        [Required]
        public ITaskItem[] Extensions { get; set; }

        [Required]
        public string[] Assemblies { get; set; }

        [Output]
        [Required]
        public string TagHelperManifest { get; set; }

        public string ProjectRoot { get; set; }

        internal override string Command => "discover";

        protected override bool SkipTaskExecution()
        {
            if (Assemblies.Length == 0)
            {
                // If for some reason there are no assemblies, we have nothing to do. Let's just
                // skip running the tool.
                File.WriteAllText(TagHelperManifest, "{ }");
                return true;
            }

            return false;
        }

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

            for (var i = 0; i < Assemblies.Length; i++)
            {
                if (!Path.IsPathRooted(Assemblies[i]))
                {
                    Log.LogError("The assembly path {0} is invalid. Assembly paths must be rooted.", Assemblies[i]);
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

            for (var i = 0; i < Assemblies.Length; i++)
            {
                builder.AppendLine(Assemblies[i]);
            }

            builder.AppendLine("-o");
            builder.AppendLine(TagHelperManifest);

            builder.AppendLine("-p");
            builder.AppendLine(ProjectRoot);

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
