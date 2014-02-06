// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Microsoft.AspNet.Routing.Template
{
    public class TemplateParsedRoute
    {
        public TemplateParsedRoute(IList<PathSegment> pathSegments)
        {
            Contract.Assert(pathSegments != null);
            PathSegments = pathSegments;
        }

        internal IList<PathSegment> PathSegments { get; private set; }

        private static bool ForEachParameter(IList<PathSegment> pathSegments, Func<PathParameterSubsegment, bool> action)
        {
            for (int i = 0; i < pathSegments.Count; i++)
            {
                PathSegment pathSegment = pathSegments[i];

                if (pathSegment is PathSeparatorSegment)
                {
                    // We only care about parameter subsegments, so skip this
                    continue;
                }
                else
                {
                    PathContentSegment contentPathSegment = pathSegment as PathContentSegment;
                    if (contentPathSegment != null)
                    {
                        foreach (PathSubsegment subsegment in contentPathSegment.Subsegments)
                        {
                            PathLiteralSubsegment literalSubsegment = subsegment as PathLiteralSubsegment;
                            if (literalSubsegment != null)
                            {
                                // We only care about parameter subsegments, so skip this
                                continue;
                            }
                            else
                            {
                                PathParameterSubsegment parameterSubsegment = subsegment as PathParameterSubsegment;
                                if (parameterSubsegment != null)
                                {
                                    if (!action(parameterSubsegment))
                                    {
                                        return false;
                                    }
                                }
                                else
                                {
                                    Contract.Assert(false, "Invalid path subsegment type");
                                }
                            }
                        }
                    }
                    else
                    {
                        Contract.Assert(false, "Invalid path segment type");
                    }
                }
            }

            return true;
        }

        public IDictionary<string, object> Match(string virtualPath, IDictionary<string, object> defaultValues)
        {
            IList<string> requestPathSegments = TemplateRouteParser.SplitUriToPathSegmentStrings(virtualPath);

            if (defaultValues == null)
            {
                defaultValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            IDictionary<string, object> matchedValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // This flag gets set once all the data in the URI has been parsed through, but
            // the route we're trying to match against still has more parts. At this point
            // we'll only continue matching separator characters and parameters that have
            // default values.
            bool ranOutOfStuffToParse = false;

            // This value gets set once we start processing a catchall parameter (if there is one
            // at all). Once we set this value we consume all remaining parts of the URI into its
            // parameter value.
            bool usedCatchAllParameter = false;

            for (int i = 0; i < PathSegments.Count; i++)
            {
                PathSegment pathSegment = PathSegments[i];

                if (requestPathSegments.Count <= i)
                {
                    ranOutOfStuffToParse = true;
                }

                string requestPathSegment = ranOutOfStuffToParse ? null : requestPathSegments[i];

                if (pathSegment is PathSeparatorSegment)
                {
                    if (ranOutOfStuffToParse)
                    {
                        // If we're trying to match a separator in the route but there's no more content, that's OK
                    }
                    else
                    {
                        if (!String.Equals(requestPathSegment, "/", StringComparison.Ordinal))
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    PathContentSegment contentPathSegment = pathSegment as PathContentSegment;
                    if (contentPathSegment != null)
                    {
                        if (contentPathSegment.IsCatchAll)
                        {
                            Contract.Assert(i == (PathSegments.Count - 1), "If we're processing a catch-all, we should be on the last route segment.");
                            MatchCatchAll(contentPathSegment, requestPathSegments.Skip(i), defaultValues, matchedValues);
                            usedCatchAllParameter = true;
                        }
                        else
                        {
                            if (!MatchContentPathSegment(contentPathSegment, requestPathSegment, defaultValues, matchedValues))
                            {
                                return null;
                            }
                        }
                    }
                    else
                    {
                        Contract.Assert(false, "Invalid path segment type");
                    }
                }
            }

            if (!usedCatchAllParameter)
            {
                if (PathSegments.Count < requestPathSegments.Count)
                {
                    // If we've already gone through all the parts defined in the route but the URI
                    // still contains more content, check that the remaining content is all separators.
                    for (int i = PathSegments.Count; i < requestPathSegments.Count; i++)
                    {
                        if (!TemplateRouteParser.IsSeparator(requestPathSegments[i]))
                        {
                            return null;
                        }
                    }
                }
            }

            // Copy all remaining default values to the route data
            if (defaultValues != null)
            {
                foreach (var defaultValue in defaultValues)
                {
                    if (!matchedValues.ContainsKey(defaultValue.Key))
                    {
                        matchedValues.Add(defaultValue.Key, defaultValue.Value);
                    }
                }
            }

            return matchedValues;
        }

        private static void MatchCatchAll(PathContentSegment contentPathSegment, IEnumerable<string> remainingRequestSegments, IDictionary<string, object> defaultValues, IDictionary<string, object> matchedValues)
        {
            string remainingRequest = String.Join(String.Empty, remainingRequestSegments.ToArray());

            PathParameterSubsegment catchAllSegment = contentPathSegment.Subsegments[0] as PathParameterSubsegment;

            object catchAllValue;

            if (remainingRequest.Length > 0)
            {
                catchAllValue = remainingRequest;
            }
            else
            {
                defaultValues.TryGetValue(catchAllSegment.ParameterName, out catchAllValue);
            }

            matchedValues.Add(catchAllSegment.ParameterName, catchAllValue);
        }

        private static bool MatchContentPathSegment(PathContentSegment routeSegment, string requestPathSegment, IDictionary<string, object> defaultValues, IDictionary<string, object> matchedValues)
        {
            if (String.IsNullOrEmpty(requestPathSegment))
            {
                // If there's no data to parse, we must have exactly one parameter segment and no other segments - otherwise no match

                if (routeSegment.Subsegments.Count > 1)
                {
                    return false;
                }

                PathParameterSubsegment parameterSubsegment = routeSegment.Subsegments[0] as PathParameterSubsegment;
                if (parameterSubsegment == null)
                {
                    return false;
                }

                // We must have a default value since there's no value in the request URI
                object parameterValue;
                if (defaultValues.TryGetValue(parameterSubsegment.ParameterName, out parameterValue))
                {
                    // If there's a default value for this parameter, use that default value
                    matchedValues.Add(parameterSubsegment.ParameterName, parameterValue);
                    return true;
                }
                else
                {
                    // If there's no default value, this segment doesn't match
                    return false;
                }
            }

            // Optimize for the common case where there is only one subsegment in the segment - either a parameter or a literal
            if (routeSegment.Subsegments.Count == 1)
            {
                return MatchSingleContentPathSegment(routeSegment.Subsegments[0], requestPathSegment, matchedValues);
            }

            // Find last literal segment and get its last index in the string

            int lastIndex = requestPathSegment.Length;
            int indexOfLastSegmentUsed = routeSegment.Subsegments.Count - 1;

            PathParameterSubsegment parameterNeedsValue = null; // Keeps track of a parameter segment that is pending a value
            PathLiteralSubsegment lastLiteral = null; // Keeps track of the left-most literal we've encountered

            while (indexOfLastSegmentUsed >= 0)
            {
                int newLastIndex = lastIndex;

                PathParameterSubsegment parameterSubsegment = routeSegment.Subsegments[indexOfLastSegmentUsed] as PathParameterSubsegment;
                if (parameterSubsegment != null)
                {
                    // Hold on to the parameter so that we can fill it in when we locate the next literal
                    parameterNeedsValue = parameterSubsegment;
                }
                else
                {
                    PathLiteralSubsegment literalSubsegment = routeSegment.Subsegments[indexOfLastSegmentUsed] as PathLiteralSubsegment;
                    if (literalSubsegment != null)
                    {
                        lastLiteral = literalSubsegment;

                        int startIndex = lastIndex - 1;
                        // If we have a pending parameter subsegment, we must leave at least one character for that
                        if (parameterNeedsValue != null)
                        {
                            startIndex--;
                        }

                        if (startIndex < 0)
                        {
                            return false;
                        }

                        int indexOfLiteral = requestPathSegment.LastIndexOf(literalSubsegment.Literal, startIndex, StringComparison.OrdinalIgnoreCase);
                        if (indexOfLiteral == -1)
                        {
                            // If we couldn't find this literal index, this segment cannot match
                            return false;
                        }

                        // If the first subsegment is a literal, it must match at the right-most extent of the request URI.
                        // Without this check if your route had "/Foo/" we'd match the request URI "/somethingFoo/".
                        // This check is related to the check we do at the very end of this function.
                        if (indexOfLastSegmentUsed == (routeSegment.Subsegments.Count - 1))
                        {
                            if ((indexOfLiteral + literalSubsegment.Literal.Length) != requestPathSegment.Length)
                            {
                                return false;
                            }
                        }

                        newLastIndex = indexOfLiteral;
                    }
                    else
                    {
                        Contract.Assert(false, "Invalid path segment type");
                    }
                }

                if ((parameterNeedsValue != null) && (((lastLiteral != null) && (parameterSubsegment == null)) || (indexOfLastSegmentUsed == 0)))
                {
                    // If we have a pending parameter that needs a value, grab that value

                    int parameterStartIndex;
                    int parameterTextLength;

                    if (lastLiteral == null)
                    {
                        if (indexOfLastSegmentUsed == 0)
                        {
                            parameterStartIndex = 0;
                        }
                        else
                        {
                            parameterStartIndex = newLastIndex;
                            Contract.Assert(false, "indexOfLastSegementUsed should always be 0 from the check above");
                        }
                        parameterTextLength = lastIndex;
                    }
                    else
                    {
                        // If we're getting a value for a parameter that is somewhere in the middle of the segment
                        if ((indexOfLastSegmentUsed == 0) && (parameterSubsegment != null))
                        {
                            parameterStartIndex = 0;
                            parameterTextLength = lastIndex;
                        }
                        else
                        {
                            parameterStartIndex = newLastIndex + lastLiteral.Literal.Length;
                            parameterTextLength = lastIndex - parameterStartIndex;
                        }
                    }

                    string parameterValueString = requestPathSegment.Substring(parameterStartIndex, parameterTextLength);

                    if (String.IsNullOrEmpty(parameterValueString))
                    {
                        // If we're here that means we have a segment that contains multiple sub-segments.
                        // For these segments all parameters must have non-empty values. If the parameter
                        // has an empty value it's not a match.
                        return false;
                    }
                    else
                    {
                        // If there's a value in the segment for this parameter, use the subsegment value
                        matchedValues.Add(parameterNeedsValue.ParameterName, parameterValueString);
                    }

                    parameterNeedsValue = null;
                    lastLiteral = null;
                }

                lastIndex = newLastIndex;
                indexOfLastSegmentUsed--;
            }

            // If the last subsegment is a parameter, it's OK that we didn't parse all the way to the left extent of
            // the string since the parameter will have consumed all the remaining text anyway. If the last subsegment
            // is a literal then we *must* have consumed the entire text in that literal. Otherwise we end up matching
            // the route "Foo" to the request URI "somethingFoo". Thus we have to check that we parsed the *entire*
            // request URI in order for it to be a match.
            // This check is related to the check we do earlier in this function for LiteralSubsegments.
            return (lastIndex == 0) || (routeSegment.Subsegments[0] is PathParameterSubsegment);
        }

        private static bool MatchSingleContentPathSegment(PathSubsegment pathSubsegment, string requestPathSegment, IDictionary<string, object> matchedValues)
        {
            PathParameterSubsegment parameterSubsegment = pathSubsegment as PathParameterSubsegment;
            if (parameterSubsegment == null)
            {
                // Handle a single literal segment
                PathLiteralSubsegment literalSubsegment = pathSubsegment as PathLiteralSubsegment;
                Contract.Assert(literalSubsegment != null, "Invalid path segment type");
                return literalSubsegment.Literal.Equals(requestPathSegment, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // Handle a single parameter segment
                matchedValues.Add(parameterSubsegment.ParameterName, requestPathSegment);
                return true;
            }
        }
    }
}
