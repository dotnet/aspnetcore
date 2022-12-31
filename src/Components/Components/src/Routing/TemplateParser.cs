// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components.Routing;

// This implementation is temporary, in the future we'll want to have
// a more performant/properly designed routing set of abstractions.
// To be more precise these are some things we are scoping out:
// * We are not doing link generation.
// * We are not supporting all the route constraint formats supported by ASP.NET server-side routing.
// The class in here just takes care of parsing a route and extracting
// simple parameters from it.
// Some differences with ASP.NET Core routes are:
// * We don't support complex segments.
// The things that we support are:
// * Literal path segments. (Like /Path/To/Some/Page)
// * Parameter path segments (Like /Customer/{Id}/Orders/{OrderId})
// * Catch-all parameters (Like /blog/{*slug})
internal sealed class TemplateParser
{
    private static readonly IndexOfAnyValues<char> _invalidParameterNameCharacters = IndexOfAnyValues.Create("{}=.");

    internal static RouteTemplate ParseTemplate(string template)
    {
        var originalTemplate = template;
        template = template.Trim('/');
        if (template == string.Empty)
        {
            // Special case "/";
            return new RouteTemplate("/", Array.Empty<TemplateSegment>());
        }

        var segments = template.Split('/');
        var templateSegments = new TemplateSegment[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (string.IsNullOrEmpty(segment))
            {
                throw new InvalidOperationException(
                    $"Invalid template '{template}'. Empty segments are not allowed.");
            }

            if (segment[0] != '{')
            {
                if (segment[segment.Length - 1] == '}')
                {
                    throw new InvalidOperationException(
                        $"Invalid template '{template}'. Missing '{{' in parameter segment '{segment}'.");
                }
                if (segment[^1] == '?')
                {
                    throw new InvalidOperationException(
                        $"Invalid template '{template}'. '?' is not allowed in literal segment '{segment}'.");
                }
                templateSegments[i] = new TemplateSegment(originalTemplate, segment, isParameter: false);
            }
            else
            {
                if (segment[segment.Length - 1] != '}')
                {
                    throw new InvalidOperationException(
                        $"Invalid template '{template}'. Missing '}}' in parameter segment '{segment}'.");
                }

                if (segment.Length < 3)
                {
                    throw new InvalidOperationException(
                        $"Invalid template '{template}'. Empty parameter name in segment '{segment}' is not allowed.");
                }

                var invalidCharacter = segment.AsSpan(1, segment.Length - 2).IndexOfAny(_invalidParameterNameCharacters);
                if (invalidCharacter >= 0)
                {
                    invalidCharacter++;     // accommodate the slice above
                    throw new InvalidOperationException(
                        $"Invalid template '{template}'. The character '{segment[invalidCharacter]}' in parameter segment '{segment}' is not allowed.");
                }

                templateSegments[i] = new TemplateSegment(originalTemplate, segment.Substring(1, segment.Length - 2), isParameter: true);
            }
        }

        for (int i = 0; i < templateSegments.Length; i++)
        {
            var currentSegment = templateSegments[i];

            if (currentSegment.IsCatchAll && i != templateSegments.Length - 1)
            {
                throw new InvalidOperationException($"Invalid template '{template}'. A catch-all parameter can only appear as the last segment of the route template.");
            }

            if (!currentSegment.IsParameter)
            {
                continue;
            }

            for (int j = i + 1; j < templateSegments.Length; j++)
            {
                var nextSegment = templateSegments[j];

                if (currentSegment.IsOptional && !nextSegment.IsOptional && !nextSegment.IsCatchAll)
                {
                    throw new InvalidOperationException($"Invalid template '{template}'. Non-optional parameters or literal routes cannot appear after optional parameters.");
                }

                if (string.Equals(currentSegment.Value, nextSegment.Value, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Invalid template '{template}'. The parameter '{currentSegment}' appears multiple times.");
                }
            }
        }

        return new RouteTemplate(template, templateSegments);
    }
}
