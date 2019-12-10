// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Microsoft.AspNetCore.Components.Build
{
    public class ValidateEmbeddedLinkerConfig : Task
    {
        [Required]
        public string AssemblyPath { get; set; }

        public override bool Execute()
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(AssemblyPath);
            var expectedName = $"{assemblyDefinition.Name.Name}.xml";
            var embeddedConfig = assemblyDefinition.MainModule.Resources.FirstOrDefault(r => r.Name.Equals(expectedName, StringComparison.Ordinal)) as EmbeddedResource;
            if (embeddedConfig == null)
            {
               Log.LogError($"Could not find linker config resource '{expectedName}' inside assembly at path '{AssemblyPath}'.\n\nTo fix this, run the build with /p:RegenerateLinkerConfig=true");
               return false;
            }

            var actualConfig = Encoding.UTF8.GetString(embeddedConfig.GetResourceData());
            var expectedConfig = LinkerConfigGenerator.Generate(AssemblyPath);

            if (!actualConfig.Equals(expectedConfig, StringComparison.OrdinalIgnoreCase))
            {
                Log.LogError($"Embedded linker config is not up-to-date in assembly at path '{AssemblyPath}'.\n\nTo fix this, run the build with /p:RegenerateLinkerConfig=true");
                return false;
            }

            return true;
        }
    }
}
