// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RoutePatternAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(new[]
    {
        DiagnosticDescriptors.RoutePatternIssue,
        DiagnosticDescriptors.RoutePatternUnusedParameter
    });

    public void Analyze(SemanticModelAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var cancellationToken = context.CancellationToken;

        var root = syntaxTree.GetRoot(cancellationToken);
        WellKnownTypes? wellKnownTypes = null;
        Analyze(context, root, ref wellKnownTypes, cancellationToken);
    }

    private void Analyze(
        SemanticModelAnalysisContext context,
        SyntaxNode node,
        ref WellKnownTypes? wellKnownTypes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.IsNode)
            {
                Analyze(context, child.AsNode()!, ref wellKnownTypes, cancellationToken);
            }
            else
            {
                var token = child.AsToken();
                if (!RouteStringSyntaxDetector.IsRouteStringSyntaxToken(token, context.SemanticModel, cancellationToken))
                {
                    continue;
                }

                if (wellKnownTypes == null && !WellKnownTypes.TryGetOrCreate(context.SemanticModel.Compilation, out wellKnownTypes))
                {
                    return;
                }

                var usageContext = RoutePatternUsageDetector.BuildContext(token, context.SemanticModel, wellKnownTypes, cancellationToken);

                var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
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

                if (usageContext.MethodSymbol != null)
                {
                    var routeParameterNames = new HashSet<string>(tree.RouteParameters.Keys, StringComparer.OrdinalIgnoreCase);
                    foreach (var parameter in usageContext.MethodSymbol.Parameters)
                    {
                        routeParameterNames.Remove(parameter.Name);
                    }

                    foreach (var unusedParameterName in routeParameterNames)
                    {
                        var unusedParameter = tree.RouteParameters[unusedParameterName];
                        var properties = new Dictionary<string, string>
                        {
                            ["RouteParameterName"] = unusedParameter.Name,
                            ["RouteParameterPolicy"] = string.Join(string.Empty, unusedParameter.Policies),
                            ["RouteParameterIsOptional"] = unusedParameter.IsOptional.ToString()
                        };

                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.RoutePatternUnusedParameter,
                            Location.Create(context.SemanticModel.SyntaxTree, unusedParameter.Span),
                            DiagnosticDescriptors.RoutePatternUnusedParameter.DefaultSeverity,
                            additionalLocations: null,
                            properties: properties.ToImmutableDictionary(),
                            unusedParameterName));
                    }
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
