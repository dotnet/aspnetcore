// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Grpc.Shared;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

/// <summary>
/// Routes on HTTP rule are similar to ASP.NET Core routes but add and remove some features.
/// <list type="bullet">
/// <item><description>Constraints aren't supported.</description></item>
/// <item><description>Optional parameters aren't supported.</description></item>
/// <item><description>Parameters spanning multiple segments are supported.</description></item>
/// </list>
///
/// The purpose of this type is to add support for parameters spanning multiple segments and
/// anonymous any or catch-all segments. This type transforms an HTTP route into an ASP.NET Core
/// route by rewritting it to a compatible format and providing actions to reconstruct parameters
/// that span multiple segments.
///
/// <para>
/// For example, consider a multi-segment parameter route:
/// <list type="bullet">
/// <item><description>Before: /v1/{book.name=shelves/*/books/*}</description></item>
/// <item><description>After: /v1/shelves/{__Complex_book.name_2}/books/{__Complex_book.name_4}</description></item>
/// </list>
/// </para>
/// 
/// It is rewritten so that any * or ** segments become ASP.NET Core route parameters. These parameter
/// names are never used by the user, and instead they're reconstructed into the final value by the
/// adapter and then added to the HttpRequest.RouteValues collection.
/// <list type="bullet">
/// <item><description>Request URL: /v1/shelves/example-shelf/books/example-book</description></item>
/// <item><description>Route parameter: book.name = shelves/example-self/books/example-book</description></item>
/// </list>
/// </summary>
internal sealed class JsonTranscodingRouteAdapter
{
    public HttpRoutePattern HttpRoutePattern { get; }
    public string ResolvedRouteTemplate { get; }
    public List<Action<HttpContext>> RewriteVariableActions { get; }

    private JsonTranscodingRouteAdapter(HttpRoutePattern httpRoutePattern, string resolvedRoutePattern, List<Action<HttpContext>> rewriteVariableActions)
    {
        HttpRoutePattern = httpRoutePattern;
        ResolvedRouteTemplate = resolvedRoutePattern;
        RewriteVariableActions = rewriteVariableActions;
    }

