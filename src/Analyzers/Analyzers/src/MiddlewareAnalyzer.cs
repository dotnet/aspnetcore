// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers
{
    internal class MiddlewareAnalyzer
    {
        private readonly StartupAnalysisBuilder _context;

        public MiddlewareAnalyzer(StartupAnalysisBuilder context)
        {
            _context = context;
        }

        public void AnalyzeConfigureMethod(OperationBlockStartAnalysisContext context)
        {
            var configureMethod = (IMethodSymbol)context.OwningSymbol;
            var middleware = ImmutableArray.CreateBuilder<MiddlewareItem>();
            // Stores a set of method calls, e.g. `app.UseRouting();` or `app.UseRouting().UseAuthorization().UseEndpoints();`
            var previousMiddlewareCalls = new Stack<MiddlewareItem>();

            // Note: this is a simple source-order implementation. We don't attempt perform data flow
            // analysis in order to determine the actual order in which middleware are ordered.
            //
            // This can currently be confused by things like Map(...)
            context.RegisterOperationAction(context =>
            {
                // We're looking for usage of extension methods, so we need to look at the 'this' parameter
                // rather than invocation.Instance.
                if (context.Operation is IInvocationOperation invocation &&
                    invocation.Instance == null &&
                    invocation.Arguments.Length >= 1 &&
                    invocation.Arguments[0].Parameter?.Type == _context.StartupSymbols.IApplicationBuilder) {
                    var middlewareItem = new MiddlewareItem(invocation);

                    // First call of a new set of operations, i.e. the previous chained block has finished.
                    if (invocation.Parent.Parent.Kind == OperationKind.Block) {
                        AddPreviousItems(middleware, previousMiddlewareCalls);
                    }

                    previousMiddlewareCalls.Push(middlewareItem);
                }
            }, OperationKind.Invocation);

            context.RegisterOperationBlockEndAction(context =>
            {
                AddPreviousItems(middleware, previousMiddlewareCalls);
                _context.ReportAnalysis(new MiddlewareAnalysis(configureMethod, middleware.ToImmutable()));
            });
        }

        private static void AddPreviousItems(ImmutableArray<MiddlewareItem>.Builder middleware, Stack<MiddlewareItem> previousMiddlewareCalls) {

            // Chained methods arrive in reverse order
            // e.g. app.UseRouting()
            //         .UseAuthorization()
            //         .UseEndpoints();
            // will be stored as
            // [0] UseEndpoints()
            // [1] UseAuthorization()
            // [2] UseRouting()
            while (previousMiddlewareCalls.Count > 0) {
                middleware.Add(previousMiddlewareCalls.Pop());
            }
        }
    }
}
