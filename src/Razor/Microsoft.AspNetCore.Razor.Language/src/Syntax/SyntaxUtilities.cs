// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SyntaxUtilities
{
    public static MarkupTextLiteralSyntax MergeTextLiterals(params MarkupTextLiteralSyntax[] literalSyntaxes)
    {
        if (literalSyntaxes == null || literalSyntaxes.Length == 0)
        {
            return null;
        }

        SyntaxNode parent = null;
        var position = 0;
        var seenFirstLiteral = false;
        var builder = Syntax.InternalSyntax.SyntaxListBuilder.Create();

        foreach (var syntax in literalSyntaxes)
        {
            if (syntax == null)
            {
                continue;
            }
            else if (!seenFirstLiteral)
            {
                // Set the parent and position of the merged literal to the value of the first non-null literal.
                parent = syntax.Parent;
                position = syntax.Position;
                seenFirstLiteral = true;
            }

            foreach (var token in syntax.LiteralTokens)
            {
                builder.Add(token.Green);
            }
        }

        var mergedLiteralSyntax = Syntax.InternalSyntax.SyntaxFactory.MarkupTextLiteral(
            builder.ToList<Syntax.InternalSyntax.SyntaxToken>());

        return (MarkupTextLiteralSyntax)mergedLiteralSyntax.CreateRed(parent, position);
    }
}
