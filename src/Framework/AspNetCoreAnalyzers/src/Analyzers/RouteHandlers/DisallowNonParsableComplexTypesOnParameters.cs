// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DisallowNonParsableComplexTypesOnParameters(
        in OperationAnalysisContext context,
        IInvocationOperation invocation,
        IMethodSymbol methodSymbol)
    {
        var routePatternArgument = invocation.Arguments[1];

        if (routePatternArgument.Syntax is not ArgumentSyntax routePatternArgumentSyntax ||
            routePatternArgumentSyntax.Expression is not LiteralExpressionSyntax routePatternArgumentLiteralSyntax)
        {
            return;
        }

        var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(routePatternArgumentLiteralSyntax.Token);
        var parser = RoutePatternParser.TryParse(virtualChars, false);

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

            // If the parameter is one of the special request delegate types we can skip it.
            if (wellKnownTypes.IsType(parameterTypeSymbol, RouteWellKnownTypes.ParameterSpecialTypes))
            {
                continue;
            }

            var syntax = (ParameterSyntax)handlerDelegateParameter.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
            var location = syntax.GetLocation();

            // Match handler parameter against route parameters. If it is a route parameter it needs to be parsable/bindable in some fashion.
            if (parser.TryGetRouteParameter(handlerDelegateParameter.Name, out var routeParameter))
            {
                if (!(ParsabilityHelper.IsTypeParsable(parameterTypeSymbol, wellKnownTypes) || ParsabilityHelper.IsTypeBindable(parameterTypeSymbol, wellKnownTypes)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
                        location,
                        routeParameter.Name,
                        parameterTypeSymbol.Name
                        ));
                }

                continue;
            }

            if (ReportFromAttributeDiagnostic(
                context,
                WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromHeaderMetadata,
                DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
                wellKnownTypes,
                handlerDelegateParameter,
                parameterTypeSymbol,
                location,
                ParsabilityHelper.IsTypeParsable
                )) { continue; }

            if (ReportFromAttributeDiagnostic(
                context,
                WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromQueryMetadata,
                DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
                wellKnownTypes,
                handlerDelegateParameter,
                parameterTypeSymbol,
                location,
                ParsabilityHelper.IsTypeParsable
                )) { continue; }
        }

        static bool ReportFromAttributeDiagnostic(OperationAnalysisContext context, WellKnownType fromMetadataInterfaceType, DiagnosticDescriptor descriptor, WellKnownTypes wellKnownTypes, IParameterSymbol parameter, INamedTypeSymbol parameterTypeSymbol, Location location, Func<ITypeSymbol, WellKnownTypes, bool> check)
        {
            var fromMetadataInterfaceTypeSymbol = wellKnownTypes.Get(fromMetadataInterfaceType);
            if (parameter.GetAttributes().Any(ad => ad.AttributeClass.Implements(fromMetadataInterfaceTypeSymbol)) &&
                !check(parameterTypeSymbol, wellKnownTypes))
            {
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

        static INamedTypeSymbol ResovleParameterTypeSymbol(IParameterSymbol parameterSymbol)
        {
            INamedTypeSymbol parameterTypeSymbol = null;

            // If it is an array, unwrap it.
            if (parameterSymbol.Type is IArrayTypeSymbol arrayTypeSymbol)
            {
                parameterTypeSymbol = arrayTypeSymbol.ElementType as INamedTypeSymbol;
            }
            else
            {
                parameterTypeSymbol = parameterSymbol.Type as INamedTypeSymbol;
            }

            // If it is nullable, unwrap it.
            if (parameterTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                parameterTypeSymbol = parameterTypeSymbol.TypeArguments[0] as INamedTypeSymbol;
            }

            return parameterTypeSymbol;
        }
    }
}
