// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RoutePatternAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(new[]
    {
        DiagnosticDescriptors.RoutePatternIssue,
        DiagnosticDescriptors.RoutePatternUnusedParameter,
        DiagnosticDescriptors.RoutePatternAddParameterConstraint
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

                // Add warnings from the route.
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

                // The route has an associated method, e.g. it's a route in an attribute on an MVC action or is in a Map method.
                // Analyze the route and method together to detect issues.
                if (usageContext.MethodSymbol != null)
                {
                    var routeParameterNames = new HashSet<string>(tree.RouteParameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

                    // Get method parameters, including properties on AsParameters objects.
                    var parameterSymbols = RoutePatternParametersDetector.GetParameterSymbols(usageContext.MethodSymbol);
                    var resolvedParameterSymbols = RoutePatternParametersDetector.ResolvedParameters(usageContext.MethodSymbol, wellKnownTypes);
                    
                    foreach (var parameter in resolvedParameterSymbols)
                    {
                        var parameterName = parameter.Symbol.Name;

                        if (routeParameterNames.Remove(parameterName))
                        {
                            var routeParameter = tree.GetRouteParameter(parameterName);
                            if (HasTypePolicy(routeParameter.Policies))
                            {
                                // Don't report route parameters that already have a type policy.
                                continue;
                            }

                            var policy = CalculatePolicyFromSymbol(parameter.Symbol, wellKnownTypes);
                            if (policy == null)
                            {
                                // Don't report when we can't calculate a policy.
                                // For example, the parameter has a type that doesn't map to a built-in policy.
                                continue;
                            }

                            var parameterSyntax = FindMethodParameter(usageContext, parameter, parameterName);

                            var propertiesBuilder = ImmutableDictionary.CreateBuilder<string, string>();
                            propertiesBuilder.Add("RouteParameterName", parameterName);
                            propertiesBuilder.Add("RouteParameterPolicy", policy);

                            // Add diagnostics with two locations:
                            // 1. The method parameter.
                            // 2. The route parameter.
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.RoutePatternAddParameterConstraint,
                                Location.Create(context.SemanticModel.SyntaxTree, parameterSyntax.Span),
                                DiagnosticDescriptors.RoutePatternAddParameterConstraint.DefaultSeverity,
                                additionalLocations: new List<Location> { Location.Create(context.SemanticModel.SyntaxTree, routeParameter.ParameterNode.GetSpan()) },
                                properties: propertiesBuilder.ToImmutableDictionary(),
                                parameterName));
                        }
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
                            Location.Create(context.SemanticModel.SyntaxTree, unusedParameter.ParameterNode.GetSpan()),
                            DiagnosticDescriptors.RoutePatternUnusedParameter.DefaultSeverity,
                            additionalLocations: null,
                            properties: propertiesBuilder.ToImmutableDictionary(),
                            unusedParameterName));
                    }
                }
            }
        }
    }

    private static ParameterSyntax FindMethodParameter(RoutePatternUsageContext usageContext, ParameterSymbol parameter, string parameterName)
    {
        var parameterList = usageContext.MethodSyntax switch
        {
            BaseMethodDeclarationSyntax methodSyntax => methodSyntax.ParameterList,
            ParenthesizedLambdaExpressionSyntax lambdaExpressionSyntax => lambdaExpressionSyntax.ParameterList,
            _ => throw new InvalidOperationException($"Unexpected method syntax: {usageContext.MethodSyntax.GetType().FullName}")
        };

        // In the case of properties from [AsParameters] types, we still want to resolve to the top level parameter.
        var topLevelParameterName = (parameter.TopLevelSymbol ?? parameter.Symbol).Name;

        foreach (var item in parameterList.Parameters)
        {
            if (string.Equals(item.Identifier.Text, topLevelParameterName, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        throw new InvalidOperationException($"Couldn't find {parameterName} in method syntax.");
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

    private static string? CalculatePolicyFromSymbol(ISymbol symbol, WellKnownTypes wellKnownTypes)
    {
        var parameterType = symbol switch
        {
            IParameterSymbol parameterSymbol => parameterSymbol.Type,
            IPropertySymbol propertySymbol => propertySymbol.Type,
            _ => throw new InvalidOperationException($"Unexpected parameter symbol type: {symbol.GetType().FullName}")
        };

        return CalculatePolicyFromType(parameterType, wellKnownTypes);

        static string? CalculatePolicyFromType(ITypeSymbol type, WellKnownTypes wellKnownTypes)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return "bool";
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                    return "int";
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    return "long";
                case SpecialType.System_Decimal:
                    return "decimal";
                case SpecialType.System_Single:
                    return "float";
                case SpecialType.System_Double:
                    return "double";
                case SpecialType.System_Nullable_T:
                    break;
                case SpecialType.System_DateTime:
                    return "datetime";
                default:
                    if (IsNullable(type, out var underlyingType))
                    {
                        return CalculatePolicyFromType(underlyingType, wellKnownTypes);
                    }
                    if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Guid))
                    {
                        return "guid";
                    }
                    break;
            }

            return null;
        }
    }

    public static bool IsNullable(ITypeSymbol symbol, [NotNullWhen(true)] out ITypeSymbol? underlyingType)
    {
        if (symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            underlyingType = ((INamedTypeSymbol)symbol).TypeArguments[0];
            return true;
        }

        underlyingType = null;
        return false;
    }

    private static bool HasTypePolicy(IImmutableList<string> routeParameterPolicy)
    {
        foreach (var policy in routeParameterPolicy)
        {
            var isTypePolicy = policy.TrimStart(':') switch
            {
                "int" => true,
                "long" => true,
                "bool" => true,
                "datetime" => true,
                "decimal" => true,
                "double" => true,
                "float" => true,
                "guid" => true,
                _ => false
            };

            if (isTypePolicy)
            {
                return true;
            }
        }

        return false;
    }

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSemanticModelAction(Analyze);
    }
}
