// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
{
    public static class GeneratorResultExtensions
    {
        public static string GetMainClassName(
            this GeneratorResults results,
            IMvcRazorHost host,
            SyntaxTree syntaxTree)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            // The mainClass name should return directly from the generator results.
            var classes = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            var mainClass = classes.FirstOrDefault(c =>
                c.Identifier.ValueText.StartsWith(host.MainClassNamePrefix, StringComparison.Ordinal));

            if (mainClass != null)
            {
                var typeName = mainClass.Identifier.ValueText;

                if (!string.IsNullOrEmpty(host.DefaultNamespace))
                {
                    typeName = host.DefaultNamespace + "." + typeName;
                }

                return typeName;
            }

            return null;
        }
    }
}
