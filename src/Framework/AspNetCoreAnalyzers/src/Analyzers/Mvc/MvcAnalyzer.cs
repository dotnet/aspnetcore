// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.Mvc;

using WellKnownType = WellKnownTypeData.WellKnownType;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class MvcAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.AmbiguousActionRoute,
        DiagnosticDescriptors.OverriddenAuthorizeAttribute
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);
            var routeUsageCache = RouteUsageCache.GetOrCreate(compilation);

            var concurrentQueue = new ConcurrentQueue<(List<ActionRoute> ActionRoutes, List<AttributeInfo> AuthorizeAttributes)>();

            context.RegisterSymbolAction(context =>
            {
                var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

                // Visit Controllers
                if (MvcDetector.IsController(namedTypeSymbol, wellKnownTypes))
                {
                    // Pool and reuse lists for each block.
                    if (!concurrentQueue.TryDequeue(out var pooledItems))
                    {
                        pooledItems.ActionRoutes = [];
                        pooledItems.AuthorizeAttributes = [];
                    }

                    DetectOverriddenAuthorizeAttributeOnController(context, wellKnownTypes, namedTypeSymbol, pooledItems.AuthorizeAttributes, out var allowAnonClass);
                    pooledItems.AuthorizeAttributes.Clear();

                    // Visit Actions
                    foreach (var member in namedTypeSymbol.GetMembers())
                    {
                        if (member is IMethodSymbol methodSymbol && MvcDetector.IsAction(methodSymbol, wellKnownTypes))
                        {
                            PopulateActionRoutes(context, wellKnownTypes, routeUsageCache, pooledItems.ActionRoutes, methodSymbol);
                            DetectOverriddenAuthorizeAttributeOnAction(context, wellKnownTypes, methodSymbol, pooledItems.AuthorizeAttributes, allowAnonClass);
                            pooledItems.AuthorizeAttributes.Clear();
                        }
                    }

                    RoutePatternTree? controllerRoutePattern = null;
                    var controllerRouteAttribute = namedTypeSymbol.GetAttributes(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_RouteAttribute), inherit: true).FirstOrDefault();
                    if (controllerRouteAttribute != null)
                    {
                        var routeUsage = GetRouteUsageModel(controllerRouteAttribute, routeUsageCache, context.CancellationToken);
                        if (routeUsage != null)
                        {
                            controllerRoutePattern = routeUsage.RoutePattern;
                        }
                    }

                    DetectAmbiguousActionRoutes(context, wellKnownTypes, controllerRoutePattern, pooledItems.ActionRoutes);

                    // Return to the pool.
                    pooledItems.ActionRoutes.Clear();
                    concurrentQueue.Enqueue(pooledItems);
                }
            }, SymbolKind.NamedType);
        });
    }

    private static void PopulateActionRoutes(SymbolAnalysisContext context, WellKnownTypes wellKnownTypes, RouteUsageCache routeUsageCache, List<ActionRoute> actionRoutes, IMethodSymbol methodSymbol)
    {
        // [Route("xxx")] attributes don't have a HTTP method and instead use the HTTP methods of other attributes.
        // For example, [HttpGet] + [HttpPost] + [Route("xxx")] means the route "xxx" is combined with the HTTP methods.
        var unroutedHttpMethods = GetUnroutedMethodHttpMethods(wellKnownTypes, methodSymbol);

        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (attribute.AttributeClass is null || !wellKnownTypes.IsType(attribute.AttributeClass, RouteAttributeTypes, out var match))
            {
                continue;
            }

            var routeUsage = GetRouteUsageModel(attribute, routeUsageCache, context.CancellationToken);
            if (routeUsage is null)
            {
                continue;
            }

            // [Route] uses unrouted HTTP verb attributes for its HTTP methods.
            var methods = match.Value is WellKnownType.Microsoft_AspNetCore_Mvc_RouteAttribute
                ? unroutedHttpMethods
                : ImmutableArray.Create(GetHttpMethod(match.Value)!);

            actionRoutes.Add(new ActionRoute(methodSymbol, routeUsage, methods));
        }
    }

    private static ImmutableArray<string> GetUnroutedMethodHttpMethods(WellKnownTypes wellKnownTypes, IMethodSymbol methodSymbol)
    {
        var httpMethodsBuilder = ImmutableArray.CreateBuilder<string>();
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (attribute.AttributeClass is null || !wellKnownTypes.IsType(attribute.AttributeClass, RouteAttributeTypes, out var match))
            {
                continue;
            }
            if (!attribute.ConstructorArguments.IsEmpty)
            {
                continue;
            }

            if (GetHttpMethod(match.Value) is { } method)
            {
                httpMethodsBuilder.Add(method);
            }
        }

        return httpMethodsBuilder.ToImmutable();
    }

    private static string? GetHttpMethod(WellKnownType match)
    {
        return match switch
        {
            WellKnownType.Microsoft_AspNetCore_Mvc_RouteAttribute => null,// No HTTP method.
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpDeleteAttribute => "DELETE",
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpGetAttribute => "GET",
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpHeadAttribute => "HEAD",
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpOptionsAttribute => "OPTIONS",
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpPatchAttribute => "PATCH",
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpPostAttribute => "POST",
            WellKnownType.Microsoft_AspNetCore_Mvc_HttpPutAttribute => "PUT",
            _ => throw new InvalidOperationException("Unexpected well known type:" + match),
        };
    }

    private static readonly WellKnownType[] RouteAttributeTypes = new[]
    {
        WellKnownType.Microsoft_AspNetCore_Mvc_RouteAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpDeleteAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpGetAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpHeadAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpOptionsAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpPatchAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpPostAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpPutAttribute
    };

    private static RouteUsageModel? GetRouteUsageModel(AttributeData attribute, RouteUsageCache routeUsageCache, CancellationToken cancellationToken)
    {
        if (attribute.ConstructorArguments.IsEmpty || attribute.ApplicationSyntaxReference is null)
        {
            return null;
        }

        if (attribute.ApplicationSyntaxReference.GetSyntax(cancellationToken) is AttributeSyntax attributeSyntax &&
            attributeSyntax.ArgumentList is { } argumentList)
        {
            var attributeArgument = argumentList.Arguments[0];
            if (attributeArgument.Expression is LiteralExpressionSyntax literalExpression)
            {
                return routeUsageCache.Get(literalExpression.Token, cancellationToken);
            }
        }

        return null;
    }

    private record struct ActionRoute(IMethodSymbol ActionSymbol, RouteUsageModel RouteUsageModel, ImmutableArray<string> HttpMethods);
    private record struct AttributeInfo(AttributeData AttributeData, bool IsTargetingMethod);
}
