// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class ServicesAnalyzer
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
                    invocation.Arguments[0].Parameter?.Type == _context.StartupSymbols.IServiceCollection)
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
}
