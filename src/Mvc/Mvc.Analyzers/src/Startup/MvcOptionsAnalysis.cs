// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class MvcOptionsAnalysis : ConfigureServicesMethodAnalysis
    {
        public static MvcOptionsAnalysis CreateAndInitialize(StartupAnalysisContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var analysis = new MvcOptionsAnalysis((IMethodSymbol)context.OperationBlockStartAnalysisContext.OwningSymbol);

            context.OperationBlockStartAnalysisContext.RegisterOperationAction(context =>
            {
                if (context.Operation is ISimpleAssignmentOperation operation &&
                    operation.Value.ConstantValue.HasValue &&
                    operation.Target is IPropertyReferenceOperation property &&
                    property.Member?.Name == SymbolNames.EnableEndpointRoutingProperty)
                {
                    analysis.EndpointRoutingEnabled = operation.Value.ConstantValue.Value as bool?;
                }

            }, OperationKind.SimpleAssignment);

            return analysis;
        }

        public MvcOptionsAnalysis(IMethodSymbol configureServicesMethod)
            : base(configureServicesMethod)
        {
        }

        public bool? EndpointRoutingEnabled { get; private set; }
    }
}
