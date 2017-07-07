// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class AssemblyAttributeInjectionPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        private const string RazorViewAttribute = "global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute";
        private const string RazorPageAttribute = "global::Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.RazorPageAttribute";

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            if (@namespace == null || string.IsNullOrEmpty(@namespace.Content))
            {
                // No namespace node or it's incomplete. Skip.
                return;
            }

            var @class = documentNode.FindPrimaryClass();
            if (@class == null || string.IsNullOrEmpty(@class.ClassName))
            {
                // No class node or it's incomplete. Skip.
                return;
            }

            var generatedTypeName = $"{@namespace.Content}.{@class.ClassName}";
            var path = codeDocument.GetRelativePath();
            var escapedPath = EscapeAsVerbatimLiteral(path);

            string attribute;
            if (documentNode.DocumentKind == MvcViewDocumentClassifierPass.MvcViewDocumentKind)
            {
                attribute = $"[assembly:{RazorViewAttribute}({escapedPath}, typeof({generatedTypeName}))]";
            }
            else if (documentNode.DocumentKind == RazorPageDocumentClassifierPass.RazorPageDocumentKind &&
                PageDirective.TryGetPageDirective(documentNode, out var pageDirective))
            {
                var escapedRoutePrefix = EscapeAsVerbatimLiteral(pageDirective.RouteTemplate);
                attribute = $"[assembly:{RazorPageAttribute}({escapedPath}, typeof({generatedTypeName}), {escapedRoutePrefix})]";
            }
            else
            {
                return;
            }

            var index = documentNode.Children.IndexOf(@namespace);
            Debug.Assert(index >= 0);

            var pageAttribute = new CSharpCodeIntermediateNode();
            pageAttribute.Children.Add(new IntermediateToken()
            {
                Kind = TokenKind.CSharp,
                Content = attribute,
            });

            documentNode.Children.Insert(index, pageAttribute);
        }

        private static string EscapeAsVerbatimLiteral(string value)
        {
            if (value == null)
            {
                return "null";
            }

            value = value.Replace("\"", "\"\"");
            return $"@\"{value}\"";
        }
    }
}
