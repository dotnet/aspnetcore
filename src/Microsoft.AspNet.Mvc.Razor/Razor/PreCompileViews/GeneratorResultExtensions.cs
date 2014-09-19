using System;
using System.Linq;
using Microsoft.AspNet.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNet.Mvc.Razor
{
    public static class GeneratorResultExtensions
    {
        public static string GetMainClassName([NotNull] this GeneratorResults results,
                                              [NotNull] IMvcRazorHost host,
                                              [NotNull] SyntaxTree syntaxTree)
        {
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
