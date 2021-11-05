// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentLayoutDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        var @namespace = documentNode.FindPrimaryNamespace();
        var @class = documentNode.FindPrimaryClass();
        if (@namespace == null || @class == null)
        {
            return;
        }

        var directives = documentNode.FindDirectiveReferences(ComponentLayoutDirective.Directive);
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
            Content = $"[{ComponentsApi.LayoutAttribute.FullTypeName}(typeof({token.Content}))]",
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
