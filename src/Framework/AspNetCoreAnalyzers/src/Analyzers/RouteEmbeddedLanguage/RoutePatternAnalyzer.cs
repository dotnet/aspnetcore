// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RoutePatternAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(new[]
    {
        DiagnosticDescriptors.RoutePatternIssue
    });

    public void Analyze(SemanticModelAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var cancellationToken = context.CancellationToken;

        var root = syntaxTree.GetRoot(cancellationToken);
        Analyze(context, root, cancellationToken);
    }

    private void Analyze(
        SemanticModelAnalysisContext context,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.IsNode)
            {
                Analyze(context, child.AsNode()!, cancellationToken);
            }
            else
            {
                var token = child.AsToken();
                if (!RouteStringSyntaxDetector.IsRouteStringSyntaxToken(token, context.SemanticModel, cancellationToken))
                {
                    continue;
                }

                var usageContext = RoutePatternUsageDetector.BuildContext(token, context.SemanticModel, cancellationToken);

                var virtualChars = AspNetCoreCSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
                var tree = RoutePatternParser.TryParse(virtualChars, supportTokenReplacement: usageContext.IsMvcAttribute);
                if (tree == null)
                {
                    continue;
                }

                foreach (var diag in tree.Diagnostics)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.RoutePatternIssue,
                        Location.Create(context.SemanticModel.SyntaxTree, diag.Span),
                        DiagnosticDescriptors.RoutePatternIssue.DefaultSeverity,
                        additionalLocations: null,
                        properties: null,
                        diag.Message));
                }
            }
        }
    }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSemanticModelAction(Analyze);
    }
}
