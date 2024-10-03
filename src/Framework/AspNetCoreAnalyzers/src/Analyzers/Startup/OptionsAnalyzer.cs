// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.Startup;

internal sealed class OptionsAnalyzer
{
    private readonly StartupAnalysisBuilder _context;

    public OptionsAnalyzer(StartupAnalysisBuilder context)
    {
        _context = context;
    }

    public void AnalyzeConfigureServices(OperationBlockStartAnalysisContext context)
    {
        var configureServicesMethod = (IMethodSymbol)context.OwningSymbol;
        var options = ImmutableArray.CreateBuilder<OptionsItem>();

        context.RegisterOperationAction(context =>
        {
            if (context.Operation is ISimpleAssignmentOperation operation &&
                operation.Value.ConstantValue.HasValue &&
                // For nullable types, it's possible for Value to be null when HasValue is true.
                operation.Value.ConstantValue.Value != null &&
                operation.Target is IPropertyReferenceOperation property &&
                property.Property?.ContainingType?.Name != null &&
                property.Property.ContainingType.Name.EndsWith("Options", StringComparison.Ordinal))
            {
                options.Add(new OptionsItem(property.Property, operation.Value.ConstantValue.Value));
            }

        }, OperationKind.SimpleAssignment);

        context.RegisterOperationBlockEndAction(context =>
        {
            _context.ReportAnalysis(new OptionsAnalysis(configureServicesMethod, options.ToImmutable()));
        });
    }
}
