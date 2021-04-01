// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Components.LegacyRouteMatching
{
    internal class LegacyTemplateSegment
    {
        public LegacyTemplateSegment(string template, string segment, bool isParameter)
        {
            IsParameter = isParameter;

            IsCatchAll = segment.StartsWith('*');

            if (IsCatchAll)
            {
                // Only one '*' currently allowed
                Value = segment.Substring(1);

                var invalidCharacter = Value.IndexOf('*');
                if (Value.IndexOf('*') != -1)
                {
                    throw new InvalidOperationException($"Invalid template '{template}'. A catch-all parameter may only have one '*' at the beginning of the segment.");
                }
            }
            else
            {
                Value = segment;
            }

            // Process segments that are not parameters or do not contain
            // a token separating a type constraint.
            if (!isParameter || Value.IndexOf(':') < 0)
            {
                // Set the IsOptional flag to true for segments that contain
                // a parameter with no type constraints but optionality set
                // via the '?' token.
                if (Value.IndexOf('?') == Value.Length - 1)
                {
                    IsOptional = true;
                    Value = Value.Substring(0, Value.Length - 1);
                }
                // If the `?` optional marker shows up in the segment but not at the very end,
                // then throw an error.
                else if (Value.IndexOf('?') >= 0 && Value.IndexOf('?') != Value.Length - 1)
                {
                    throw new ArgumentException($"Malformed parameter '{segment}' in route '{template}'. '?' character can only appear at the end of parameter name.");
                }

                Constraints = Array.Empty<LegacyRouteConstraint>();
            }
            else
            {
                var tokens = Value.Split(':');
                if (tokens[0].Length == 0)
                {
                    throw new ArgumentException($"Malformed parameter '{segment}' in route '{template}' has no name before the constraints list.");
                }

                // Set the IsOptional flag to true if any type constraints
                // for this parameter are designated as optional.
                IsOptional = tokens.Skip(1).Any(token => token.EndsWith('?'));

                Value = tokens[0];
                Constraints = tokens.Skip(1)
                    .Select(token => LegacyRouteConstraint.Parse(template, segment, token))
                    .ToArray();
            }

            if (IsParameter)
            {
                if (IsOptional && IsCatchAll)
                {
                    throw new InvalidOperationException($"Invalid segment '{segment}' in route '{template}'. A catch-all parameter cannot be marked optional.");
                }

                // Moving the check for this here instead of TemplateParser so we can allow catch-all.
                // We checked for '*' up above specifically for catch-all segments, this one checks for all others
                if (Value.IndexOf('*') != -1)
                {
                    throw new InvalidOperationException($"Invalid template '{template}'. The character '*' in parameter segment '{{{segment}}}' is not allowed.");
                }
            }
        }

        // The value of the segment. The exact text to match when is a literal.
        // The parameter name when its a segment
        public string Value { get; }

        public bool IsParameter { get; }

        public bool IsOptional { get;  }

        public bool IsCatchAll { get; }

        public LegacyRouteConstraint[] Constraints { get; }

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
