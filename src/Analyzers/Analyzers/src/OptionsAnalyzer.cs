// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class OptionsAnalyzer
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
                    property.Property.ContainingType.Name.EndsWith("Options"))
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
}
