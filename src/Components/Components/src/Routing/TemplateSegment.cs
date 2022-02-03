// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

internal class TemplateSegment
{
    public TemplateSegment(string template, string segment, bool isParameter)
    {
        IsParameter = isParameter;

        IsCatchAll = isParameter && segment.StartsWith('*');

        if (IsCatchAll)
        {
            // Only one '*' currently allowed
            Value = segment[1..];

            var invalidCharacterIndex = Value.IndexOf('*');
            if (invalidCharacterIndex != -1)
            {
                throw new InvalidOperationException($"Invalid template '{template}'. A catch-all parameter may only have one '*' at the beginning of the segment.");
            }
        }
        else
        {
            Value = segment;
        }

        // Process segments that parameters  that do not contain a token separating a type constraint.
        if (IsParameter)
        {
            if (Value.IndexOf(':') < 0)
            {
                // Set the IsOptional flag to true for segments that contain
                // a parameter with no type constraints but optionality set
                // via the '?' token.
                var questionMarkIndex = Value.IndexOf('?');
                if (questionMarkIndex == Value.Length - 1)
                {
                    IsOptional = true;
                    Value = Value[0..^1];
                }
                // If the `?` optional marker shows up in the segment but not at the very end,
                // then throw an error.
                else if (questionMarkIndex >= 0)
                {
                    throw new ArgumentException($"Malformed parameter '{segment}' in route '{template}'. '?' character can only appear at the end of parameter name.");
                }

                Constraints = Array.Empty<UrlValueConstraint>();
            }
            else
            {
                var tokens = Value.Split(':');
                if (tokens[0].Length == 0)
                {
                    throw new ArgumentException($"Malformed parameter '{segment}' in route '{template}' has no name before the constraints list.");
                }

                Value = tokens[0];
                IsOptional = tokens[^1].EndsWith('?');
                if (IsOptional)
                {
                    tokens[^1] = tokens[^1][0..^1];
                }

                Constraints = new UrlValueConstraint[tokens.Length - 1];
                for (var i = 1; i < tokens.Length; i++)
                {
                    Constraints[i - 1] = RouteConstraint.Parse(template, segment, tokens[i]);
                }
            }
        }
        else
        {
            Constraints = Array.Empty<UrlValueConstraint>();
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

    public bool IsOptional { get; }

    public bool IsCatchAll { get; }

    public UrlValueConstraint[] Constraints { get; }

    public bool Match(string pathSegment, out object? matchedParameterValue)
    {
        if (IsParameter)
        {
            matchedParameterValue = pathSegment;

            foreach (var constraint in Constraints)
            {
                if (!constraint.TryParse(pathSegment, out matchedParameterValue))
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

    public override string ToString() => this switch
    {
        { IsParameter: true, IsOptional: false, IsCatchAll: false, Constraints: { Length: 0 } } => $"{{{Value}}}",
        { IsParameter: true, IsOptional: false, IsCatchAll: false, Constraints: { Length: > 0 } } => $"{{{Value}:{string.Join(':', (object[])Constraints)}}}",
        { IsParameter: true, IsOptional: true, Constraints: { Length: 0 } } => $"{{{Value}?}}",
        { IsParameter: true, IsOptional: true, Constraints: { Length: > 0 } } => $"{{{Value}:{string.Join(':', (object[])Constraints)}?}}",
        { IsParameter: true, IsCatchAll: true, Constraints: { Length: 0 } } => $"{{*{Value}}}",
        { IsParameter: true, IsCatchAll: true, Constraints: { Length: > 0 } } => $"{{*{Value}:{string.Join(':', (object[])Constraints)}?}}",
        { IsParameter: false } => Value,
        _ => throw new InvalidOperationException("Invalid template segment.")
    };
}
