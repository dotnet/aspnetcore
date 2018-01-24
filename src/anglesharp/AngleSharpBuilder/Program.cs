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
     */

    public static class Program
    {
        public static void Main()
        {
            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "dist");
            var angleSharpAssembly = Assembly.GetAssembly(typeof(HtmlParser));
            WriteWithInternalsVisibleTo(
                angleSharpAssembly,
                "Microsoft.AspNetCore.Blazor.Build",
                outputDir);
        }

        private static void WriteWithInternalsVisibleTo(Assembly assembly, string internalVisibleToArg, string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            var assemblyLocation = assembly.Location;
            var moduleDefinition = ModuleDefinition.ReadModule(assemblyLocation);

            var internalsVisibleToCtor = moduleDefinition.ImportReference(
                typeof(InternalsVisibleToAttribute).GetConstructor(new[] { typeof(string) }));

            var customAttribute = new CustomAttribute(internalsVisibleToCtor);
            customAttribute.ConstructorArguments.Add(
                new CustomAttributeArgument(moduleDefinition.TypeSystem.String, internalVisibleToArg));

            moduleDefinition.Assembly.CustomAttributes.Add(customAttribute);

            moduleDefinition.Write(Path.Combine(outputDir, Path.GetFileName(assemblyLocation)));
        }
    }
}
