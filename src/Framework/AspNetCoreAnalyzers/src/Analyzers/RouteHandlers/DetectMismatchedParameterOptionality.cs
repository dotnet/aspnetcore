// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DetectMismatchedParameterOptionality(
        in OperationAnalysisContext context,
        IInvocationOperation invocation,
        IMethodSymbol methodSymbol)
    {
        if (invocation.Arguments.Length < 2)
        {
            return;
        }

        var value = invocation.Arguments[1].Value;
        if (value.ConstantValue is not { HasValue: true } constant ||
            constant.Value is not string routeTemplate)
        {
            return;
        }

        var allDeclarations = methodSymbol.GetAllMethodSymbolsOfPartialParts();
        foreach (var method in allDeclarations)
        {
            var parametersInArguments = method.Parameters;
            var enumerator = new RouteTokenEnumerator(routeTemplate);

            while (enumerator.MoveNext())
            {
                foreach (var parameter in parametersInArguments)
                {
                    var paramName = parameter.Name;
                    //  If this is not the methpd parameter associated with the route
                    // parameter then continue looking for it in the list
                    if (!enumerator.CurrentName.Equals(paramName.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var argumentIsOptional = parameter.IsOptional || parameter.NullableAnnotation != NullableAnnotation.NotAnnotated;
                    var location = parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();
                    var routeParamIsOptional = enumerator.CurrentQualifiers.IndexOf('?') > -1;

                    if (!argumentIsOptional && routeParamIsOptional)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DetectMismatchedParameterOptionality,
                            location,
                            paramName));
                    }
                }
            }
        }
    }

    internal ref struct RouteTokenEnumerator
    {
        private ReadOnlySpan<char> _routeTemplate;

        public RouteTokenEnumerator(string routeTemplateString)
        {
            _routeTemplate = routeTemplateString.AsSpan();
            CurrentName = default;
            CurrentQualifiers = default;
        }

        public ReadOnlySpan<char> CurrentName { get; private set; }
        public ReadOnlySpan<char> CurrentQualifiers { get; private set; }

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
            var qualifier = token.IndexOfAny(new[] { ':', '=', '?' });
            CurrentName = qualifier == -1 ? token : token.Slice(0, qualifier);
            CurrentQualifiers = qualifier == -1 ? null : token.Slice(qualifier);

            _routeTemplate = _routeTemplate.Slice(endIndex + 1);
            return true;
        }
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
