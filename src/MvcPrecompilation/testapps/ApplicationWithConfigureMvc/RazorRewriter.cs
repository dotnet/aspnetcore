using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApplicationWithConfigureStartup
{
    public class RazorRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.Token.IsKind(SyntaxKind.StringLiteralToken))
            {
                return node.WithToken(SyntaxFactory.Literal(
                    node.Token.ValueText.Replace(Environment.NewLine, Environment.NewLine + "<br />")));
            }

            return node;
        }
    }
}
