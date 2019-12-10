// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.Build
{
    public class ValidateEmbeddedLinkerConfig : Task
    {
        [Required]
        public string AssemblyPath { get; set; }

        public override bool Execute()
        {
            var assembly = Assembly.LoadFrom(AssemblyPath);
            var expectedName = $"{assembly.GetName().Name}.xml";
            var embeddedConfig = assembly.GetManifestResourceStream(expectedName);
            if (embeddedConfig == null)
            {
               Log.LogError($"Could not find linker config resource '{expectedName}' inside assembly at path '{AssemblyPath}'");
               return false;
            }

            using var streamReader = new StreamReader(embeddedConfig);
            var actualConfig = streamReader.ReadToEnd();

            using var ms = new MemoryStream();
            LinkerConfigGenerator.Generate(assembly, ms);
            var expectedConfig = Encoding.UTF8.GetString(ms.ToArray());

            if (!actualConfig.Equals(expectedConfig, StringComparison.OrdinalIgnoreCase))
            {
                Log.LogError($"Embedded linker config is not up-to-date in assembly at path {AssemblyPath}");
                return false;
            }

            return true;
        }
    }
}
