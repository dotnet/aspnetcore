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

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DisallowNonParsableComplexTypesOnParameters(
        in OperationAnalysisContext context,
        RouteUsageModel routeUsage,
        IMethodSymbol methodSymbol)
    {
        var wellKnownTypes = WellKnownTypes.GetOrCreate(context.Compilation);

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

            var routeParameterName = ResolveRouteParameterName(handlerDelegateParameter, wellKnownTypes);

            // Match handler parameter against route parameters. If it is a route parameter it needs to be parsable/bindable in some fashion.
            if (routeUsage.RoutePattern.TryGetRouteParameter(routeParameterName, out var routeParameter))
            {
                var parsability = ParsabilityHelper.GetParsability(parameterTypeSymbol, wellKnownTypes);
                var bindability = ParsabilityHelper.GetBindability(parameterTypeSymbol, wellKnownTypes);

                if (!(parsability == Parsability.Parsable || bindability == Bindability.Bindable))
                {
                    var descriptor = SelectDescriptor(parsability, bindability);

                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor,
                        location,
                        handlerDelegateParameter.Name,
                        parameterTypeSymbol.Name
                        ));
                }

                continue;
            }
        }

        static string ResolveRouteParameterName(IParameterSymbol parameterSymbol, WellKnownTypes wellKnownTypes)
        {
            if (!TryGetFromMetadataInterfaceTypeAttributeData(parameterSymbol, WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromRouteMetadata, wellKnownTypes, out var attributeData))
            {
                return parameterSymbol.Name; // No route metadata attribute!
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "Name")
                {
                    var routeParameterNameConstant = namedArgument.Value;
                    var routeParameterName = (string)routeParameterNameConstant.Value!;
                    return routeParameterName; // Have attribute & name is specified.
                }
            }

            return parameterSymbol.Name; // We have the attribute, but name isn't specified!
        }

        static bool TryGetFromMetadataInterfaceTypeAttributeData(IParameterSymbol parameterSymbol, WellKnownType fromMetadataInterfaceType, WellKnownTypes wellknowTypes, out AttributeData attributeData)
        {
            var fromMetadataInterfaceTypeSymbol = wellknowTypes.Get(fromMetadataInterfaceType);
            attributeData = parameterSymbol.GetAttributes().SingleOrDefault(ad => ad.AttributeClass.Implements(fromMetadataInterfaceTypeSymbol))!;
            return attributeData != null;
        }

        static bool HasAttributeImplementingFromMetadataInterfaceType(IParameterSymbol parameterSymbol, WellKnownType fromMetadataInterfaceType, WellKnownTypes wellKnownTypes)
        {
            return TryGetFromMetadataInterfaceTypeAttributeData(parameterSymbol, fromMetadataInterfaceType, wellKnownTypes, out var _);
        }

        static bool ReportFromAttributeDiagnostic(OperationAnalysisContext context, WellKnownType fromMetadataInterfaceType, WellKnownTypes wellKnownTypes, IParameterSymbol parameter, INamedTypeSymbol parameterTypeSymbol, Location location)
        {
            var parsability = ParsabilityHelper.GetParsability(parameterTypeSymbol, wellKnownTypes);
            if (HasAttributeImplementingFromMetadataInterfaceType(parameter, fromMetadataInterfaceType, wellKnownTypes) && parsability != Parsability.Parsable)
            {
                var descriptor = SelectDescriptor(parsability, Bindability.NotBindable);

                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor,
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

        static DiagnosticDescriptor SelectDescriptor(Parsability parsability, Bindability bindability)
        {
            // This abomination is used to take the parsability and bindability and together figure
            // out what the most optimal diagnostic message is to give to our plucky user.
            return (parsability, bindability) switch
            {
                { parsability: Parsability.NotParsable, bindability: Bindability.InvalidReturnType } => DiagnosticDescriptors.BindAsyncSignatureMustReturnValueTaskOfT,
                _ => DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsableOrBindable
            };
        }
    }
}
