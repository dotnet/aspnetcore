// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class ServicesAnalysis : ConfigureServicesMethodAnalysis
    {
        public static ServicesAnalysis CreateAndInitialize(StartupAnalysisContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var symbols = context.StartupSymbols;
            var analysis = new ServicesAnalysis((IMethodSymbol)context.OperationBlockStartAnalysisContext.OwningSymbol);

            var services = ImmutableArray.CreateBuilder<ServicesItem>();
            context.OperationBlockStartAnalysisContext.RegisterOperationAction(context =>
            {
                // We're looking for usage of extension methods, so we need to look at the 'this' parameter
                // rather than invocation.Instance.
                if (context.Operation is IInvocationOperation invocation &&
                    invocation.Instance == null &&
                    invocation.Arguments.Length >= 1 &&
                    invocation.Arguments[0].Parameter?.Type == symbols.IServiceCollection)
                {
                    services.Add(new ServicesItem(invocation));
                }
            }, OperationKind.Invocation);

            context.OperationBlockStartAnalysisContext.RegisterOperationBlockEndAction(context =>
            {
                analysis.Services = services.ToImmutable();
            });

            return analysis;
        }

        public ServicesAnalysis(IMethodSymbol configureServicesMethod) 
            : base(configureServicesMethod)
        {
        }

        public ImmutableArray<ServicesItem> Services { get; private set; } = ImmutableArray<ServicesItem>.Empty;
    }
}
