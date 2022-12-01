// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            var fromServicesAttributeTypeSymbol = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_FromServicesAttribute);
            if (handlerDelegateParameter.HasAttribute(fromServicesAttributeTypeSymbol))
            {
                continue;
            }

            var parameterTypeSymbol = ResovleParameterTypeSymbol(handlerDelegateParameter);

            // If the parameter is one of the special request delegate types we can skip it.
            if (wellKnownTypes.IsType(parameterTypeSymbol, RouteWellKnownTypes.ParameterSpecialTypes))
            { 
                continue;
            }

            // Match handler parameter against route parameters. If it is a route parameter it needs to be parsable/bindable in some fashion.
            if (parser.TryGetRouteParameter(handlerDelegateParameter.Name, out RouteParameter routeParameter))
            {
                if (!(ParsabilityHelper.IsTypeParsable(parameterTypeSymbol, wellKnownTypes) || ParsabilityHelper.IsTypeBindable(parameterTypeSymbol, wellKnownTypes)))
                {
                    var syntax = (ParameterSyntax)handlerDelegateParameter.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
                        syntax.GetLocation(),
                        routeParameter.Name,
                        parameterTypeSymbol.Name
                        ));
                }

                continue;
            }

            var fromHeaderAttributeTypeSymbol = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_FromHeaderAttribute);
            if (handlerDelegateParameter.HasAttribute(fromHeaderAttributeTypeSymbol) && !ParsabilityHelper.IsTypeParsable(parameterTypeSymbol, wellKnownTypes))
            {
                var syntax = (ParameterSyntax)handlerDelegateParameter.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
                    syntax.GetLocation(),
                    routeParameter.Name,
                    parameterTypeSymbol.Name
                    ));

                continue;
            }

            var fromQueryAttributeTypeSymbol = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_FromQueryAttribute);
            if (handlerDelegateParameter.HasAttribute(fromQueryAttributeTypeSymbol) && !ParsabilityHelper.IsTypeParsable(parameterTypeSymbol, wellKnownTypes))
            {
                var syntax = (ParameterSyntax)handlerDelegateParameter.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.RouteParameterComplexTypeIsNotParsable,
                    syntax.GetLocation(),
                    handlerDelegateParameter.Name,
                    parameterTypeSymbol.Name
                    ));

                continue;
            }

            var fromBodyAttributeTypeSymbol = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Mvc_FromBodyAttribute);
            if (handlerDelegateParameter.HasAttribute(fromBodyAttributeTypeSymbol) && !ParsabilityHelper.IsTypeBindable(parameterTypeSymbol, wellKnownTypes))
            {
                var syntax = (ParameterSyntax)handlerDelegateParameter.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.RouteHandlerParamterComplexTypeIsNotBindable,
                    syntax.GetLocation(),
                    handlerDelegateParameter.Name,
                    parameterTypeSymbol.Name
                    ));

                continue;
            }
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
