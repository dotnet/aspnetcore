// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.Startup;

internal sealed class ProblemDetailsWriterAnalyzer
{
    private readonly StartupAnalysis _context;

    public ProblemDetailsWriterAnalyzer(StartupAnalysis context)
    {
        _context = context;
    }

    public void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        Debug.Assert(context.Symbol.Kind == SymbolKind.NamedType);

        var type = (INamedTypeSymbol)context.Symbol;

        var serviceAnalyses = _context.GetRelatedAnalyses<ServicesAnalysis>(type);
        if (serviceAnalyses == null)
        {
            return;
        }

        foreach (var serviceAnalysis in serviceAnalyses)
        {
            var mvcServiceItems = serviceAnalysis.Services
                .Where(IsMvcServiceCollectionExtension)
                .ToArray();

            if (mvcServiceItems.Length == 0)
            {
                continue;
            }

            var problemDetailsWriterServiceItems = serviceAnalysis.Services
                .Where(IsProblemDetailsWriterRegistration)
                .ToArray();

            if (problemDetailsWriterServiceItems.Length == 0)
            {
                continue;
            }

            var mvcServiceTextSpans = mvcServiceItems.ToDictionary(x => x, x => x.Operation.Syntax.Span);

            foreach (var problemDetailsWriterServiceItem in problemDetailsWriterServiceItems)
            {
                var problemDetailsWriterServiceTextSpan = problemDetailsWriterServiceItem.Operation.Syntax.Span;

                foreach (var mvcServiceTextSpan in mvcServiceTextSpans)
                {
                    var mvcService = mvcServiceTextSpan.Key;
                    var textSpan = mvcServiceTextSpan.Value;

                    // Check if the IProblemDetailsWriter registration is after the MVC registration in the source.
                    if (problemDetailsWriterServiceTextSpan.CompareTo(textSpan) > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.IncorrectlyConfiguredProblemDetailsWriter,
                            problemDetailsWriterServiceItem.Operation.Syntax.GetLocation(),
                            additionalLocations: [mvcService.Operation.Syntax.GetLocation()]));

                        break;
                    }
                }
            }
        }
    }

    private static bool IsMvcServiceCollectionExtension(ServicesItem middlewareItem)
    {
        var methodName = middlewareItem.UseMethod.Name;

        if (string.Equals(methodName, SymbolNames.MvcServiceCollectionExtensions.AddControllersMethodName, StringComparison.Ordinal)
            || string.Equals(methodName, SymbolNames.MvcServiceCollectionExtensions.AddControllersWithViewsMethodName, StringComparison.Ordinal)
            || string.Equals(methodName, SymbolNames.MvcServiceCollectionExtensions.AddMvcMethodName, StringComparison.Ordinal)
            || string.Equals(methodName, SymbolNames.MvcServiceCollectionExtensions.AddRazorPagesMethodName, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static bool IsProblemDetailsWriterRegistration(ServicesItem servicesItem)
    {
        var methodName = servicesItem.UseMethod.Name;

        if (string.Equals(methodName, SymbolNames.ServiceCollectionServiceExtensions.AddTransientMethodName, StringComparison.Ordinal)
            || string.Equals(methodName, SymbolNames.ServiceCollectionServiceExtensions.AddScopedMethodName, StringComparison.Ordinal)
            || string.Equals(methodName, SymbolNames.ServiceCollectionServiceExtensions.AddSingletonMethodName, StringComparison.Ordinal))
        {
            var typeArguments = servicesItem.Operation.TargetMethod.TypeArguments;

            if (typeArguments.Length == 2
                && string.Equals(typeArguments[0].Name, SymbolNames.IProblemDetailsWriter.Name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
