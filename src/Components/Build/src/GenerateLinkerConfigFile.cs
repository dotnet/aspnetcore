// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.Build
{
    public class GenerateLinkerConfigFile : Task
    {
        [Required]
        public string AssemblyPath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            var config = LinkerConfigGenerator.Generate(AssemblyPath);
            File.WriteAllText(OutputPath, config, new UTF8Encoding(false));
            return true;
        }
    }
}
