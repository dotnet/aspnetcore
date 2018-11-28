// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class ScriptTagPass : IntermediateNodePassBase, IRazorDocumentClassifierPass
    {
        // Run as soon as possible after the Component rewrite pass
        public override int Order => ComponentDocumentClassifierPass.DefaultFeatureOrder + 2;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (documentNode.DocumentKind != ComponentDocumentClassifierPass.ComponentDocumentKind)
            {
                return;
            }

            var visitor = new Visitor();
            visitor.Visit(documentNode);
        }

        private class Visitor : IntermediateNodeWalker, IExtensionIntermediateNodeVisitor<HtmlElementIntermediateNode>
        {
            public void VisitExtension(HtmlElementIntermediateNode node)
            {
                // Disallow <script> in components as per #552
                if (string.Equals(node.TagName, "script", StringComparison.OrdinalIgnoreCase))
                {
                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        // We allow you to suppress this error like:
                        // <script suppress-error="BL9992" />
                        var attribute = node.Children[i] as HtmlAttributeIntermediateNode;
                        if (attribute != null && 
                            attribute.AttributeName == "suppress-error" &&
                            attribute.Children.Count == 1 &&
                            attribute.Children[0] is HtmlAttributeValueIntermediateNode value &&
                            value.Children.Count == 1 &&
                            value.Children[0] is IntermediateToken token &&
                            token.IsHtml &&
                            string.Equals(token.Content, "BL9992", StringComparison.Ordinal))
                        {
                            node.Children.RemoveAt(i);
                            return;
                        }
                    }
                    
                    var diagnostic = BlazorDiagnosticFactory.Create_DisallowedScriptTag(node.Source);
                    node.Diagnostics.Add(diagnostic);
                }

                base.VisitDefault(node);
            }
        }
    }
}