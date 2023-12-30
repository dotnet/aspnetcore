// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Buffers;
using System.Diagnostics;
#if COMPONENTS
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Routing.Patterns;
#endif

namespace Microsoft.AspNetCore.Routing.Patterns;

internal static class RoutePatternParser
{
    private const char Separator = '/';
    private const char OpenBrace = '{';
    private const char CloseBrace = '}';
    private const char QuestionMark = '?';
    private const string PeriodString = ".";

    internal static readonly SearchValues<char> InvalidParameterNameChars = SearchValues.Create("/{}?*");

    public static RoutePattern Parse(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var trimmedPattern = TrimPrefix(pattern);

        var context = new Context(trimmedPattern);
        var segments = new List<RoutePatternPathSegment>();

        while (context.MoveNext())
        {
            var i = context.Index;

            if (context.Current == Separator)
            {
                // If we get here is means that there's a consecutive '/' character.
                // Templates don't start with a '/' and parsing a segment consumes the separator.
                throw new RoutePatternException(pattern, Resources.TemplateRoute_CannotHaveConsecutiveSeparators);
            }

            if (!ParseSegment(context, segments))
            {
                throw new RoutePatternException(pattern, context.Error);
            }

            // A successful parse should always result in us being at the end or at a separator.
            Debug.Assert(context.AtEnd() || context.Current == Separator);

            if (context.Index <= i)
            {
                // This shouldn't happen, but we want to crash if it does.
                var message = "Infinite loop detected in the parser. Please open an issue.";
                throw new InvalidProgramException(message);
            }
        }

        if (IsAllValid(context, segments))
        {
            return RoutePatternFactory.Pattern(pattern, segments);
        }
        else
        {
            throw new RoutePatternException(pattern, context.Error);
        }
    }

    private static bool ParseSegment(Context context, List<RoutePatternPathSegment> segments)
    {
        Debug.Assert(context != null);
        Debug.Assert(segments != null);

        var parts = new List<RoutePatternPart>();

        while (true)
        {
            var i = context.Index;

            if (context.Current == OpenBrace)
            {
                if (!context.MoveNext())
                {
                    // This is a dangling open-brace, which is not allowed
                    context.Error = Resources.TemplateRoute_MismatchedParameter;
                    return false;
                }

                if (context.Current == OpenBrace)
                {
                    // This is an 'escaped' brace in a literal, like "{{foo"
                    context.Back();
                    if (!ParseLiteral(context, parts))
                    {
                        return false;
                    }
                }
                else
                {
                    // This is a parameter
                    context.Back();
                    if (!ParseParameter(context, parts))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!ParseLiteral(context, parts))
                {
                    return false;
                }
            }

            if (context.Current == Separator || context.AtEnd())
            {
                // We've reached the end of the segment
                break;
            }

            if (context.Index <= i)
            {
                // This shouldn't happen, but we want to crash if it does.
                var message = "Infinite loop detected in the parser. Please open an issue.";
                throw new InvalidProgramException(message);
            }
        }

        if (IsSegmentValid(context, parts))
        {
            segments.Add(new RoutePatternPathSegment(parts));
            return true;
        }
        else
        {
            return false;
        }
    }

    private static bool ParseParameter(Context context, List<RoutePatternPart> parts)
    {
        Debug.Assert(context.Current == OpenBrace);
        context.Mark();

        context.MoveNext();

        while (true)
        {
            if (context.Current == OpenBrace)
            {
                // This is an open brace inside of a parameter, it has to be escaped
                if (context.MoveNext())
                {
                    if (context.Current != OpenBrace)
                    {
                        // If we see something like "{p1:regex(^\d{3", we will come here.
                        context.Error = Resources.TemplateRoute_UnescapedBrace;
                        return false;
                    }
                }
                else
                {
                    // This is a dangling open-brace, which is not allowed
                    // Example: "{p1:regex(^\d{"
                    context.Error = Resources.TemplateRoute_MismatchedParameter;
                    return false;
                }
            }
            else if (context.Current == CloseBrace)
            {
                // When we encounter Closed brace here, it either means end of the parameter or it is a closed
                // brace in the parameter, in that case it needs to be escaped.
                // Example: {p1:regex(([}}])\w+}. First pair is escaped one and last marks end of the parameter
                if (!context.MoveNext())
                {
                    // This is the end of the string -and we have a valid parameter
                    break;
                }

                if (context.Current == CloseBrace)
                {
                    // This is an 'escaped' brace in a parameter name
                }
                else
                {
                    // This is the end of the parameter
                    break;
                }
            }

            if (!context.MoveNext())
            {
                // This is a dangling open-brace, which is not allowed
                context.Error = Resources.TemplateRoute_MismatchedParameter;
                return false;
            }
        }

        var text = context.Capture();
        if (text == "{}")
        {
            context.Error = Resources.FormatTemplateRoute_InvalidParameterName(string.Empty);
            return false;
        }

        var inside = text.Substring(1, text.Length - 2);
        var decoded = inside.Replace("}}", "}").Replace("{{", "{");

        // At this point, we need to parse the raw name for inline constraint,
        // default values and optional parameters.
        var templatePart = RouteParameterParser.ParseRouteParameter(decoded);

        // See #475 - this is here because InlineRouteParameterParser can't return errors
        if (decoded.StartsWith('*') && decoded.EndsWith('?'))
        {
            context.Error = Resources.TemplateRoute_CatchAllCannotBeOptional;
            return false;
        }

        if (templatePart.IsOptional && templatePart.Default != null)
        {
            // Cannot be optional and have a default value.
            // The only way to declare an optional parameter is to have a ? at the end,
            // hence we cannot have both default value and optional parameter within the template.
            // A workaround is to add it as a separate entry in the defaults argument.
            context.Error = Resources.TemplateRoute_OptionalCannotHaveDefaultValue;
            return false;
        }

        var parameterName = templatePart.Name;
        if (IsValidParameterName(context, parameterName))
        {
            parts.Add(templatePart);
            return true;
        }
        else
        {
            return false;
        }
    }

