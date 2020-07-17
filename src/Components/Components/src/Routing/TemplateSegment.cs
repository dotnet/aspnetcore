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

            // Process segments that are not parameters or do not contain
            // a token separating a type constraint.
            if (!isParameter || segment.IndexOf(':') < 0)
            {
                // Set the IsOptional flag to true for segments that contain
                // a parameter with no type constraints but optionality set
                // via the '?' token.
                if (segment.IndexOf('?') == segment.Length - 1)
                {
                    IsOptional = true;
                    Value = segment.Substring(0, segment.Length - 1);
                }
                // If the `?` optional marker shows up in the segment but not at the very end,
                // then throw an error.
                else if (segment.IndexOf('?') >= 0 && segment.IndexOf('?') != segment.Length - 1)
                {
                    throw new ArgumentException($"Malformed parameter '{segment}' in route '{template}'. '?' character can only appear at the end of parameter name.");
                }
                else
                {
                    Value = segment;
                }
                
                Constraints = Array.Empty<RouteConstraint>();
            }
            else
            {
                var tokens = segment.Split(':');
                if (tokens[0].Length == 0)
                {
                    throw new ArgumentException($"Malformed parameter '{segment}' in route '{template}' has no name before the constraints list.");
                }

                // Set the IsOptional flag to true if any type constraints
                // for this parameter are designated as optional.
                IsOptional = tokens.Skip(1).Any(token => token.EndsWith("?"));

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

        public bool IsOptional { get;  }

        public RouteConstraint[] Constraints { get; }

        public bool Match(string pathSegment, out object? matchedParameterValue)
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
