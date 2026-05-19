// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using RoutePatternToken = Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax.EmbeddedSyntaxToken<Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern.RoutePatternKind>;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

[ExportAspNetCoreEmbeddedLanguageClassifier(name: "Route", language: LanguageNames.CSharp)]
internal sealed class RoutePatternClassifier : IAspNetCoreEmbeddedLanguageClassifier
{
    public void RegisterClassifications(AspNetCoreEmbeddedLanguageClassificationContext context)
    {
        var routeUsageCache = RouteUsageCache.GetOrCreate(context.SemanticModel.Compilation);
        var routeUsage = routeUsageCache.Get(context.SyntaxToken, context.CancellationToken);
        if (routeUsage is null)
        {
            return;
        }

        var visitor = new Visitor(context);
        AddClassifications(routeUsage.RoutePattern.Root, visitor);
    }

    private static void AddClassifications(RoutePatternNode node, Visitor visitor)
    {
        node.Accept(visitor);

        foreach (var child in node)
        {
            if (child.IsNode)
            {
                AddClassifications(child.Node, visitor);
            }
        }
    }

    private sealed class Visitor : IRoutePatternNodeVisitor
    {
        public AspNetCoreEmbeddedLanguageClassificationContext _context;

        public Visitor(AspNetCoreEmbeddedLanguageClassificationContext context)
        {
            _context = context;
        }

        public void Visit(RoutePatternCompilationUnit node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternSegmentNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternReplacementNode node)
        {
            AddClassification(node.OpenBracketToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.TextToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CloseBracketToken, ClassificationTypeNames.RegexCharacterClass);
        }

        public void Visit(RoutePatternParameterNode node)
        {
            AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexCharacterClass);
        }

        public void Visit(RoutePatternLiteralNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternSegmentSeparatorNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternOptionalSeparatorNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternCatchAllParameterPartNode node)
        {
            AddClassification(node.AsteriskToken, ClassificationTypeNames.RegexAnchor);
        }

        public void Visit(RoutePatternNameParameterPartNode node)
        {
            // Parameter name should look like a regular parameter.
            // ClassificationTypeNames.ParameterName isn't working so temporarily reuse color from JSON syntax:
            // https://github.com/dotnet/roslyn/blob/e1ee2f544a7a4f8d536278bed4e180c54919276e/src/Workspaces/Core/Portable/Classification/ClassificationTypeNames.cs#L181
            //
            // TODO: Fix properly with https://github.com/dotnet/aspnetcore/issues/46207
            AddClassification(node.ParameterNameToken, "json - object");
        }

        public void Visit(RoutePatternPolicyParameterPartNode node)
        {
            AddClassification(node.ColonToken, ClassificationTypeNames.RegexCharacterClass);
        }

        public void Visit(RoutePatternPolicyFragmentEscapedNode node)
        {
            AddClassification(node.OpenParenToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CloseParenToken, ClassificationTypeNames.RegexCharacterClass);
        }

        public void Visit(RoutePatternPolicyFragment node)
        {
            AddClassification(node.ArgumentToken, ClassificationTypeNames.RegexGrouping);
        }

        public void Visit(RoutePatternOptionalParameterPartNode node)
        {
            AddClassification(node.QuestionMarkToken, ClassificationTypeNames.RegexAnchor);
        }

        public void Visit(RoutePatternDefaultValueParameterPartNode node)
        {
            AddClassification(node.EqualsToken, ClassificationTypeNames.RegexCharacterClass);
        }

        private void AddClassification(RoutePatternToken token, string typeName)
        {
            if (!token.IsMissing)
            {
                _context.AddClassification(typeName, token.GetSpan());
            }
        }

        private void ClassifyWholeNode(RoutePatternNode node, string typeName)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    ClassifyWholeNode(child.Node, typeName);
                }
                else
                {
                    AddClassification(child.Token, typeName);
                }
            }
        }
    }

    // IAspNetCoreEmbeddedLanguageClassifier is internal and tests don't have access to it. Provide a way to get its assembly.
    // Just for unit tests. Don't use in production code.
    internal static class TestAccessor
    {
        public static Assembly ExternalAccessAssembly => typeof(IAspNetCoreEmbeddedLanguageClassifier).Assembly;
    }
}
