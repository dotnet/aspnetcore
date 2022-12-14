// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class RouteHandlerAnalyzer : DiagnosticAnalyzer
{
    private static void DetectAmbiguousRoutes(in OperationBlockAnalysisContext context, ConcurrentDictionary<MapOperation, byte> mapOperations)
    {
        if (mapOperations.IsEmpty)
        {
            return;
        }

        var groupedByParent = mapOperations
            .Select(kvp => kvp.Key)
            .Where(u => !u.RouteUsageModel.UsageContext.HttpMethods.IsDefault)
            .GroupBy(u => new MapOperationGroupKey(u.Builder, u.Operation, u.RouteUsageModel.RoutePattern, u.RouteUsageModel.UsageContext.HttpMethods));

        foreach (var ambigiousGroup in groupedByParent.Where(g => g.Count() >= 2))
        {
            foreach (var ambigiousMapOperation in ambigiousGroup)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AmbiguousRouteHandlerRoute,
                    ambigiousMapOperation.RouteUsageModel.UsageContext.RouteToken.GetLocation(),
                    ambigiousMapOperation.RouteUsageModel.RoutePattern.Root.ToString()));
            }
        }
    }

    private readonly struct MapOperationGroupKey : IEquatable<MapOperationGroupKey>
    {
        public IOperation? ParentOperation { get; }
        public IOperation? Builder { get; }
        public RoutePatternTree RoutePattern { get; }
        public ImmutableArray<string> HttpMethods { get; }

        public MapOperationGroupKey(IOperation? builder, IInvocationOperation operation, RoutePatternTree routePattern, ImmutableArray<string> httpMethods)
        {
            Debug.Assert(!httpMethods.IsDefault);

            ParentOperation = GetParentOperation(operation);
            Builder = builder;
            RoutePattern = routePattern;
            HttpMethods = httpMethods;
        }

        private static IOperation? GetParentOperation(IOperation operation)
        {
            // We want to group routes in a block together because we know they're being used together.
            // There are some circumstances where we still don't want to use the route, either because it is only conditionally
            // being called, or the IEndpointConventionBuilder returned from the method is being used. We can't accurately
            // detect what extra endpoint metadata is being added to the routes.
            //
            // Don't use route endpoint if:
            // - It's in a conditional statement.
            // - It's in a coalesce statement.
            // - It's an argument to a method call.
            // - It's has methods called on it.
            // - It's assigned to a variable.
            var current = operation;
            while (current != null)
            {
                if (current.Parent is IBlockOperation blockOperation)
                {
                    return blockOperation;
                }
                else if (current.Parent is IConditionalOperation ||
                    current.Parent is ICoalesceOperation ||
                    current.Parent is IAssignmentOperation ||
                    current.Parent is IArgumentOperation ||
                    current.Parent is IInvocationOperation)
                {
                    return current;
                }
                
                current = current.Parent;
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            if (obj is MapOperationGroupKey key)
            {
                return Equals(key);
            }
            return false;
        }

        public bool Equals(MapOperationGroupKey other)
        {
            return
                ParentOperation != null &&
                Equals(ParentOperation, other.ParentOperation) &&
                Builder != null &&
                SymbolEqualityComparer.Default.Equals((Builder as ILocalReferenceOperation)?.Local, (other.Builder as ILocalReferenceOperation)?.Local) &&
                AmbiguousRoutePatternComparer.Instance.Equals(RoutePattern, other.RoutePattern) &&
                HasMatchingHttpMethods(HttpMethods, other.HttpMethods);
        }

        private static bool HasMatchingHttpMethods(ImmutableArray<string> httpMethods1, ImmutableArray<string> httpMethods2)
        {
            if (httpMethods1.IsEmpty || httpMethods2.IsEmpty)
            {
                return true;
            }

            foreach (var item1 in httpMethods1)
            {
                foreach (var item2 in httpMethods2)
                {
                    if (item2 == item1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (ParentOperation?.GetHashCode() ?? 0) ^ AmbiguousRoutePatternComparer.Instance.GetHashCode(RoutePattern);
        }
    }
}