    private static bool ParseLiteral(Context context, List<RoutePatternPart> parts)
    {
        context.Mark();

        while (true)
        {
            if (context.Current == Separator)
            {
                // End of the segment
                break;
            }
            else if (context.Current == OpenBrace)
            {
                if (!context.MoveNext())
                {
                    // This is a dangling open-brace, which is not allowed
                    context.Error = Resources.TemplateRoute_MismatchedParameter;
                    return false;
                }

                if (context.Current == OpenBrace)
                {
                    // This is an 'escaped' brace in a literal, like "{{foo" - keep going.
                }
                else
                {
                    // We've just seen the start of a parameter, so back up.
                    context.Back();
                    break;
                }
            }
            else if (context.Current == CloseBrace)
            {
                if (!context.MoveNext())
                {
                    // This is a dangling close-brace, which is not allowed
                    context.Error = Resources.TemplateRoute_MismatchedParameter;
                    return false;
                }

                if (context.Current == CloseBrace)
                {
                    // This is an 'escaped' brace in a literal, like "{{foo" - keep going.
                }
                else
                {
                    // This is an unbalanced close-brace, which is not allowed
                    context.Error = Resources.TemplateRoute_MismatchedParameter;
                    return false;
                }
            }

            if (!context.MoveNext())
            {
                break;
            }
        }

        var encoded = context.Capture();
        var decoded = encoded.Replace("}}", "}").Replace("{{", "{");
        if (IsValidLiteral(context, decoded))
        {
            parts.Add(RoutePatternFactory.LiteralPart(decoded));
            return true;
        }
        else
        {
            return false;
        }
    }

    private static bool IsAllValid(Context context, List<RoutePatternPathSegment> segments)
    {
        // A catch-all parameter must be the last part of the last segment
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            for (var j = 0; j < segment.Parts.Count; j++)
            {
                var part = segment.Parts[j];
                if (part is RoutePatternParameterPart parameter
                    && parameter.IsCatchAll &&
                    (i != segments.Count - 1 || j != segment.Parts.Count - 1))
                {
                    context.Error = Resources.TemplateRoute_CatchAllMustBeLast;
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsSegmentValid(Context context, List<RoutePatternPart> parts)
    {
        // If a segment has multiple parts, then it can't contain a catch all.
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part is RoutePatternParameterPart parameter && parameter.IsCatchAll && parts.Count > 1)
            {
                context.Error = Resources.TemplateRoute_CannotHaveCatchAllInMultiSegment;
                return false;
            }
        }

        // if a segment has multiple parts, then only the last one parameter can be optional
        // if it is following a optional separator.
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];

            if (part is RoutePatternParameterPart parameter && parameter.IsOptional && parts.Count > 1)
            {
                // This optional parameter is the last part in the segment
                if (i == parts.Count - 1)
                {
                    var previousPart = parts[i - 1];

                    if (!previousPart.IsLiteral && !previousPart.IsSeparator)
                    {
                        // The optional parameter is preceded by something that is not a literal or separator
                        // Example of error message:
                        // "In the segment '{RouteValue}{param?}', the optional parameter 'param' is preceded
                        // by an invalid segment '{RouteValue}'. Only a period (.) can precede an optional parameter.
                        context.Error = Resources.FormatTemplateRoute_OptionalParameterCanbBePrecededByPeriod(
                            RoutePatternPathSegment.DebuggerToString(parts),
                            parameter.Name,
                            parts[i - 1].DebuggerToString());

                        return false;
                    }
                    else if (previousPart is RoutePatternLiteralPart literal && literal.Content != PeriodString)
                    {
                        // The optional parameter is preceded by a literal other than period.
                        // Example of error message:
                        // "In the segment '{RouteValue}-{param?}', the optional parameter 'param' is preceded
                        // by an invalid segment '-'. Only a period (.) can precede an optional parameter.
                        context.Error = Resources.FormatTemplateRoute_OptionalParameterCanbBePrecededByPeriod(
                            RoutePatternPathSegment.DebuggerToString(parts),
                            parameter.Name,
                            parts[i - 1].DebuggerToString());

                        return false;
                    }

                    parts[i - 1] = RoutePatternFactory.SeparatorPart(((RoutePatternLiteralPart)previousPart).Content);
                }
                else
                {
                    // This optional parameter is not the last one in the segment
                    // Example:
                    // An optional parameter must be at the end of the segment. In the segment '{RouteValue?})',
                    // optional parameter 'RouteValue' is followed by ')'
                    context.Error = Resources.FormatTemplateRoute_OptionalParameterHasTobeTheLast(
                        RoutePatternPathSegment.DebuggerToString(parts),
                        parameter.Name,
                        parts[i + 1].DebuggerToString());

                    return false;
                }
            }
        }

