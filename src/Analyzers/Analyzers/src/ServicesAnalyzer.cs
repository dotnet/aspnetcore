// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class ServicesAnalyzer
{
    private readonly StartupAnalysisBuilder _context;

    public ServicesAnalyzer(StartupAnalysisBuilder context)
    {
        _context = context;
    }

    public void AnalyzeConfigureServices(OperationBlockStartAnalysisContext context)
    {
        var configureServicesMethod = (IMethodSymbol)context.OwningSymbol;
        var services = ImmutableArray.CreateBuilder<ServicesItem>();
        context.RegisterOperationAction(context =>
        {
            // We're looking for usage of extension methods, so we need to look at the 'this' parameter
            // rather than invocation.Instance.
            if (context.Operation is IInvocationOperation invocation &&
            invocation.Instance == null &&
            invocation.Arguments.Length >= 1 &&
            SymbolEqualityComparer.Default.Equals(invocation.Arguments[0].Parameter?.Type, _context.StartupSymbols.IServiceCollection))
            {
                services.Add(new ServicesItem(invocation));
            }
        }, OperationKind.Invocation);

        context.RegisterOperationBlockEndAction(context =>
        {
            _context.ReportAnalysis(new ServicesAnalysis(configureServicesMethod, services.ToImmutable()));
        });
    }
}
