// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    internal class ManifestGenerator
    {
        public ManifestGenerator(
            CSharpCompiler compiler,
            CSharpCompilation compilation)
        {
            Compiler = compiler;
            Compilation = compilation;
        }

        public CSharpCompiler Compiler { get; }

        public CSharpCompilation Compilation { get; private set; }

        public void GenerateManifest(IList<ViewCompilationInfo> results)
        {
            var views = new List<ViewCompilationInfo>();
            var pages = new List<ViewCompilationInfo>();

            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                if (result.RouteTemplate != null)
                {
                    pages.Add(result);
                }
                else
                {
                    views.Add(result);
                }
            }

            GeneratePageManifest(pages);
            GenerateViewManifest(views);
        }

        private void GenerateViewManifest(IList<ViewCompilationInfo> result)
        {
            if (result.Count == 0)
            {
                return;
            }

            var precompiledViewsArray = new StringBuilder();
            foreach (var item in result)
            {
                var path = item.ViewFileInfo.ViewEnginePath;
                precompiledViewsArray.AppendLine(
                    $"new global::{typeof(ViewInfo).FullName}(@\"{path}\", typeof({item.TypeName})),");
            }

            var factoryContent = $@"
namespace {ViewsFeatureProvider.ViewInfoContainerNamespace}
{{
  public class {ViewsFeatureProvider.ViewInfoContainerTypeName} : global::{typeof(ViewInfoContainer).FullName}
  {{
    public {ViewsFeatureProvider.ViewInfoContainerTypeName}() : base(new[]
    {{
        {precompiledViewsArray}
    }})
    {{
    }}
  }}
}}";
            var syntaxTree = Compiler.CreateSyntaxTree(SourceText.From(factoryContent));
            Compilation = Compilation.AddSyntaxTrees(syntaxTree);
        }

        private void GeneratePageManifest(IList<ViewCompilationInfo> pages)
        {
            if (pages.Count == 0)
            {
                return;
            }

            var precompiledViewsArray = new StringBuilder();
            foreach (var item in pages)
            {
                var path = item.ViewFileInfo.ViewEnginePath;
                var routeTemplate = item.RouteTemplate;
                var escapedRouteTemplate = routeTemplate.Replace("\"", "\\\"");
                precompiledViewsArray.AppendLine(
                    $"new global::{typeof(CompiledPageInfo).FullName}(@\"{path}\", typeof({item.TypeName}), \"{escapedRouteTemplate}\"),");
            }

            var factoryContent = $@"
namespace {CompiledPageFeatureProvider.CompiledPageManifestNamespace}
{{
  public class {CompiledPageFeatureProvider.CompiledPageManifestTypeName} : global::{typeof(CompiledPageManifest).FullName}
  {{
    public {CompiledPageFeatureProvider.CompiledPageManifestTypeName}() : base(new[]
    {{
        {precompiledViewsArray}
    }})
    {{
    }}
  }}
}}";
            var syntaxTree = Compiler.CreateSyntaxTree(SourceText.From(factoryContent));
            Compilation = Compilation.AddSyntaxTrees(syntaxTree);
        }

        public void AddAssemblyMetadata(
            AssemblyName applicationAssemblyName,
            CompilationOptions compilationOptions)
        {
            if (!string.IsNullOrEmpty(compilationOptions.KeyFile))
            {
                var updatedOptions = Compilation.Options.WithStrongNameProvider(new DesktopStrongNameProvider());
                var keyFilePath = Path.GetFullPath(compilationOptions.KeyFile);

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || compilationOptions.PublicSign)
                {
                    updatedOptions = updatedOptions.WithCryptoPublicKey(
                        SnkUtils.ExtractPublicKey(File.ReadAllBytes(keyFilePath)));
                }
                else
                {
                    updatedOptions = updatedOptions.WithCryptoKeyFile(keyFilePath)
                        .WithDelaySign(compilationOptions.DelaySign);
                }

                Compilation = Compilation.WithOptions(updatedOptions);
            }

            var assemblyVersionContent = $"[assembly:{typeof(AssemblyVersionAttribute).FullName}(\"{applicationAssemblyName.Version}\")]";
            var syntaxTree = Compiler.CreateSyntaxTree(SourceText.From(assemblyVersionContent));
            Compilation = Compilation.AddSyntaxTrees(syntaxTree);
        }
    }
}
