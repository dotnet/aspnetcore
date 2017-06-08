// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    internal static class AssemblyMetadataGenerator
    {
        public static CSharpCompilation AddAssemblyMetadata(
            CSharpCompiler compiler,
            CSharpCompilation compilation,
            CompilationOptions compilationOptions)
        {
            if (!string.IsNullOrEmpty(compilationOptions.KeyFile))
            {
                var updatedOptions = compilation.Options.WithStrongNameProvider(new DesktopStrongNameProvider());
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

                compilation = compilation.WithOptions(updatedOptions);
            }

            var applicationAssemblyName = Assembly.Load(new AssemblyName(compilationOptions.ApplicationName)).GetName();
            var assemblyVersionContent = $"[assembly:{typeof(AssemblyVersionAttribute).FullName}(\"{applicationAssemblyName.Version}\")]";
            var syntaxTree = compiler.CreateSyntaxTree(SourceText.From(assemblyVersionContent));
            return compilation.AddSyntaxTrees(syntaxTree);
        }
    }
}
