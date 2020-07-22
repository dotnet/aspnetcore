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
    public class ConcatenateCssFiles : DotNetToolTask
    {
        [Required]
        public ITaskItem[] FilesToProcess { get; set; }

        [Required]
        public string OutputFile { get; set; }

        internal override string Command => "concatenatecss";

        protected override string GenerateResponseFileCommands()
        {
            var builder = new StringBuilder();

            builder.AppendLine(Command);


            for (var i = 0; i < FilesToProcess.Length; i++)
            {
                var input = FilesToProcess[i];
                var inputFullPath = input.GetMetadata("FullPath");

                builder.AppendLine("-s");
                builder.AppendLine(inputFullPath);

            }

            builder.AppendLine("-o");
            builder.AppendLine(OutputFile);
            return builder.ToString();
        }
    }
}
