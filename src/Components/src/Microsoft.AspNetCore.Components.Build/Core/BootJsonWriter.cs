// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Build
{
    internal class BootJsonWriter
    {
        public static void WriteFile(
            string assemblyPath,
            string[] assemblyReferences,
            string[] embeddedResourcesSources,
            bool linkerEnabled,
            string outputPath)
        {
            var embeddedContent = EmbeddedResourcesProcessor.ExtractEmbeddedResources(
                embeddedResourcesSources, Path.GetDirectoryName(outputPath));
            var bootJsonText = GetBootJsonContent(
                Path.GetFileName(assemblyPath),
                GetAssemblyEntryPoint(assemblyPath),
                assemblyReferences,
                embeddedContent,
                linkerEnabled);
            var normalizedOutputPath = Path.GetFullPath(outputPath);
            Console.WriteLine("Writing boot data to: " + normalizedOutputPath);
            File.WriteAllText(normalizedOutputPath, bootJsonText);
        }

        public static string GetBootJsonContent(string assemblyFileName, string entryPoint, string[] assemblyReferences, IEnumerable<EmbeddedResourceInfo> embeddedContent, bool linkerEnabled)
        {
            var data = new BootJsonData(
                assemblyFileName,
                entryPoint,
                assemblyReferences,
                embeddedContent,
                linkerEnabled);
            return Json.Serialize(data);
        }

        private static string GetAssemblyEntryPoint(string assemblyPath)
        {
            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                var entryPoint = assemblyDefinition.EntryPoint;
                if (entryPoint == null)
                {
                    throw new ArgumentException($"The assembly at {assemblyPath} has no specified entry point.");
                }

                return $"{entryPoint.DeclaringType.FullName}::{entryPoint.Name}";
            }
        }

        /// <summary>
        /// Defines the structure of a Blazor boot JSON file
        /// </summary>
        class BootJsonData
        {
            public string Main { get; }
            public string EntryPoint { get; }
            public IEnumerable<string> AssemblyReferences { get; }
            public IEnumerable<string> CssReferences { get; }
            public IEnumerable<string> JsReferences { get; }
            public bool LinkerEnabled { get; }

            public BootJsonData(
                string entrypointAssemblyWithExtension,
                string entryPoint,
                IEnumerable<string> assemblyReferences,
                IEnumerable<EmbeddedResourceInfo> embeddedContent,
                bool linkerEnabled)
            {
                Main = entrypointAssemblyWithExtension;
                EntryPoint = entryPoint;
                AssemblyReferences = assemblyReferences;
                LinkerEnabled = linkerEnabled;

                CssReferences = embeddedContent
                    .Where(c => c.Kind == EmbeddedResourceKind.Css)
                    .Select(c => c.RelativePath);

                JsReferences = embeddedContent
                    .Where(c => c.Kind == EmbeddedResourceKind.JavaScript)
                    .Select(c => c.RelativePath);
            }
        }
    }
}
