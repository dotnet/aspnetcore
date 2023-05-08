// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

using WellKnownType = WellKnownTypeData.WellKnownType;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DisallowNonParsableComplexTypesOnParameters(
        in OperationAnalysisContext context,
        WellKnownTypes wellKnownTypes,
        RouteUsageModel routeUsage,
        IMethodSymbol methodSymbol)
    {
        foreach (var handlerDelegateParameter in methodSymbol.Parameters)
        {
            // If the parameter is decorated with a FromServices attribute then we can skip it.
            var fromServiceMetadataTypeSymbol = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata);
            if (handlerDelegateParameter.HasAttribute(fromServiceMetadataTypeSymbol))
            {
                continue;
            }

            var parameterTypeSymbol = ResovleParameterTypeSymbol(handlerDelegateParameter);

            // If this is null it means we aren't working with a named type symbol.
            if (parameterTypeSymbol == null)
            {
                continue;
            }

            // If the parameter is one of the special request delegate types we can skip it.
            if (wellKnownTypes.IsType(parameterTypeSymbol, RouteWellKnownTypes.ParameterSpecialTypes))
            {
                continue;
            }

            var syntax = (ParameterSyntax)handlerDelegateParameter.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
            var location = syntax.GetLocation();

            if (ReportFromAttributeDiagnostic(
                context,
                WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromHeaderMetadata,
                wellKnownTypes,
                handlerDelegateParameter,
                parameterTypeSymbol,
                location
            )) { continue; }

            if (ReportFromAttributeDiagnostic(
                context,
                WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromQueryMetadata,
                wellKnownTypes,
                handlerDelegateParameter,
                parameterTypeSymbol,
                location
                )) { continue; }

            if (IsRouteParameter(routeUsage, handlerDelegateParameter))
            {
                var parsability = ParsabilityHelper.GetParsability(parameterTypeSymbol, wellKnownTypes);

                if (parsability != Parsability.Parsable)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
                        location,
                        handlerDelegateParameter.Name,
                        parameterTypeSymbol.Name
                        ));
                }

                continue;
            }
        }

        static bool IsRouteParameter(RouteUsageModel routeUsage, IParameterSymbol handlerDelegateParameter)
        {
            // This gets the ParameterSymbol (concept from RouteEmbeddedLanguage) regardless of whether it is in
            // the route pattern or not. If it is and it has a custom [FromRoute(Name = "blah")] attribute then
            // RouteParameterName and we'll be able to find it by looking it up in the route pattern (thus confirming
            // that it is a route parameter).
            var resolvedParameter = routeUsage.UsageContext.ResolvedParameters.FirstOrDefault(rp => rp.Symbol.Name == handlerDelegateParameter.Name);
            var isRouteParameter = routeUsage.RoutePattern.TryGetRouteParameter(resolvedParameter.RouteParameterName, out var _);
            return isRouteParameter;
        }

        static bool ReportFromAttributeDiagnostic(OperationAnalysisContext context, WellKnownType fromMetadataInterfaceType, WellKnownTypes wellKnownTypes, IParameterSymbol parameter, INamedTypeSymbol parameterTypeSymbol, Location location)
        {
            var fromMetadataInterfaceTypeSymbol = wellKnownTypes.Get(fromMetadataInterfaceType);
            var parsability = ParsabilityHelper.GetParsability(parameterTypeSymbol, wellKnownTypes);
            if (parameter.HasAttributeImplementingInterface(fromMetadataInterfaceTypeSymbol) && parsability != Parsability.Parsable)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
                    location,
                    parameter.Name,
                    parameterTypeSymbol.Name
                    ));

                return true;
            }

            return false;
        }

        static INamedTypeSymbol? ResovleParameterTypeSymbol(IParameterSymbol parameterSymbol)
        {
            INamedTypeSymbol? parameterTypeSymbol = null;

            // If it is an array, unwrap it.
            if (parameterSymbol.Type is IArrayTypeSymbol arrayTypeSymbol)
            {
                parameterTypeSymbol = arrayTypeSymbol.ElementType as INamedTypeSymbol;
            }
            else if (parameterSymbol.Type is INamedTypeSymbol namedTypeSymbol)
            {
                parameterTypeSymbol = namedTypeSymbol;
            }

            // If it is nullable, unwrap it.
            if (parameterTypeSymbol!.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                parameterTypeSymbol = parameterTypeSymbol.TypeArguments[0] as INamedTypeSymbol;
            }

            return parameterTypeSymbol;
        }
    }
}
