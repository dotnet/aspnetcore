// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.MinimalActions;

public partial class MinimalActionAnalyzer : DiagnosticAnalyzer
{
    private static void RouteAttributeMismatch(
        in OperationAnalysisContext context,
        WellKnownTypes wellKnownTypes,
        IInvocationOperation mapInvocation,
        IMethodSymbol mapDelegate)
    {
        var value = mapInvocation.Arguments[1].Value;
        if (value.ConstantValue is not { HasValue: true } constant ||
            constant.Value is not string routeTemplate)
        {
            return;
        }

        var fromRouteParameters = GetFromRouteParameters(wellKnownTypes, mapDelegate.Parameters);

        var enumerator = new RouteTokenEnumerator(routeTemplate);

        while (enumerator.MoveNext())
        {
            var bound = false;
            for (var i = fromRouteParameters.Length - 1; i >= 0; i--)
            {
                if (enumerator.Current.Equals(fromRouteParameters[i].RouteName.AsSpan(), StringComparison.Ordinal))
                {
                    fromRouteParameters = fromRouteParameters.RemoveAt(i);
                    bound = true;
                }
            }

            if (!bound)
            {
                // If we didn't bind to an explicit FromRoute parameter, look for
                // an implicit one.
                foreach (var parameter in mapDelegate.Parameters)
                {
                    if (enumerator.Current.Equals(parameter.Name.AsSpan(), StringComparison.Ordinal))
                    {
                        bound = true;
                    }
                }
            }

            if (!bound)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.RouteValueIsUnused,
                    mapInvocation.Arguments[1].Syntax.GetLocation(),
                    enumerator.Current.ToString()));
            }
        }

        // Report diagnostics for any FromRoute parameter that does is not represented in the route template.
        foreach (var parameter in fromRouteParameters)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.RouteParameterCannotBeBound,
                parameter.Parameter.Locations.FirstOrDefault(),
                enumerator.Current.ToString()));
        }
    }

    private static ImmutableArray<(IParameterSymbol Parameter, string RouteName)> GetFromRouteParameters(
        WellKnownTypes wellKnownTypes, ImmutableArray<IParameterSymbol> parameters)
    {
        var result = ImmutableArray<(IParameterSymbol, string)>.Empty;
        foreach (var parameter in parameters)
        {
            var attribute = parameter.GetAttributes(wellKnownTypes.IFromRouteMetadata).FirstOrDefault();
            if (attribute is not null)
            {
                var fromRouteName = parameter.Name;
                var nameProperty = attribute.NamedArguments.FirstOrDefault(static f => f.Key == "Name");
                if (nameProperty.Value is { IsNull: false, Type: { SpecialType: SpecialType.System_String } })
                {
                    fromRouteName = (string)nameProperty.Value.Value;
                }

                result = result.Add((parameter, fromRouteName));
            }
        }

        return result;
    }

    internal ref struct RouteTokenEnumerator
    {
        private ReadOnlySpan<char> _routeTemplate;

        public RouteTokenEnumerator(string routeTemplateString)
        {
            _routeTemplate = routeTemplateString.AsSpan();
            Current = default;
        }

        public ReadOnlySpan<char> Current { get; private set; }

        public bool MoveNext()
        {
            if (_routeTemplate.IsEmpty)
            {
                return false;
            }

            findStartBrace:
            var startIndex = _routeTemplate.IndexOf('{');
            if (startIndex == -1)
            {
                return false;
            }

            if (startIndex < _routeTemplate.Length - 1 && _routeTemplate[startIndex + 1] == '{')
            {
                // Escaped sequence
                _routeTemplate = _routeTemplate.Slice(startIndex + 1);
                goto findStartBrace;
            }

            var tokenStart = startIndex + 1;

            findEndBrace:
            var endIndex = IndexOf(_routeTemplate, tokenStart, '}');
            if (endIndex == -1)
            {
                return false;
            }
            if (endIndex < _routeTemplate.Length - 1 && _routeTemplate[endIndex + 1] == '}')
            {
                tokenStart = endIndex + 2;
                goto findEndBrace;
            }

            var token = _routeTemplate.Slice(startIndex + 1, endIndex - startIndex - 1);
            var qualifier = token.IndexOfAny(new[] { ':', '=' });
            Current = qualifier == -1 ? token : token.Slice(0, qualifier);

            _routeTemplate = _routeTemplate.Slice(endIndex + 1);
            return true;
        }

        private static int IndexOf(ReadOnlySpan<char> span, int startIndex, char c)
        {
            for (var i = startIndex; i < span.Length; i++)
            {
                if (span[i] == c)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
