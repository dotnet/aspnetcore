// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNet.Routing.Template
{
    public static class TemplateParser
    {
        private const char Separator = '/';
        private const char OpenBrace = '{';
        private const char CloseBrace = '}';
        private const char EqualsSign = '=';
        private const char QuestionMark = '?';
        
        public static Template Parse(string routeTemplate)
        {
            if (routeTemplate == null)
            {
                routeTemplate = String.Empty;
            }

            if (IsInvalidRouteTemplate(routeTemplate))
            {
                throw new ArgumentException(Resources.TemplateRoute_InvalidRouteTemplate, "routeTemplate");
            }

            var context = new TemplateParserContext(routeTemplate);
            var segments = new List<TemplateSegment>();

            while (context.Next())
            {
                if (context.Current == Separator)
                {
                    // If we get here is means that there's a consecutive '/' character. Templates don't start with a '/' and
                    // parsing a segment consumes the separator.
                    throw new ArgumentException(Resources.TemplateRoute_CannotHaveConsecutiveSeparators, "routeTemplate");
                }
                else
                {
                    if (!ParseSegment(context, segments))
                    {
                        throw new ArgumentException(context.Error, "routeTemplate");
                    }
                }
            }

            if (IsAllValid(context, segments))
            {
                return new Template(segments);
            }
            else
            {
                throw new ArgumentException(context.Error, "routeTemplate");
            }
        }

        private static bool ParseSegment(TemplateParserContext context, List<TemplateSegment> segments)
        {
            Contract.Assert(context != null);
            Contract.Assert(segments != null);

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
                if (context.Current == Separator)
                {
                    // This is a dangling open-brace, which is not allowed
                    context.Error = Resources.TemplateRoute_MismatchedParameter;
                    return false;
                }
                else if (context.Current == OpenBrace)
                {
                    // If we see a '{' while parsing a parameter name it's invalid. We'll just accept it for now
                    // and let the validation code for the name find it.
                }
                else if (context.Current == CloseBrace)
                {
                    if (!context.Next())
                    {
                        // This is the end of the string - and we have a valid parameter
                        context.Back();
                        break;
                    }

                    if (context.Current == CloseBrace)
                    {
                        // This is an 'escaped' brace in a parameter name, which is not allowed. We'll just accept it for now
                        // and let the validation code for the name find it.
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

            var rawName = context.Capture();

            var isCatchAll = rawName.StartsWith("*", StringComparison.Ordinal);
            var isOptional = rawName.EndsWith("?", StringComparison.Ordinal);

            if (isCatchAll && isOptional)
            {
                context.Error = Resources.TemplateRoute_CatchAllCannotBeOptional;
                return false;
            }

            rawName = isCatchAll ? rawName.Substring(1) : rawName;
            rawName = isOptional ? rawName.Substring(0, rawName.Length - 1) : rawName;

            var parameterName = rawName;
            if (IsValidParameterName(context, parameterName))
            {
                segment.Parts.Add(TemplatePart.CreateParameter(parameterName, isCatchAll, isOptional));
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

            var decoded = encoded.Replace("}}", "}").Replace("{{", "}");
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
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                for (int j = 0; j < segment.Parts.Count; j++)
                {
                    var part = segment.Parts[j];
                    if (part.IsParameter && part.IsCatchAll && (i != segments.Count - 1 || j != segment.Parts.Count - 1))
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
            for (int i = 0; i < segment.Parts.Count; i++)
            {
                var part = segment.Parts[i];
                if (part.IsParameter && part.IsCatchAll && segment.Parts.Count > 1)
                {
                    context.Error = Resources.TemplateRoute_CannotHaveCatchAllInMultiSegment;
                    return false;
                }
            }

            // if a segment has multiple parts, then the parameters can't be optional
            for (int i = 0; i < segment.Parts.Count; i++)
            {
                var part = segment.Parts[i];
                if (part.IsParameter && part.IsOptional && segment.Parts.Count > 1)
                {
                    context.Error = Resources.TemplateRoute_CannotHaveOptionalParameterInMultiSegment;
                    return false;
                }
            }

            // A segment cannot containt two consecutive parameters
            var isLastSegmentParameter = false;
            for (int i = 0; i < segment.Parts.Count; i++)
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
                context.Error = String.Format(CultureInfo.CurrentCulture, Resources.TemplateRoute_InvalidParameterName, parameterName);
                return false;
            }

            for (int i = 0; i < parameterName.Length; i++)
            {
                var c = parameterName[i];
                if (c == Separator || c == OpenBrace || c == CloseBrace || c == QuestionMark)
                {
                    context.Error = String.Format(CultureInfo.CurrentCulture, Resources.TemplateRoute_InvalidParameterName, parameterName);
                    return false;
                }
            }

            if (!context.ParameterNames.Add(parameterName))
            {
                context.Error = String.Format(CultureInfo.CurrentCulture, Resources.TemplateRoute_RepeatedParameter, parameterName);
                return false;
            }

            return true;
        }

        private static bool IsValidLiteral(TemplateParserContext context, string literal)
        {
            Contract.Assert(context != null);
            Contract.Assert(literal != null);

            if (literal.IndexOf(QuestionMark) != -1)
            {
                context.Error = String.Format(CultureInfo.CurrentCulture, Resources.TemplateRoute_InvalidLiteral, literal);
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
                Contract.Assert(template != null);
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
