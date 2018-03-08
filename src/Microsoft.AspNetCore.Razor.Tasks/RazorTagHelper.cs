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
    }
}
