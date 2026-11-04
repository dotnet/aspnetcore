// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal static class RoutePatternWriter
{
    public static void WriteString(StringBuilder sb, IEnumerable<RoutePatternPathSegment> routeSegments)
    {
        foreach (var segment in routeSegments)
        {
            if (sb.Length > 0)
            {
                sb.Append('/');
            }

            WriteString(sb, segment);
        }
    }

    private static void WriteString(StringBuilder sb, RoutePatternPathSegment segment)
    {
        for (var i = 0; i < segment.Parts.Count; i++)
        {
            WriteString(sb, segment.Parts[i]);
        }
    }

    private static void WriteString(StringBuilder sb, RoutePatternPart part)
    {
        if (part.IsParameter && part is RoutePatternParameterPart parameterPart)
        {
            sb.Append('{');
            if (parameterPart.IsCatchAll)
            {
                sb.Append('*');
                if (!parameterPart.EncodeSlashes)
                {
                    sb.Append('*');
                }
            }
            sb.Append(parameterPart.Name);
            foreach (var item in parameterPart.ParameterPolicies)
            {
                sb.Append(':');
                sb.Append(item.Content);
            }
            if (parameterPart.Default != null)
            {
                sb.Append('=');
                sb.Append(parameterPart.Default);
            }
            if (parameterPart.IsOptional)
            {
                sb.Append('?');
            }
            sb.Append('}');
        }
        else if (part is RoutePatternLiteralPart literalPart)
        {
            sb.Append(literalPart.Content);
        }
        else if (part is RoutePatternSeparatorPart separatorPart)
        {
            sb.Append(separatorPart.Content);
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}
