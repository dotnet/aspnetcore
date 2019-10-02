// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Routing
{
    internal class TemplateSegment
    {
        public TemplateSegment(string template, string segment, bool isParameter)
        {
            IsParameter = isParameter;

            if (!isParameter || segment.IndexOf(':') < 0)
            {
                Value = segment;
                Constraints = Array.Empty<RouteConstraint>();
            }
            else
            {
                var tokens = segment.Split(':');
                if (tokens[0].Length == 0)
                {
                    throw new ArgumentException($"Malformed parameter '{segment}' in route '{template}' has no name before the constraints list.");
                }

                Value = tokens[0];
                Constraints = tokens.Skip(1)
                    .Select(token => RouteConstraint.Parse(template, segment, token))
                    .ToArray();
            }
        }

        // The value of the segment. The exact text to match when is a literal.
        // The parameter name when its a segment
        public string Value { get; }

        public bool IsParameter { get; }

        public RouteConstraint[] Constraints { get; }

        public bool Match(string pathSegment, out object matchedParameterValue)
        {
            if (IsParameter)
            {
                matchedParameterValue = pathSegment;

                foreach (var constraint in Constraints)
                {
                    if (!constraint.Match(pathSegment, out matchedParameterValue))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                matchedParameterValue = null;
                return string.Equals(Value, pathSegment, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
