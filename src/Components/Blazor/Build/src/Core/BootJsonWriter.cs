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
            /// <summary>
            /// Gets the name of the assembly with the application entry point
            /// </summary>
            public string EntryAssembly { get; }

            /// <summary>
            /// Gets the closure of assemblies to be loaded by Blazor WASM. This includes the application entry assembly.
            /// </summary>
            public IEnumerable<string> Assemblies { get; }

            /// <summary>
            /// Gets a value that determines if the linker is enabled.
            /// </summary>
            public bool LinkerEnabled { get; }

            public BootJsonData(
                string entryAssembly,
                IEnumerable<string> assemblies,
                bool linkerEnabled)
            {
                EntryAssembly = entryAssembly;
                Assemblies = assemblies;
                LinkerEnabled = linkerEnabled;
            }
        }
    }
}
