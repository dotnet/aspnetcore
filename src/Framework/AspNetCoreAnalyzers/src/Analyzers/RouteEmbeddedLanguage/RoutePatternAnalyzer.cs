// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
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
                    var routeParameterNames = new HashSet<string>(tree.RouteParameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

                    // Get method parameters, including properties on AsParameters objects.
                    var parameterSymbols = RoutePatternParametersDetector.GetParameterSymbols(usageContext.MethodSymbol);
                    var resolvedParameterSymbols = RoutePatternParametersDetector.ResolvedParameters(usageContext.MethodSymbol, wellKnownTypes);
                    
                    foreach (var parameter in resolvedParameterSymbols)
                    {
                        routeParameterNames.Remove(parameter.Symbol.Name);
                    }

                    foreach (var unusedParameterName in routeParameterNames)
                    {
                        var unusedParameter = tree.GetRouteParameter(unusedParameterName);

                        var parameterInsertIndex = -1;
                        var insertPoint = CalculateInsertPoint(
                            unusedParameter.Name,
                            tree.RouteParameters,
                            resolvedParameterSymbols);
                        if (insertPoint is { } ip)
                        {
                            parameterInsertIndex = parameterSymbols.IndexOf(ip.ExistingParameter);
                            if (!ip.Before)
                            {
                                parameterInsertIndex++;
                            }
                        }

                        // These properties are used by the fixer.
                        var propertiesBuilder = ImmutableDictionary.CreateBuilder<string, string>();
                        propertiesBuilder.Add("RouteParameterName", unusedParameter.Name);
                        propertiesBuilder.Add("RouteParameterPolicy", string.Join(string.Empty, unusedParameter.Policies));
                        propertiesBuilder.Add("RouteParameterIsOptional", unusedParameter.IsOptional.ToString(CultureInfo.InvariantCulture));
                        propertiesBuilder.Add("RouteParameterInsertIndex", parameterInsertIndex.ToString(CultureInfo.InvariantCulture));

                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.RoutePatternUnusedParameter,
                            Location.Create(context.SemanticModel.SyntaxTree, unusedParameter.Span),
                            DiagnosticDescriptors.RoutePatternUnusedParameter.DefaultSeverity,
                            additionalLocations: null,
                            properties: propertiesBuilder.ToImmutableDictionary(),
                            unusedParameterName));
                    }
                }
            }
        }
    }

    private record struct InsertPoint(ISymbol ExistingParameter, bool Before);

    private static InsertPoint? CalculateInsertPoint(string routeParameterName, ImmutableArray<RouteParameter> routeParameters, ImmutableArray<ParameterSymbol> resolvedParameterSymbols)
    {
        InsertPoint? insertPoint = null;
        var seenRouteParameterName = false;
        for (var i = 0; i < routeParameters.Length; i++)
        {
            var routeParameter = routeParameters[i];
            if (string.Equals(routeParameter.Name, routeParameterName, StringComparison.OrdinalIgnoreCase))
            {
                if (insertPoint != null)
                {
                    break;
                }

                seenRouteParameterName = true;
                continue;
            }

            var parameterSymbol = resolvedParameterSymbols.FirstOrDefault(s => string.Equals(s.Symbol.Name, routeParameter.Name, StringComparison.OrdinalIgnoreCase));
            if (parameterSymbol.Symbol != null)
            {
                var s = parameterSymbol.TopLevelSymbol ?? parameterSymbol.Symbol;

                if (!seenRouteParameterName)
                {
                    insertPoint = new InsertPoint(s, Before: false);
                }
                else
                {
                    insertPoint = new InsertPoint(s, Before: true);
                    break;
                }
            }
        }

        return insertPoint;
    }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSemanticModelAction(Analyze);
    }
}
