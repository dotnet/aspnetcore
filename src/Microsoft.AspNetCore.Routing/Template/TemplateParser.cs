// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.AspNet.Routing.Template
{
    public static class TemplateParser
    {
        private const char Separator = '/';
        private const char OpenBrace = '{';
        private const char CloseBrace = '}';
        private const char EqualsSign = '=';
        private const char QuestionMark = '?';
        private const char Asterisk = '*';
        private const string PeriodString = ".";

        public static RouteTemplate Parse(string routeTemplate)
        {
            if (routeTemplate == null)
            {
                routeTemplate = String.Empty;
            }

            if (IsInvalidRouteTemplate(routeTemplate))
            {
                throw new ArgumentException(Resources.TemplateRoute_InvalidRouteTemplate, nameof(routeTemplate));
            }

            var context = new TemplateParserContext(routeTemplate);
            var segments = new List<TemplateSegment>();

            while (context.Next())
            {
                if (context.Current == Separator)
                {
                    // If we get here is means that there's a consecutive '/' character.
                    // Templates don't start with a '/' and parsing a segment consumes the separator.
                    throw new ArgumentException(Resources.TemplateRoute_CannotHaveConsecutiveSeparators,
                                                nameof(routeTemplate));
                }
                else
                {
                    if (!ParseSegment(context, segments))
                    {
                        throw new ArgumentException(context.Error, nameof(routeTemplate));
                    }
                }
            }

            if (IsAllValid(context, segments))
            {
                return new RouteTemplate(routeTemplate, segments);
            }
            else
            {
                throw new ArgumentException(context.Error, nameof(routeTemplate));
            }
        }

        private static bool ParseSegment(TemplateParserContext context, List<TemplateSegment> segments)
        {
            Debug.Assert(context != null);
            Debug.Assert(segments != null);

            var segment = new TemplateSegment();

            while (true)
            {
                if (context.Current == OpenBrace)
                {
                    if (!context.Next())
                    {
                        // This is a dangling open-brace, which is not allowed
                        context.Error = Resources.TemplateRoute_MismatchedParameter;
                        return false;
                    }

                    if (context.Current == OpenBrace)
                    {
                        // This is an 'escaped' brace in a literal, like "{{foo"
                        context.Back();
                        if (!ParseLiteral(context, segment))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // This is the inside of a parameter
                        if (!ParseParameter(context, segment))
                        {
                            return false;
                        }
                    }
                }
                else if (context.Current == Separator)
                {
                    // We've reached the end of the segment
                    break;
                }
                else
                {
                    if (!ParseLiteral(context, segment))
                    {
                        return false;
                    }
                }

                if (!context.Next())
                {
                    // We've reached the end of the string
                    break;
                }
            }

            if (IsSegmentValid(context, segment))
            {
                segments.Add(segment);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ParseParameter(TemplateParserContext context, TemplateSegment segment)
        {
            context.Mark();

            while (true)
            {
                if (context.Current == OpenBrace)
                {
                    // This is an open brace inside of a parameter, it has to be escaped
                    if (context.Next())
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
                    if (!context.Next())
                    {
                        // This is the end of the string -and we have a valid parameter
                        context.Back();
                        break;
                    }

                    if (context.Current == CloseBrace)
                    {
                        // This is an 'escaped' brace in a parameter name
                    }
                    else
                    {
                        // This is the end of the parameter
                        context.Back();
                        break;
                    }
                }

                if (!context.Next())
                {
                    // This is a dangling open-brace, which is not allowed
                    context.Error = Resources.TemplateRoute_MismatchedParameter;
                    return false;
                }
            }

            var rawParameter = context.Capture();
            var decoded = rawParameter.Replace("}}", "}").Replace("{{", "{");

            // At this point, we need to parse the raw name for inline constraint,
            // default values and optional parameters.
            var templatePart = InlineRouteParameterParser.ParseRouteParameter(decoded);

            if (templatePart.IsCatchAll && templatePart.IsOptional)
            {
                context.Error = Resources.TemplateRoute_CatchAllCannotBeOptional;
                return false;
            }

            if (templatePart.IsOptional && templatePart.DefaultValue != null)
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
                segment.Parts.Add(templatePart);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ParseLiteral(TemplateParserContext context, TemplateSegment segment)
        {
            context.Mark();

            string encoded;
            while (true)
            {
                if (context.Current == Separator)
                {
                    encoded = context.Capture();
                    context.Back();
                    break;
                }
                else if (context.Current == OpenBrace)
                {
                    if (!context.Next())
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
                        // We've just seen the start of a parameter, so back up and return
                        context.Back();
                        encoded = context.Capture();
                        context.Back();
                        break;
                    }
                }
                else if (context.Current == CloseBrace)
                {
                    if (!context.Next())
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

                if (!context.Next())
                {
                    encoded = context.Capture();
                    break;
                }
            }

            var decoded = encoded.Replace("}}", "}").Replace("{{", "{");
            if (IsValidLiteral(context, decoded))
            {
                segment.Parts.Add(TemplatePart.CreateLiteral(decoded));
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsAllValid(TemplateParserContext context, List<TemplateSegment> segments)
        {
            // A catch-all parameter must be the last part of the last segment
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                for (var j = 0; j < segment.Parts.Count; j++)
                {
                    var part = segment.Parts[j];
                    if (part.IsParameter &&
                        part.IsCatchAll &&
                        (i != segments.Count - 1 || j != segment.Parts.Count - 1))
                    {
                        context.Error = Resources.TemplateRoute_CatchAllMustBeLast;
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsSegmentValid(TemplateParserContext context, TemplateSegment segment)
        {
            // If a segment has multiple parts, then it can't contain a catch all.
            for (var i = 0; i < segment.Parts.Count; i++)
            {
                var part = segment.Parts[i];
                if (part.IsParameter && part.IsCatchAll && segment.Parts.Count > 1)
                {
                    context.Error = Resources.TemplateRoute_CannotHaveCatchAllInMultiSegment;
                    return false;
                }
            }

            // if a segment has multiple parts, then only the last one parameter can be optional 
            // if it is following a optional seperator. 
            for (var i = 0; i < segment.Parts.Count; i++)
            {
                var part = segment.Parts[i];

                if (part.IsParameter && part.IsOptional && segment.Parts.Count > 1)
                {
                    // This optional parameter is the last part in the segment
                    if (i == segment.Parts.Count - 1)
                    {
                        Debug.Assert(segment.Parts[i - 1].IsLiteral);

                        // the optional parameter is preceded by a period
                        if (segment.Parts[i - 1].Text == PeriodString)
                        {
                            segment.Parts[i - 1].IsOptionalSeperator = true;
                        }
                        else
                        {
                            // The optional parameter is preceded by a literal other than period
                            // Example of error message:
                            // "In the complex segment {RouteValue}-{param?}, the optional parameter 'param'is preceded
                            // by an invalid segment "-". Only valid literal to precede an optional parameter is a 
                            // period (.).
                            context.Error = string.Format(
                                Resources.TemplateRoute_OptionalParameterCanbBePrecededByPeriod,
                                segment.DebuggerToString(),
                                part.Name,
                                segment.Parts[i - 1].Text);

                            return false;
                        }
                    }
                    else
                    {
                        // This optional parameter is not the last one in the segment
                        // Example:
                        // An optional parameter must be at the end of the segment.In the segment '{RouteValue?})', 
                        // optional parameter 'RouteValue' is followed by ')'
                        var nextPart = segment.Parts[i + 1];
                        var invalidPartText = nextPart.IsParameter ? nextPart.Name : nextPart.Text;

                        context.Error = string.Format(
                            Resources.TemplateRoute_OptionalParameterHasTobeTheLast,
                            segment.DebuggerToString(),
                            segment.Parts[i].Name,
                            invalidPartText
                            );

                        return false;
                    }
                }
            }

            // A segment cannot contain two consecutive parameters
            var isLastSegmentParameter = false;
            for (var i = 0; i < segment.Parts.Count; i++)
            {
                var part = segment.Parts[i];
                if (part.IsParameter && isLastSegmentParameter)
                {
                    context.Error = Resources.TemplateRoute_CannotHaveConsecutiveParameters;
                    return false;
                }

                isLastSegmentParameter = part.IsParameter;
            }

            return true;
        }

        private static bool IsValidParameterName(TemplateParserContext context, string parameterName)
        {
            if (parameterName.Length == 0)
            {
                context.Error = String.Format(CultureInfo.CurrentCulture,
                                              Resources.TemplateRoute_InvalidParameterName, parameterName);
                return false;
            }

            for (var i = 0; i < parameterName.Length; i++)
            {
                var c = parameterName[i];
                if (c == Separator || c == OpenBrace || c == CloseBrace || c == QuestionMark || c == Asterisk)
                {
                    context.Error = String.Format(CultureInfo.CurrentCulture,
                                                  Resources.TemplateRoute_InvalidParameterName, parameterName);
                    return false;
                }
            }

            if (!context.ParameterNames.Add(parameterName))
            {
                context.Error = String.Format(CultureInfo.CurrentCulture,
                                              Resources.TemplateRoute_RepeatedParameter, parameterName);
                return false;
            }

            return true;
        }

        private static bool IsValidLiteral(TemplateParserContext context, string literal)
        {
            Debug.Assert(context != null);
            Debug.Assert(literal != null);

            if (literal.IndexOf(QuestionMark) != -1)
            {
                context.Error = String.Format(CultureInfo.CurrentCulture,
                                              Resources.TemplateRoute_InvalidLiteral, literal);
                return false;
            }

            return true;
        }

        private static bool IsInvalidRouteTemplate(string routeTemplate)
        {
            return routeTemplate.StartsWith("~", StringComparison.Ordinal) ||
                   routeTemplate.StartsWith("/", StringComparison.Ordinal);
        }

        private class TemplateParserContext
        {
            private readonly string _template;
            private int _index;
            private int? _mark;

            private HashSet<string> _parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public TemplateParserContext(string template)
            {
                Debug.Assert(template != null);
                _template = template;

                _index = -1;
            }

            public char Current
            {
                get { return (_index < _template.Length && _index >= 0) ? _template[_index] : (char)0; }
            }

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

            public bool Next()
            {
                return ++_index < _template.Length;
            }

            public void Mark()
            {
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
        }
    }
}
