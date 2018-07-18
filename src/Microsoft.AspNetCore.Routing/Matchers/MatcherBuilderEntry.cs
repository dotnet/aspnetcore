// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class MatcherBuilderEntry : IComparable<MatcherBuilderEntry>
    {
        public MatcherBuilderEntry(MatcherEndpoint endpoint)
        {
            Endpoint = endpoint;

            Precedence = RoutePrecedence.ComputeInbound(endpoint.ParsedTemplate);
        }

        public MatcherEndpoint Endpoint { get; }

        public int Order => Endpoint.Order;

        public RouteTemplate Pattern => Endpoint.ParsedTemplate;

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

            return Pattern.TemplateText.CompareTo(other.Pattern.TemplateText);
        }

        public bool PriorityEquals(MatcherBuilderEntry other)
        {
            return Order == other.Order && Precedence == other.Precedence;
        }
    }
}
