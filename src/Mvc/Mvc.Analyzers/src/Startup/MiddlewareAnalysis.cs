// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class MiddlewareAnalysis : ConfigureMethodAnalysis
    {
        public static MiddlewareAnalysis CreateAndInitialize(StartupAnalysisContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var symbols = context.StartupSymbols;
            var analysis = new MiddlewareAnalysis((IMethodSymbol)context.OperationBlockStartAnalysisContext.OwningSymbol);

            var middleware = ImmutableArray.CreateBuilder<MiddlewareItem>();

            context.OperationBlockStartAnalysisContext.RegisterOperationAction(context =>
            {
                // We're looking for usage of extension methods, so we need to look at the 'this' parameter
                // rather than invocation.Instance.
                if (context.Operation is IInvocationOperation invocation &&
                    invocation.Instance == null &&
                    invocation.Arguments.Length >= 1 &&
                    invocation.Arguments[0].Parameter?.Type == symbols.IApplicationBuilder)
                {
                    middleware.Add(new MiddlewareItem(invocation));
                }
            }, OperationKind.Invocation);

            context.OperationBlockStartAnalysisContext.RegisterOperationBlockEndAction(context =>
            {
                analysis.Middleware = middleware.ToImmutable();
            });

            return analysis;
        }

        public MiddlewareAnalysis(IMethodSymbol configureMethod)
            : base(configureMethod)
        {
        }

        public ImmutableArray<MiddlewareItem> Middleware { get; private set; } = ImmutableArray<MiddlewareItem>.Empty;
    }
}
