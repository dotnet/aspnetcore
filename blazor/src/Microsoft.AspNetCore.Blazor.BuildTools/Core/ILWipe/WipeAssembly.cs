// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Mono.Cecil;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.BuildTools.Core.ILWipe
{
    static class WipeAssembly
    {
        public static void Exec(string inputPath, string outputPath, string specFilePath, bool logVerbose)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.ChangeExtension(inputPath, ".wiped" + Path.GetExtension(inputPath));
            }

            var specLines = File.ReadAllLines(specFilePath);
            var wipeSpecList = new SpecList(specLines);
            var moduleDefinition = ModuleDefinition.ReadModule(inputPath);

            if (!wipeSpecList.IsEmpty)
            {
                var createMethodWipedException = MethodWipedExceptionMethod.AddToAssembly(moduleDefinition);

                var contents = AssemblyItem.ListContents(moduleDefinition).ToList();
                foreach (var contentItem in contents)
                {
                    var shouldWipe = wipeSpecList.Match(contentItem)
                        && contentItem.Method != createMethodWipedException;

                    if (logVerbose)
                    {
                        Console.WriteLine($"{(shouldWipe ? "Wiping" : "Retaining")}: {contentItem}");
                    }

                    if (shouldWipe)
                    {
                        contentItem.WipeFromAssembly(createMethodWipedException);
                    }
                }
            }

            // Also resolve referenced assemblies in the same directory
            if (moduleDefinition.AssemblyResolver is DefaultAssemblyResolver resolver)
            {
                resolver.AddSearchDirectory(Path.GetDirectoryName(inputPath));
            }

            moduleDefinition.Write(outputPath);
        }
    }
}
