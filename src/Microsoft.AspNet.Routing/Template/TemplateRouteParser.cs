// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNet.Routing.Template
{
    public static class TemplateRouteParser
    {
        private static string GetLiteral(string segmentLiteral)
        {
            // Scan for errant single { and } and convert double {{ to { and double }} to }

            // First we eliminate all escaped braces and then check if any other braces are remaining
            string newLiteral = segmentLiteral.Replace("{{", String.Empty).Replace("}}", String.Empty);
            if (newLiteral.Contains("{") || newLiteral.Contains("}"))
            {
                return null;
            }

            // If it's a valid format, we unescape the braces
            return segmentLiteral.Replace("{{", "{").Replace("}}", "}");
        }

        private static int IndexOfFirstOpenParameter(string segment, int startIndex)
        {
            // Find the first unescaped open brace
            while (true)
            {
                startIndex = segment.IndexOf('{', startIndex);
                if (startIndex == -1)
                {
                    // If there are no more open braces, stop
                    return -1;
                }
                if ((startIndex + 1 == segment.Length) ||
                    ((startIndex + 1 < segment.Length) && (segment[startIndex + 1] != '{')))
                {
                    // If we found an open brace that is followed by a non-open brace, it's
                    // a parameter delimiter.
                    // It's also a delimiter if the open brace is the last character - though
                    // it ends up being being called out as invalid later on.
                    return startIndex;
                }
                // Increment by two since we want to skip both the open brace that
                // we're on as well as the subsequent character since we know for
                // sure that it is part of an escape sequence.
                startIndex += 2;
            }
        }

        internal static bool IsSeparator(string s)
        {
            return String.Equals(s, "/", StringComparison.Ordinal);
        }

        private static bool IsValidParameterName(string parameterName)
        {
            if (parameterName.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < parameterName.Length; i++)
            {
                char c = parameterName[i];
                if (c == '/' || c == '{' || c == '}')
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsInvalidRouteTemplate(string routeTemplate)
        {
            return routeTemplate.StartsWith("~", StringComparison.Ordinal) ||
                   routeTemplate.StartsWith("/", StringComparison.Ordinal) ||
                   (routeTemplate.IndexOf('?') != -1);
        }

        public static TemplateParsedRoute Parse(string routeTemplate)
        {
            if (routeTemplate == null)
            {
                routeTemplate = String.Empty;
            }

            if (IsInvalidRouteTemplate(routeTemplate))
            {
                throw new ArgumentException(Resources.TemplateRoute_InvalidRouteTemplate, "routeTemplate");
            }

            IList<string> uriParts = SplitUriToPathSegmentStrings(routeTemplate);
            Exception ex = ValidateUriParts(uriParts);
            if (ex != null)
            {
                throw ex;
            }

            IList<PathSegment> pathSegments = SplitUriToPathSegments(uriParts);

            Contract.Assert(uriParts.Count == pathSegments.Count, "The number of string segments should be the same as the number of path segments");

            return new TemplateParsedRoute(pathSegments);
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly",
            Justification = "The exceptions are just constructed here, but they are thrown from a method that does have those parameter names.")]
        private static IList<PathSubsegment> ParseUriSegment(string segment, out Exception exception)
        {
            int startIndex = 0;

            List<PathSubsegment> pathSubsegments = new List<PathSubsegment>();

            while (startIndex < segment.Length)
            {
                int nextParameterStart = IndexOfFirstOpenParameter(segment, startIndex);
                if (nextParameterStart == -1)
                {
                    // If there are no more parameters in the segment, capture the remainder as a literal and stop
                    string lastLiteralPart = GetLiteral(segment.Substring(startIndex));
                    if (lastLiteralPart == null)
                    {
                        exception = new ArgumentException(
                            String.Format(CultureInfo.CurrentCulture, Resources.TemplateRoute_MismatchedParameter, segment),
                            "routeTemplate");

                        return null;
                    }

                    if (lastLiteralPart.Length > 0)
                    {
                        pathSubsegments.Add(new PathLiteralSubsegment(lastLiteralPart));
                    }
                    break;
                }

                int nextParameterEnd = segment.IndexOf('}', nextParameterStart + 1);
                if (nextParameterEnd == -1)
                {
                    exception = new ArgumentException(
                            String.Format(CultureInfo.CurrentCulture, Resources.TemplateRoute_MismatchedParameter, segment),
                            "routeTemplate");
                    return null;
                }

                string literalPart = GetLiteral(segment.Substring(startIndex, nextParameterStart - startIndex));
                if (literalPart == null)
                {
                    exception = new ArgumentException(
                            String.Format(CultureInfo.CurrentCulture, Resources.TemplateRoute_MismatchedParameter, segment),
                            "routeTemplate");
                    return null;
                }

                if (literalPart.Length > 0)
                {
                    pathSubsegments.Add(new PathLiteralSubsegment(literalPart));
                }

                string parameterName = segment.Substring(nextParameterStart + 1, nextParameterEnd - nextParameterStart - 1);
                pathSubsegments.Add(new PathParameterSubsegment(parameterName));

                startIndex = nextParameterEnd + 1;
            }

            exception = null;
            return pathSubsegments;
        }

        private static IList<PathSegment> SplitUriToPathSegments(IList<string> uriParts)
        {
            List<PathSegment> pathSegments = new List<PathSegment>();

            foreach (string pathSegment in uriParts)
            {
                bool isCurrentPartSeparator = IsSeparator(pathSegment);
                if (isCurrentPartSeparator)
                {
                    pathSegments.Add(new PathSeparatorSegment());
                }
                else
                {
                    Exception exception;
                    IList<PathSubsegment> subsegments = ParseUriSegment(pathSegment, out exception);
                    Contract.Assert(exception == null, "This only gets called after the path has been validated, so there should never be an exception here");
                    pathSegments.Add(new PathContentSegment(subsegments));
                }
            }
            return pathSegments;
        }

        internal static IList<string> SplitUriToPathSegmentStrings(string uri)
        {
            List<string> parts = new List<string>();

            if (String.IsNullOrEmpty(uri))
            {
                return parts;
            }

            int currentIndex = 0;

            // Split the incoming URI into individual parts
            while (currentIndex < uri.Length)
            {
                int indexOfNextSeparator = uri.IndexOf('/', currentIndex);
                if (indexOfNextSeparator == -1)
                {
                    // If there are no more separators, the rest of the string is the last part
                    string finalPart = uri.Substring(currentIndex);
                    if (finalPart.Length > 0)
                    {
                        parts.Add(finalPart);
                    }
                    break;
                }

                string nextPart = uri.Substring(currentIndex, indexOfNextSeparator - currentIndex);
                if (nextPart.Length > 0)
                {
                    parts.Add(nextPart);
                }

                Contract.Assert(uri[indexOfNextSeparator] == '/', "The separator char itself should always be a '/'.");
                parts.Add("/");
                currentIndex = indexOfNextSeparator + 1;
            }

            return parts;
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Not changing original algorithm")]
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly",
            Justification = "The exceptions are just constructed here, but they are thrown from a method that does have those parameter names.")]
        private static Exception ValidateUriParts(IList<string> pathSegments)
        {
            Contract.Assert(pathSegments != null, "The value should always come from SplitUri(), and that function should never return null.");

            HashSet<string> usedParameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool? isPreviousPartSeparator = null;

            bool foundCatchAllParameter = false;

            foreach (string pathSegment in pathSegments)
            {
                if (foundCatchAllParameter)
                {
                    // If we ever start an iteration of the loop and we've already found a
                    // catchall parameter then we have an invalid URI format.
                    return new ArgumentException(Resources.TemplateRoute_CatchAllMustBeLast, "routeTemplate");
                }

                bool isCurrentPartSeparator;
                if (isPreviousPartSeparator == null)
                {
                    // Prime the loop with the first value
                    isPreviousPartSeparator = IsSeparator(pathSegment);
                    isCurrentPartSeparator = isPreviousPartSeparator.Value;
                }
                else
                {
                    isCurrentPartSeparator = IsSeparator(pathSegment);

                    // If both the previous part and the current part are separators, it's invalid
                    if (isCurrentPartSeparator && isPreviousPartSeparator.Value)
                    {
                        return new ArgumentException(Resources.TemplateRoute_CannotHaveConsecutiveSeparators, "routeTemplate");
                    }

                    Contract.Assert(isCurrentPartSeparator != isPreviousPartSeparator.Value, "This assert should only happen if both the current and previous parts are non-separators. This should never happen because consecutive non-separators are always parsed as a single part.");
                    isPreviousPartSeparator = isCurrentPartSeparator;
                }

                // If it's not a separator, parse the segment for parameters and validate it
                if (!isCurrentPartSeparator)
                {
                    Exception exception;
                    IList<PathSubsegment> subsegments = ParseUriSegment(pathSegment, out exception);
                    if (exception != null)
                    {
                        return exception;
                    }

                    exception = ValidateUriSegment(subsegments, usedParameterNames);
                    if (exception != null)
                    {
                        return exception;
                    }

                    foundCatchAllParameter = subsegments.Any<PathSubsegment>(seg => (seg is PathParameterSubsegment) && ((PathParameterSubsegment)seg).IsCatchAll);
                }
            }
            return null;
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly",
            Justification = "The exceptions are just constructed here, but they are thrown from a method that does have those parameter names.")]
        private static Exception ValidateUriSegment(IList<PathSubsegment> pathSubsegments, HashSet<string> usedParameterNames)
        {
            bool segmentContainsCatchAll = false;

            Type previousSegmentType = null;

            foreach (PathSubsegment subsegment in pathSubsegments)
            {
                if (previousSegmentType != null)
                {
                    if (previousSegmentType == subsegment.GetType())
                    {
                        return new ArgumentException(Resources.TemplateRoute_CannotHaveConsecutiveParameters, "routeTemplate");
                    }
                }
                previousSegmentType = subsegment.GetType();

                PathLiteralSubsegment literalSubsegment = subsegment as PathLiteralSubsegment;
                if (literalSubsegment != null)
                {
                    // Nothing to validate for literals - everything is valid
                }
                else
                {
                    PathParameterSubsegment parameterSubsegment = subsegment as PathParameterSubsegment;
                    if (parameterSubsegment != null)
                    {
                        string parameterName = parameterSubsegment.ParameterName;

                        if (parameterSubsegment.IsCatchAll)
                        {
                            segmentContainsCatchAll = true;
                        }

                        // Check for valid characters in the parameter name
                        if (!IsValidParameterName(parameterName))
                        {
                            return new ArgumentException(
                                String.Format(CultureInfo.CurrentCulture, Resources.TemplateRoute_InvalidParameterName, parameterName),
                                "routeTemplate");
                        }

                        if (usedParameterNames.Contains(parameterName))
                        {
                            return new ArgumentException(
                                String.Format(CultureInfo.CurrentCulture, Resources.TemplateRoute_RepeatedParameter, parameterName),
                                "routeTemplate");
                        }
                        else
                        {
                            usedParameterNames.Add(parameterName);
                        }
                    }
                    else
                    {
                        Contract.Assert(false, "Invalid path subsegment type");
                    }
                }
            }

            if (segmentContainsCatchAll && (pathSubsegments.Count != 1))
            {
                return new ArgumentException(Resources.TemplateRoute_CannotHaveCatchAllInMultiSegment, "routeTemplate");
            }

            return null;
        }
    }
}
