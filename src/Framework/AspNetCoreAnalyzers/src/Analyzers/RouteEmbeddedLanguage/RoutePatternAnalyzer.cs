// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
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

    private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var cancellationToken = context.CancellationToken;

        var root = syntaxTree.GetRoot(cancellationToken);
        var routeUsageCache = RouteUsageCache.GetOrCreate(context.SemanticModel.Compilation);

        // Update to use FilterSpan when available. See https://github.com/dotnet/aspnetcore/issues/48157
        foreach (var item in root.DescendantTokens())
        {
            cancellationToken.ThrowIfCancellationRequested();

            AnalyzeToken(context, routeUsageCache, item, cancellationToken);
        }
    }

    private static void AnalyzeToken(SemanticModelAnalysisContext context, RouteUsageCache routeUsageCache, SyntaxToken token, CancellationToken cancellationToken)
    {
        if (!RouteStringSyntaxDetector.IsRouteStringSyntaxToken(token, context.SemanticModel, cancellationToken, out var options))
        {
            return;
        }

        var routeUsage = routeUsageCache.Get(token, cancellationToken);
        if (routeUsage is null)
        {
            return;
        }

        foreach (var diag in routeUsage.RoutePattern.Diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.RoutePatternIssue,
                Location.Create(context.SemanticModel.SyntaxTree, diag.Span),
                DiagnosticDescriptors.RoutePatternIssue.DefaultSeverity,
                additionalLocations: null,
                properties: null,
                diag.Message));
        }

        if (routeUsage.UsageContext.MethodSymbol != null)
        {
            var routeParameterNames = new HashSet<string>(routeUsage.RoutePattern.RouteParameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

            foreach (var parameter in routeUsage.UsageContext.ResolvedParameters)
            {
                routeParameterNames.Remove(parameter.RouteParameterName);
            }

            foreach (var unusedParameterName in routeParameterNames)
            {
                var unusedParameter = routeUsage.RoutePattern.GetRouteParameter(unusedParameterName);

                var parameterInsertIndex = -1;
                var insertPoint = CalculateInsertPoint(
                    unusedParameter.Name,
                    routeUsage.RoutePattern.RouteParameters,
                    routeUsage.UsageContext.ResolvedParameters);
                if (insertPoint is { } ip)
                {
                    parameterInsertIndex = routeUsage.UsageContext.Parameters.IndexOf(ip.ExistingParameter);
                    if (!ip.Before)
                    {
                        parameterInsertIndex++;
                    }
                }

                // These properties are used by the fixer.
                var propertiesBuilder = ImmutableDictionary.CreateBuilder<string, string?>();
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

            var parameterSymbol = resolvedParameterSymbols.FirstOrDefault(s => string.Equals(s.RouteParameterName, routeParameter.Name, StringComparison.OrdinalIgnoreCase));
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
        // Run on generated code to include routes specified in Razor files.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterSemanticModelAction(AnalyzeSemanticModel);
    }
}
