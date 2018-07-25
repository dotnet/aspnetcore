// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // Use to sort and group MatcherEndpoints.
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
    internal class MatcherEndpointComparer : IComparer<MatcherEndpoint>, IEqualityComparer<MatcherEndpoint>
    {
        private readonly IComparer<MatcherEndpoint>[] _comparers;

        public MatcherEndpointComparer(IEndpointComparerPolicy[] policies)
        {
            // Order, Precedence, (others)...
            _comparers = new IComparer<MatcherEndpoint>[2 + policies.Length];
            _comparers[0] = OrderComparer.Instance;
            _comparers[1] = PrecedenceComparer.Instance;
            for (var i = 0; i < policies.Length; i++)
            {
                _comparers[i + 2] = policies[i].Comparer;
            }
        }

        public int Compare(MatcherEndpoint x, MatcherEndpoint y)
        {
            // We don't expose this publicly, and we should never call it on
            // a null endpoint.
            Debug.Assert(x != null);
            Debug.Assert(y != null);

            var compare = CompareCore(x, y);

            // Since we're sorting, use the route template as a last resort.
            return compare == 0 ? x.RoutePattern.RawText.CompareTo(y.RoutePattern.RawText) : compare;
        }

        public bool Equals(MatcherEndpoint x, MatcherEndpoint y)
        {
            // We don't expose this publicly, and we should never call it on
            // a null endpoint.
            Debug.Assert(x != null);
            Debug.Assert(y != null);

            return CompareCore(x, y) == 0;
        }
        
        public int GetHashCode(MatcherEndpoint obj)
        {
            // This should not be possible to call publicly.
            Debug.Fail("We don't expect this to be called.");
            throw new System.NotImplementedException();
        }

        private int CompareCore(MatcherEndpoint x, MatcherEndpoint y)
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

        private class OrderComparer : IComparer<MatcherEndpoint>
        {
            public static readonly IComparer<MatcherEndpoint> Instance = new OrderComparer();

            public int Compare(MatcherEndpoint x, MatcherEndpoint y)
            {
                return x.Order.CompareTo(y.Order);
            }
        }

        private class PrecedenceComparer : IComparer<MatcherEndpoint>
        {
            public static readonly IComparer<MatcherEndpoint> Instance = new PrecedenceComparer();

            public int Compare(MatcherEndpoint x, MatcherEndpoint y)
            {
                return x.RoutePattern.InboundPrecedence.CompareTo(y.RoutePattern.InboundPrecedence);
            }
        }
    }
}
