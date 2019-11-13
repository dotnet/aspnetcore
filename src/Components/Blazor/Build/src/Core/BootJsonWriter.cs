// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Blazor.Build
{
    internal class BootJsonWriter
    {
        public static void WriteFile(
            string assemblyPath,
            string[] assemblyReferences,
            bool linkerEnabled,
            string outputPath)
        {
            var bootJsonText = GetBootJsonContent(
                AssemblyName.GetAssemblyName(assemblyPath).Name,
                assemblyReferences,
                linkerEnabled);
            var normalizedOutputPath = Path.GetFullPath(outputPath);
            Console.WriteLine("Writing boot data to: " + normalizedOutputPath);
            File.WriteAllText(normalizedOutputPath, bootJsonText);
        }

        public static string GetBootJsonContent(string entryAssembly, string[] assemblyReferences, bool linkerEnabled)
        {
            var data = new BootJsonData(
                entryAssembly,
                assemblyReferences,
                linkerEnabled);
            return JsonSerializer.Serialize(data, JsonSerializerOptionsProvider.Options);
        }

        /// <summary>
        /// Defines the structure of a Blazor boot JSON file
        /// </summary>
        readonly struct BootJsonData
        {
            public string EntryAssembly { get; }
            public IEnumerable<string> AssemblyReferences { get; }
            public bool LinkerEnabled { get; }

            public BootJsonData(
                string entryAssembly,
                IEnumerable<string> assemblyReferences,
                bool linkerEnabled)
            {
                EntryAssembly = entryAssembly;
                AssemblyReferences = assemblyReferences;
                LinkerEnabled = linkerEnabled;
            }
        }
    }
}
