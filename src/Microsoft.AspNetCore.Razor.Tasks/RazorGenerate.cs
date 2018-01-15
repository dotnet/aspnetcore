// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class RazorGenerate : DotNetToolTask
    {
        [Required]
        public string[] Sources { get; set; }

        [Required]
        public string ProjectRoot { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string TagHelperManifest { get; set; }

        internal override RequestCommand Command => RequestCommand.RazorGenerate;

        protected override string GenerateResponseFileCommands()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < Sources.Length; i++)
            {
                builder.AppendLine(Sources[i]);
            }

            builder.AppendLine("-p");
            builder.AppendLine(ProjectRoot);

            builder.AppendLine("-o");
            builder.AppendLine(OutputPath);

            builder.AppendLine("-t");
            builder.AppendLine(TagHelperManifest);

            return builder.ToString();
        }
    }
}
