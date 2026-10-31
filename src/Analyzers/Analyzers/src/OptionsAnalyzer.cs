// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers;

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
