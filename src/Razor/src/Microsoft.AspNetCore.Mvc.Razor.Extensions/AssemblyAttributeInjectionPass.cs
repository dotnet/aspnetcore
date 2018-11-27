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
            if (documentNode.Options.DesignTime)
            {
                return;
            }

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

            // The MVC attributes require a relative path to be specified so that we can make a view engine path.
            // We can't use a rooted path because we don't know what the project root is.
            //
            // If we can't sanitize the path, we'll just set it to null and let is blow up at runtime - we don't
            // want to create noise if this code has to run in some unanticipated scenario.
            var escapedPath = MakeVerbatimStringLiteral(ConvertToViewEnginePath(codeDocument.Source.RelativePath));

            string attribute;
            if (documentNode.DocumentKind == MvcViewDocumentClassifierPass.MvcViewDocumentKind)
            {
                attribute = $"[assembly:{RazorViewAttribute}({escapedPath}, typeof({generatedTypeName}))]";
            }
            else if (documentNode.DocumentKind == RazorPageDocumentClassifierPass.RazorPageDocumentKind &&
                PageDirective.TryGetPageDirective(documentNode, out var pageDirective))
            {
                var escapedRoutePrefix = MakeVerbatimStringLiteral(pageDirective.RouteTemplate);
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

        private static string MakeVerbatimStringLiteral(string value)
        {
            if (value == null)
            {
                return "null";
            }

            value = value.Replace("\"", "\"\"");
            return $"@\"{value}\"";
        }
        
        private static string ConvertToViewEnginePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return null;
            }

            // Checking for both / and \ because a \ will become a /.
            if (!relativePath.StartsWith("/") && !relativePath.StartsWith("\\"))
            {
                relativePath = "/" + relativePath;
            }
            
            relativePath = relativePath.Replace('\\', '/');
            return relativePath;
        }
    }
}
