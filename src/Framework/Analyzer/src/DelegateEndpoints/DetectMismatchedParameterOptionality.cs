// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints;

public partial class DelegateEndpointAnalyzer : DiagnosticAnalyzer
{
    internal const string DetectMismatchedParameterOptionalityRuleId = "ASP0006";

    private static void DetectMismatchedParameterOptionality(
        in OperationAnalysisContext context,
        IInvocationOperation invocation,
        IMethodSymbol methodSymbol)
    {
        var value = invocation.Arguments[1].Value;
        if (value.ConstantValue is not { HasValue: true } constant ||
            constant.Value is not string routeTemplate)
        {
            return;
        }

        var parametersInArguments = methodSymbol.Parameters;
        var parametersInRoute = GetParametersFromRoute(routeTemplate);

        foreach (var parameter in parametersInArguments)
        {
            var isOptional = parameter.IsOptional || parameter.NullableAnnotation != NullableAnnotation.NotAnnotated;
            var location = parameter.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax().GetLocation();
            var paramName = parameter.Name;
            var parameterFound = parametersInRoute.TryGetValue(paramName, out var routeParam);

            if (!isOptional && parameterFound && routeParam.IsOptional)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DetectMismatchedParameterOptionality,
                    location,
                    paramName));
            }
        }
    }

    private static IDictionary<string, RouteParameter> GetParametersFromRoute(string routeTemplate)
    {
        var enumerator = new RouteTokenEnumerator(routeTemplate);
        Dictionary<string, RouteParameter> result = new(StringComparer.OrdinalIgnoreCase);
        while (enumerator.MoveNext())
        {
            var isOptional = enumerator.CurrentQualifiers.IndexOf('?') > -1;
            result.Add(
                enumerator.CurrentName.ToString(),
                new RouteParameter(enumerator.CurrentName.ToString(), isOptional));
        }
        return result;
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

    internal record RouteParameter(string Name, bool IsOptional);
}