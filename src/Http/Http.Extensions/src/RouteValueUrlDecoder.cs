// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides methods for extracting and decoding route parameter values from the raw request target
/// to avoid double-decoding that would occur when applying <see cref="Uri.UnescapeDataString(string)"/>
/// to already partially-decoded route values.
/// </summary>
/// <remarks>
/// This type is intended for use by generated code and should not be used directly.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RouteValueUrlDecoder
{
    /// <summary>
    /// Gets a fully URL-decoded route parameter value by extracting it from the raw request target
    /// and applying a single pass of <see cref="Uri.UnescapeDataString(string)"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="parameterName">The name of the route parameter to decode.</param>
    /// <returns>The fully decoded route parameter value, or <c>null</c> if the parameter is not found.</returns>
    public static string? GetUrlDecodedRouteValue(HttpContext httpContext, string parameterName)
    {
        var routeValue = httpContext.Request.RouteValues[parameterName]?.ToString();
        if (routeValue is null)
        {
            return null;
        }

        // Try to get the raw (undecoded) request target.
        var rawTarget = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;
        if (rawTarget is null)
        {
            // Raw target not available (e.g. non-Kestrel server).
            // Return the route value without additional decoding to avoid double-decode risk.
            return routeValue;
        }

        // Get the route template from endpoint diagnostics metadata.
        var endpoint = httpContext.GetEndpoint();
        var routeTemplate = endpoint?.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()?.Route;
        if (routeTemplate is null)
        {
            return routeValue;
        }

        // Find the parameter's segment index in the route template.
        var (segmentIndex, isSimple, isCatchAll) = FindParameterSegment(routeTemplate, parameterName);
        if (segmentIndex < 0 || !isSimple)
        {
            // Complex segment (e.g. {action}.{format}) or parameter not found.
            // Cannot safely extract from the raw target.
            return routeValue;
        }

        // Strip the query string from the raw target.
        var rawPath = rawTarget.AsSpan();
        var queryIndex = rawPath.IndexOf('?');
        if (queryIndex >= 0)
        {
            rawPath = rawPath[..queryIndex];
        }

        // Strip PathBase from the raw path.
        var pathBase = httpContext.Request.PathBase.Value;
        if (!string.IsNullOrEmpty(pathBase) && rawPath.StartsWith(pathBase.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            rawPath = rawPath[pathBase.Length..];
        }

        // Extract the raw segment at the computed index and decode it.
        var rawSegment = ExtractSegment(rawPath, segmentIndex, isCatchAll);
        if (rawSegment.IsEmpty)
        {
            return routeValue;
        }

        return Uri.UnescapeDataString(rawSegment.ToString());
    }

    internal static (int segmentIndex, bool isSimple, bool isCatchAll) FindParameterSegment(string template, string parameterName)
    {
        var segmentIndex = 0;
        var i = 0;

        // Skip leading '/'.
        if (i < template.Length && template[i] == '/')
        {
            i++;
        }

        while (i < template.Length)
        {
            var segStart = i;

            // Find the end of the current segment.
            while (i < template.Length && template[i] != '/')
            {
                i++;
            }

            var segment = template.AsSpan(segStart, i - segStart);
            if (TryMatchParameter(segment, parameterName, out var isSimple, out var isCatchAll))
            {
                return (segmentIndex, isSimple, isCatchAll);
            }

            segmentIndex++;

            // Skip '/'.
            if (i < template.Length)
            {
                i++;
            }
        }

        return (-1, false, false);
    }

    private static bool TryMatchParameter(ReadOnlySpan<char> segment, string parameterName, out bool isSimple, out bool isCatchAll)
    {
        isSimple = false;
        isCatchAll = false;

        var braceStart = segment.IndexOf('{');
        if (braceStart < 0)
        {
            return false;
        }

        var braceEnd = segment.LastIndexOf('}');
        if (braceEnd < 0)
        {
            return false;
        }

        // Extract the content between braces.
        var content = segment[(braceStart + 1)..braceEnd];

        // Handle catch-all prefix (** or *).
        if (content.StartsWith("**"))
        {
            content = content[2..];
            isCatchAll = true;
        }
        else if (content.StartsWith("*"))
        {
            content = content[1..];
            isCatchAll = true;
        }

        // Strip constraints (everything after ':').
        var colonIndex = content.IndexOf(':');
        if (colonIndex >= 0)
        {
            content = content[..colonIndex];
        }

        // Strip optional marker '?'.
        if (content.EndsWith("?"))
        {
            content = content[..^1];
        }

        // Compare parameter name (case-insensitive).
        if (!content.Equals(parameterName.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // A segment is "simple" if it consists entirely of this one parameter.
        isSimple = braceStart == 0 && braceEnd == segment.Length - 1;

        return true;
    }

    private static ReadOnlySpan<char> ExtractSegment(ReadOnlySpan<char> rawPath, int targetIndex, bool isCatchAll)
    {
        var currentIndex = 0;
        var i = 0;

        // Skip leading '/'.
        if (i < rawPath.Length && rawPath[i] == '/')
        {
            i++;
        }

        var segStart = i;

        while (i <= rawPath.Length)
        {
            if (i == rawPath.Length || rawPath[i] == '/')
            {
                if (currentIndex == targetIndex)
                {
                    return isCatchAll ? rawPath[segStart..] : rawPath[segStart..i];
                }

                currentIndex++;
                segStart = i + 1;
            }

            i++;
        }

        return ReadOnlySpan<char>.Empty;
    }
}
