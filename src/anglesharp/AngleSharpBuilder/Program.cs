// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AngleSharp.Parser.Html;
using Mono.Cecil;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AngleSharpBuilder
{
    /*
     * AngleSharp's HtmlTokenizer is perfect for the RazorCompiler use case of splitting up
     * incomplete HTML strings into partial elements, attributes, etc. Unfortunately,
     * AngleSharp does not expose HtmlTokenizer publicly.
     * 
     * For now, we work around this by building a custom version of AngleSharp.dll that
     * specifies [InternalsVisibleTo("Microsoft.AspNetCore.Blazor.Build")]. Longer term we can ask
     * AngleSharp to expose HtmlTokenizer as a public API, and if that's not viable, possibly
     * replace AngleSharp with a different library for HTML tokenization.
     * 
     * Similarly, we have build-process reasons for needing the assembly not to be strong
     * named and be called Microsoft.AspNetCore.Blazor.AngleSharp. These requirements will
     * not be permanent but it enables progress in the short term.
     */

    public static class Program
    {
        public static void Main()
        {
            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "dist");
            var inputAssembly = Assembly.GetAssembly(typeof(HtmlParser));
            WriteModifiedAssembly(inputAssembly, outputDir);
        }

        private static void WriteModifiedAssembly(Assembly assembly, string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            var assemblyLocation = assembly.Location;
            var moduleDefinition = ModuleDefinition.ReadModule(assemblyLocation);

            AddInternalsVisibleTo(moduleDefinition, "Microsoft.AspNetCore.Blazor.Build");
            AddInternalsVisibleTo(moduleDefinition, "Microsoft.AspNetCore.Blazor.Razor.Extensions");
            RemoveStrongName(moduleDefinition);
            SetAssemblyName(moduleDefinition, "Microsoft.AspNetCore.Blazor.AngleSharp");

            // Try to minimize the chance of a parallel build reading the assembly in an
            // incomplete state by writing it to a temp location then moving the result.
            // There's still a race condition here, but hopefully this is enough to stop
            // it from surfacing in practice.

            var tempFilePath = Path.GetTempFileName();
            moduleDefinition.Write(tempFilePath);

            var outputPath = Path.Combine(outputDir, $"{moduleDefinition.Name}.dll");
            try
            {
                File.Delete(outputPath);
                File.Move(tempFilePath, outputPath);
            }
            finally
            {
                File.Delete(tempFilePath); // In case it didn't get moved above
            }
        }

        private static void SetAssemblyName(ModuleDefinition moduleDefinition, string name)
        {
            moduleDefinition.Name = name;
            moduleDefinition.Assembly.Name.Name = name;
        }

        private static void RemoveStrongName(ModuleDefinition moduleDefinition)
        {
            var assemblyName = moduleDefinition.Assembly.Name;
            assemblyName.HasPublicKey = false;
            assemblyName.PublicKey = new byte[0];
            moduleDefinition.Attributes &= ~ModuleAttributes.StrongNameSigned;
        }

        private static void AddInternalsVisibleTo(ModuleDefinition moduleDefinition, string internalVisibleToArg)
        {
            var internalsVisibleToCtor = moduleDefinition.ImportReference(
                typeof(InternalsVisibleToAttribute).GetConstructor(new[] { typeof(string) }));

            var customAttribute = new CustomAttribute(internalsVisibleToCtor);
            customAttribute.ConstructorArguments.Add(
                new CustomAttributeArgument(moduleDefinition.TypeSystem.String, internalVisibleToArg));

            moduleDefinition.Assembly.CustomAttributes.Add(customAttribute);
        }
    }
}