        // A segment cannot contain two consecutive parameters
        var isLastSegmentParameter = false;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.IsParameter && isLastSegmentParameter)
            {
                context.Error = Resources.TemplateRoute_CannotHaveConsecutiveParameters;
                return false;
            }

            isLastSegmentParameter = part.IsParameter;
        }

        return true;
    }

    private static bool IsValidParameterName(Context context, string parameterName)
    {
        if (parameterName.Length == 0 || parameterName.AsSpan().IndexOfAny(InvalidParameterNameChars) >= 0)
        {
            context.Error = Resources.FormatTemplateRoute_InvalidParameterName(parameterName);
            return false;
        }

        if (!context.ParameterNames.Add(parameterName))
        {
            context.Error = Resources.FormatTemplateRoute_RepeatedParameter(parameterName);
            return false;
        }

        return true;
    }

    private static bool IsValidLiteral(Context context, string literal)
    {
        Debug.Assert(context != null);
        Debug.Assert(literal != null);

        if (literal.Contains(QuestionMark))
        {
            context.Error = Resources.FormatTemplateRoute_InvalidLiteral(literal);
            return false;
        }

        return true;
    }

    private static string TrimPrefix(string routePattern)
    {
        if (routePattern.StartsWith("~/", StringComparison.Ordinal))
        {
            return routePattern.Substring(2);
        }
        else if (routePattern.StartsWith('/'))
        {
            return routePattern.Substring(1);
        }
        else if (routePattern.StartsWith('~'))
        {
            throw new RoutePatternException(routePattern, Resources.TemplateRoute_InvalidRouteTemplate);
        }
        return routePattern;
    }

    [DebuggerDisplay("{DebuggerToString()}")]
    private sealed class Context
    {
        private readonly string _template;
        private int _index;
        private int? _mark;

        private readonly HashSet<string> _parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public Context(string template)
        {
            Debug.Assert(template != null);
            _template = template;

            _index = -1;
        }

        public char Current
        {
            get { return (_index < _template.Length && _index >= 0) ? _template[_index] : (char)0; }
        }

        public int Index => _index;

        public string Error
        {
            get;
            set;
        }

        public HashSet<string> ParameterNames
        {
            get { return _parameterNames; }
        }

        public bool Back()
        {
            return --_index >= 0;
        }

        public bool AtEnd()
        {
            return _index >= _template.Length;
        }

        public bool MoveNext()
        {
            return ++_index < _template.Length;
        }

        public void Mark()
        {
            Debug.Assert(_index >= 0);

            // Index is always the index of the character *past* Current - we want to 'mark' Current.
            _mark = _index;
        }

        public string Capture()
        {
            if (_mark.HasValue)
            {
                var value = _template.Substring(_mark.Value, _index - _mark.Value);
                _mark = null;
                return value;
            }
            else
            {
                return null;
            }
        }

        private string DebuggerToString()
        {
            if (_index == -1)
            {
                return _template;
            }
            else if (_mark.HasValue)
            {
                return _template.Substring(0, _mark.Value) +
                    "|" +
                    _template.Substring(_mark.Value, _index - _mark.Value) +
                    "|" +
                    _template.Substring(_index);
            }
            else
            {
                return string.Concat(_template.Substring(0, _index), "|", _template.Substring(_index));
            }
        }
    }
}
