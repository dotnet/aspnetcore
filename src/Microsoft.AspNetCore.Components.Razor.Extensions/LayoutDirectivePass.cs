// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class LayoutDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                return;
            }

            var directives = documentNode.FindDirectiveReferences(LayoutDirective.Directive);
            if (directives.Count == 0)
            {
                return;
            }

            var token = ((DirectiveIntermediateNode)directives[0].Node).Tokens.FirstOrDefault();
            if (token == null)
            {
                return;
            }

            var attributeNode = new CSharpCodeIntermediateNode();
            attributeNode.Children.Add(new IntermediateToken()
            {
                Kind = TokenKind.CSharp,
                Content = $"[{ComponentsApi.LayoutAttribute.FullTypeName}(typeof({token.Content}))]" + Environment.NewLine,
            });
            
            // Insert the new attribute on top of the class
            for (var i = 0; i < @namespace.Children.Count; i++)
            {
                if (object.ReferenceEquals(@namespace.Children[i], @class))
                {
                    @namespace.Children.Insert(i, attributeNode);
                    break;
                }
            }
        }
    }
}
