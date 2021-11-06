// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal class AttributeDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        var @namespace = documentNode.FindPrimaryNamespace();
        var @class = documentNode.FindPrimaryClass();
        if (@namespace == null || @class == null)
        {
            return;
        }

        var classIndex = @namespace.Children.IndexOf(@class);
        foreach (var attribute in documentNode.FindDirectiveReferences(AttributeDirective.Directive))
        {
            var token = ((DirectiveIntermediateNode)attribute.Node).Tokens.FirstOrDefault();
            if (token != null)
            {
                var node = new CSharpCodeIntermediateNode
                {
                    Source = token.Source
                };

                node.Children.Add(new IntermediateToken()
                {
                    Content = token.Content,
                    Source = token.Source,
                    Kind = TokenKind.CSharp,
                });

                @namespace.Children.Insert(classIndex++, node);
            }
        }
    }
}
