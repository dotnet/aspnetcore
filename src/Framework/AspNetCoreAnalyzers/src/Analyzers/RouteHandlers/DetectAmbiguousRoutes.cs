// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
    private static void DetectAmbiguousRoutes(in OperationBlockAnalysisContext context, List<MapOperation> blockRouteUsage)
    {
        var groupedByParent = blockRouteUsage
            .Where(u => !u.RouteUsageModel.UsageContext.HttpMethods.IsDefault)
            .GroupBy(u => new MapOperationGroupKey(u.Operation, u.RouteUsageModel.RoutePattern, u.RouteUsageModel.UsageContext.HttpMethods));

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
        public RoutePatternTree RoutePattern { get; }
        public ImmutableArray<string> HttpMethods { get; }

        public MapOperationGroupKey(IOperation operation, RoutePatternTree routePattern, ImmutableArray<string> httpMethods)
        {
            Debug.Assert(!httpMethods.IsDefault);

            ParentOperation = GetParentOperation(operation);
            RoutePattern = routePattern;
            HttpMethods = httpMethods;
        }

        private static IOperation? GetParentOperation(IOperation operation)
        {
            var parent = operation.Parent;
            while (parent is not null)
            {
                if (parent is IBlockOperation)
                {
                    return parent;
                }

                parent = parent.Parent;
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
                Equals(ParentOperation, other.ParentOperation) &&
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