    public static JsonTranscodingRouteAdapter Parse(HttpRoutePattern pattern)
    {
        var rewriteActions = new List<Action<HttpContext>>();

        var tempSegments = pattern.Segments.ToList();
        var haveCatchAll = false;

        var i = 0;
        while (i < tempSegments.Count)
        {
            var segmentVariable = GetVariable(pattern, i);
            if (segmentVariable != null)
            {
                var fullPath = string.Join(".", segmentVariable.FieldPath);

                var remainingSegmentCount = segmentVariable.EndSegment - segmentVariable.StartSegment;

                // Handle situation where the last segment is catch all but there is a verb.
                if (remainingSegmentCount == 1 && segmentVariable.HasCatchAllPath && pattern.Verb != null)
                {
                    // Move past the catch all so the regex added below just includes the verb.
                    remainingSegmentCount++;
                }

                if (remainingSegmentCount == 1)
                {
                    // Single segment parameter. Include in route with its default name.
                    tempSegments[i] = segmentVariable.HasCatchAllPath
                        ? $"{{**{fullPath}}}"
                        : $"{{{fullPath}}}";
                    i++;
                }
                else
                {
                    var routeParameterParts = new List<string>();
                    var routeValueFormatTemplateParts = new List<string>();
                    var variableParts = new List<string>();
                    var catchAllSuffix = string.Empty;

                    while (i < segmentVariable.EndSegment && !haveCatchAll)
                    {
                        var segment = tempSegments[i];
                        var segmentType = GetSegmentType(segment);
                        switch (segmentType)
                        {
                            case SegmentType.Literal:
                                routeValueFormatTemplateParts.Add(segment);
                                break;
                            case SegmentType.Any:
                                {
                                    var parameterName = $"__Complex_{fullPath}_{i}";
                                    tempSegments[i] = $"{{{parameterName}}}";

                                    routeValueFormatTemplateParts.Add($"{{{variableParts.Count}}}");
                                    variableParts.Add(parameterName);
                                    break;
                                }
                            case SegmentType.CatchAll:
                                {
                                    var parameterName = $"__Complex_{fullPath}_{i}";
                                    var suffix = BuildSuffix(tempSegments.Skip(i + 1), pattern.Verb);
                                    catchAllSuffix = BuildSuffix(tempSegments.Skip(i + remainingSegmentCount - 1), pattern.Verb);

                                    // It's possible to have multiple routes with catch-all parameters that have different suffixes.
                                    // For example:
                                    // - /{name=v1/**/b}/one
                                    // - /{name=v1/**/b}/two
                                    // The suffix is added as a route constraint to avoid matching multiple routes to a request.
                                    var constraint = suffix.Length > 0 ? $":regex({Regex.Escape(suffix)}$)" : string.Empty;
                                    tempSegments[i] = $"{{**{parameterName}{constraint}}}";

                                    routeValueFormatTemplateParts.Add($"{{{variableParts.Count}}}");
                                    variableParts.Add(parameterName);
                                    haveCatchAll = true;

                                    // Remove remaining segments. They have been added in the route constraint.
                                    while (i < tempSegments.Count - 1)
                                    {
                                        tempSegments.RemoveAt(tempSegments.Count - 1);
                                    }
                                    break;
                                }
                        }
                        i++;
                    }

                    var routeValueFormatTemplate = string.Join("/", routeValueFormatTemplateParts);

                    // Add an action to reconstruct the multiple segment parameter from ASP.NET Core
                    // request route values. This should be called when the request is received.
                    rewriteActions.Add(context =>
                    {
                        var values = new object?[variableParts.Count];
                        for (var i = 0; i < values.Length; i++)
                        {
                            values[i] = context.Request.RouteValues[variableParts[i]];
                        }
                        var finalValue = string.Format(CultureInfo.InvariantCulture, routeValueFormatTemplate, values);

                        // Catch-all route parameter is always the last parameter. The original HTTP pattern could specify a
                        // literal suffix after the catch-all, e.g. /{param=**}/suffix. Because ASP.NET Core routing provides
                        // the entire remainder of the URL in the route value, we must trim the suffix from that route value.
                        if (!string.IsNullOrEmpty(catchAllSuffix))
                        {
                            finalValue = finalValue[..^catchAllSuffix.Length];
                        }
                        context.Request.RouteValues[fullPath] = finalValue;
                    });
                }
            }
            else
            {
                // HTTP route can match any value in a segment without a parameter.
                // For example, v1/*/books. Add a parameter to match this behavior logic.
                // Parameter value is never used.

                var segmentType = GetSegmentType(tempSegments[i]);
                switch (segmentType)
                {
                    case SegmentType.Literal:
                        // Literal is unchanged.
                        break;
                    case SegmentType.Any:
                        // Ignore any segment value.
                        tempSegments[i] = $"{{__Discard_{i}}}";
                        break;
                    case SegmentType.CatchAll:
                        // Ignore remaining segment values.
                        if (pattern.Verb != null)
                        {
                            tempSegments[i] = $"{{**__Discard_{i}:regex({Regex.Escape($":{pattern.Verb}")}$)}}";
                        }
                        else
                        {
                            tempSegments[i] = $"{{**__Discard_{i}}}";
                        }
                        haveCatchAll = true;
                        break;
                }

                i++;
            }
        }

        string resolvedRoutePattern = "/" + string.Join("/", tempSegments);
        // If the route has a catch all then the verb is included in the catch all regex constraint.
        if (pattern.Verb != null && !haveCatchAll)
        {
            resolvedRoutePattern += ":" + pattern.Verb;
        }
        return new JsonTranscodingRouteAdapter(pattern, resolvedRoutePattern, rewriteActions);

        static string BuildSuffix(IEnumerable<string> segments, string? verb)
        {
            var pattern = string.Join("/", segments);
            if (!string.IsNullOrEmpty(pattern))
            {
                pattern = "/" + pattern;
            }
            if (verb != null)
            {
                pattern += ":" + verb;
            }
            return pattern;
        }
    }

    private static SegmentType GetSegmentType(string segment)
    {
        if (segment.StartsWith("**", StringComparison.Ordinal))
        {
            return SegmentType.CatchAll;
        }
        else if (segment.StartsWith('*'))
        {
            return SegmentType.Any;
        }
        else
        {
            return SegmentType.Literal;
        }
    }

    private enum SegmentType
    {
        Literal,
        Any,
        CatchAll
    }

    private static HttpRouteVariable? GetVariable(HttpRoutePattern pattern, int i)
    {
        foreach (var variable in pattern.Variables)
        {
            if (i >= variable.StartSegment && i < variable.EndSegment)
            {
                return variable;
            }
        }

        return null;
    }
}
