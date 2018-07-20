// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class MatcherBuilderEntry : IComparable<MatcherBuilderEntry>
    {
        public MatcherBuilderEntry(MatcherEndpoint endpoint)
        {
            Endpoint = endpoint;

            Precedence = RoutePrecedence.ComputeInbound(endpoint.RoutePattern);
        }

        public MatcherEndpoint Endpoint { get; }

        public int Order => Endpoint.Order;

        public RoutePattern RoutePattern => Endpoint.RoutePattern;

        public decimal Precedence { get; }

        public int CompareTo(MatcherBuilderEntry other)
        {
            var comparison = Order.CompareTo(other.Order);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = Precedence.CompareTo(other.Precedence);
            if (comparison != 0)
            {
                return comparison;
            }

            return RoutePattern.RawText.CompareTo(other.RoutePattern.RawText);
        }

        public bool PriorityEquals(MatcherBuilderEntry other)
        {
            return Order == other.Order && Precedence == other.Precedence;
        }
    }
}
