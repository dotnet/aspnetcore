// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

// Use to sort and group Endpoints. RouteEndpoints are sorted before other implementations.
//
// NOTE:
// When ordering endpoints, we compare the route templates as an absolute last resort.
// This is used as a factor to ensure that we always have a predictable ordering
// for tests, errors, etc.
//
// When we group endpoints we don't consider the route template, because we're trying
// to group endpoints not separate them.
//
// TLDR:
//  IComparer implementation considers the template string as a tiebreaker.
//  IEqualityComparer implementation does not.
//  This is cool and good.
internal sealed class EndpointComparer : IComparer<Endpoint>, IEqualityComparer<Endpoint>
{
    private readonly IComparer<Endpoint>[] _comparers;

    public EndpointComparer(IEndpointComparerPolicy[] policies)
    {
        // Order, Precedence, (others)...
        _comparers = new IComparer<Endpoint>[2 + policies.Length];
        _comparers[0] = OrderComparer.Instance;
        _comparers[1] = PrecedenceComparer.Instance;
        for (var i = 0; i < policies.Length; i++)
        {
            _comparers[i + 2] = policies[i].Comparer;
        }
    }

    public int Compare(Endpoint? x, Endpoint? y)
    {
        // We don't expose this publicly, and we should never call it on
        // a null endpoint.
        Debug.Assert(x != null);
        Debug.Assert(y != null);

        var compare = CompareCore(x, y);

        // Since we're sorting, use the route template as a last resort.
        return compare == 0 ? ComparePattern(x, y) : compare;
    }

    private static int ComparePattern(Endpoint x, Endpoint y)
    {
        // A RouteEndpoint always comes before a non-RouteEndpoint, regardless of its RawText value
        var routeEndpointX = x as RouteEndpoint;
        var routeEndpointY = y as RouteEndpoint;

        if (routeEndpointX != null)
        {
            if (routeEndpointY != null)
            {
                return string.Compare(routeEndpointX.RoutePattern.RawText, routeEndpointY.RoutePattern.RawText, StringComparison.OrdinalIgnoreCase);
            }

            return 1;
        }
        else if (routeEndpointY != null)
        {
            return -1;
        }

        return 0;
    }

    public bool Equals(Endpoint? x, Endpoint? y)
    {
        // We don't expose this publicly, and we should never call it on
        // a null endpoint.
        Debug.Assert(x != null);
        Debug.Assert(y != null);

        return CompareCore(x, y) == 0;
    }

    public int GetHashCode(Endpoint obj)
    {
        // This should not be possible to call publicly.
        Debug.Fail("We don't expect this to be called.");
        throw new System.NotImplementedException();
    }

    private int CompareCore(Endpoint x, Endpoint y)
    {
        for (var i = 0; i < _comparers.Length; i++)
        {
            var compare = _comparers[i].Compare(x, y);
            if (compare != 0)
            {
                return compare;
            }
        }

        return 0;
    }

    private sealed class OrderComparer : IComparer<Endpoint>
    {
        public static readonly IComparer<Endpoint> Instance = new OrderComparer();

        public int Compare(Endpoint? x, Endpoint? y)
        {
            var routeEndpointX = x as RouteEndpoint;
            var routeEndpointY = y as RouteEndpoint;

            if (routeEndpointX != null)
            {
                if (routeEndpointY != null)
                {
                    return routeEndpointX.Order.CompareTo(routeEndpointY.Order);
                }

                return 1;
            }
            else if (routeEndpointY != null)
            {
                return -1;
            }

            return 0;
        }
    }

    private sealed class PrecedenceComparer : IComparer<Endpoint>
    {
        public static readonly IComparer<Endpoint> Instance = new PrecedenceComparer();

        public int Compare(Endpoint? x, Endpoint? y)
        {
            var routeEndpointX = x as RouteEndpoint;
            var routeEndpointY = y as RouteEndpoint;

            if (routeEndpointX != null)
            {
                if (routeEndpointY != null)
                {
                    return routeEndpointX.RoutePattern.InboundPrecedence
                        .CompareTo(routeEndpointY.RoutePattern.InboundPrecedence);
                }

                return 1;
            }
            else if (routeEndpointY != null)
            {
                return -1;
            }

            return 0;
        }
    }
}
