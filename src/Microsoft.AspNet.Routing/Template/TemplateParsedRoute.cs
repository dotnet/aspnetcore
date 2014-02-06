// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.AspNet.Routing.Template
{
    public sealed class TemplateParsedRoute
    {
        public TemplateParsedRoute(IList<PathSegment> pathSegments)
        {
            Contract.Assert(pathSegments != null);
            PathSegments = pathSegments;
        }

        internal IList<PathSegment> PathSegments { get; private set; }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Not changing original algorithm")]
        [SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode", Justification = "Not changing original algorithm")]
        public BoundRouteTemplate Bind(IDictionary<string, object> currentValues, IDictionary<string, object> values, IDictionary<string, object> defaultValues, IDictionary<string, object> constraints)
        {
            if (currentValues == null)
            {
                currentValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            if (values == null)
            {
                values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            if (defaultValues == null)
            {
                defaultValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            // The set of values we should be using when generating the URI in this route
            IDictionary<string, object> acceptedValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Keep track of which new values have been used
            HashSet<string> unusedNewValues = new HashSet<string>(values.Keys, StringComparer.OrdinalIgnoreCase);

            // Step 1: Get the list of values we're going to try to use to match and generate this URI

            // Find out which entries in the URI are valid for the URI we want to generate.
            // If the URI had ordered parameters a="1", b="2", c="3" and the new values
            // specified that b="9", then we need to invalidate everything after it. The new
            // values should then be a="1", b="9", c=<no value>.
            ForEachParameter(PathSegments, delegate(PathParameterSubsegment parameterSubsegment)
            {
                // If it's a parameter subsegment, examine the current value to see if it matches the new value
                string parameterName = parameterSubsegment.ParameterName;

                object newParameterValue;
                bool hasNewParameterValue = values.TryGetValue(parameterName, out newParameterValue);
                if (hasNewParameterValue)
                {
                    unusedNewValues.Remove(parameterName);
                }

                object currentParameterValue;
                bool hasCurrentParameterValue = currentValues.TryGetValue(parameterName, out currentParameterValue);

                if (hasNewParameterValue && hasCurrentParameterValue)
                {
                    if (!RoutePartsEqual(currentParameterValue, newParameterValue))
                    {
                        // Stop copying current values when we find one that doesn't match
                        return false;
                    }
                }

                // If the parameter is a match, add it to the list of values we will use for URI generation
                if (hasNewParameterValue)
                {
                    if (IsRoutePartNonEmpty(newParameterValue))
                    {
                        acceptedValues.Add(parameterName, newParameterValue);
                    }
                }
                else
                {
                    if (hasCurrentParameterValue)
                    {
                        acceptedValues.Add(parameterName, currentParameterValue);
                    }
                }
                return true;
            });

            // Add all remaining new values to the list of values we will use for URI generation
            foreach (var newValue in values)
            {
                if (IsRoutePartNonEmpty(newValue.Value))
                {
                    if (!acceptedValues.ContainsKey(newValue.Key))
                    {
                        acceptedValues.Add(newValue.Key, newValue.Value);
                    }
                }
            }

            // Add all current values that aren't in the URI at all
            foreach (var currentValue in currentValues)
            {
                string parameterName = currentValue.Key;
                if (!acceptedValues.ContainsKey(parameterName))
                {
                    PathParameterSubsegment parameterSubsegment = GetParameterSubsegment(PathSegments, parameterName);
                    if (parameterSubsegment == null)
                    {
                        acceptedValues.Add(parameterName, currentValue.Value);
                    }
                }
            }

            // Add all remaining default values from the route to the list of values we will use for URI generation
            ForEachParameter(PathSegments, delegate(PathParameterSubsegment parameterSubsegment)
            {
                if (!acceptedValues.ContainsKey(parameterSubsegment.ParameterName))
                {
                    object defaultValue;
                    if (!IsParameterRequired(parameterSubsegment, defaultValues, out defaultValue))
                    {
                        // Add the default value only if there isn't already a new value for it and
                        // only if it actually has a default value, which we determine based on whether
                        // the parameter value is required.
                        acceptedValues.Add(parameterSubsegment.ParameterName, defaultValue);
                    }
                }
                return true;
            });

            // All required parameters in this URI must have values from somewhere (i.e. the accepted values)
            bool hasAllRequiredValues = ForEachParameter(PathSegments, delegate(PathParameterSubsegment parameterSubsegment)
            {
                object defaultValue;
                if (IsParameterRequired(parameterSubsegment, defaultValues, out defaultValue))
                {
                    if (!acceptedValues.ContainsKey(parameterSubsegment.ParameterName))
                    {
                        // If the route parameter value is required that means there's
                        // no default value, so if there wasn't a new value for it
                        // either, this route won't match.
                        return false;
                    }
                }
                return true;
            });
            if (!hasAllRequiredValues)
            {
                return null;
            }

            // All other default values must match if they are explicitly defined in the new values
            IDictionary<string, object> otherDefaultValues = new Dictionary<string, object>(defaultValues, StringComparer.OrdinalIgnoreCase);
            ForEachParameter(PathSegments, delegate(PathParameterSubsegment parameterSubsegment)
            {
                otherDefaultValues.Remove(parameterSubsegment.ParameterName);
                return true;
            });

            foreach (var defaultValue in otherDefaultValues)
            {
                object value;
                if (values.TryGetValue(defaultValue.Key, out value))
                {
                    unusedNewValues.Remove(defaultValue.Key);
                    if (!RoutePartsEqual(value, defaultValue.Value))
                    {
                        // If there is a non-parameterized value in the route and there is a
                        // new value for it and it doesn't match, this route won't match.
                        return null;
                    }
                }
            }

            // Step 2: If the route is a match generate the appropriate URI

            StringBuilder uri = new StringBuilder();
            StringBuilder pendingParts = new StringBuilder();

            bool pendingPartsAreAllSafe = false;
            bool blockAllUriAppends = false;

            for (int i = 0; i < PathSegments.Count; i++)
            {
                PathSegment pathSegment = PathSegments[i]; // parsedRouteUriPart

                if (pathSegment is PathSeparatorSegment)
                {
                    if (pendingPartsAreAllSafe)
                    {
                        // Accept
                        if (pendingParts.Length > 0)
                        {
                            if (blockAllUriAppends)
                            {
                                return null;
                            }

                            // Append any pending literals to the URI
                            uri.Append(pendingParts.ToString());
                            pendingParts.Length = 0;
                        }
                    }
                    pendingPartsAreAllSafe = false;

                    // Guard against appending multiple separators for empty segments
                    if (pendingParts.Length > 0 && pendingParts[pendingParts.Length - 1] == '/')
                    {
                        // Dev10 676725: Route should not be matched if that causes mismatched tokens
                        // Dev11 86819: We will allow empty matches if all subsequent segments are null
                        if (blockAllUriAppends)
                        {
                            return null;
                        }

                        // Append any pending literals to the URI (without the trailing slash) and prevent any future appends
                        uri.Append(pendingParts.ToString(0, pendingParts.Length - 1));
                        pendingParts.Length = 0;
                        blockAllUriAppends = true;
                    }
                    else
                    {
                        pendingParts.Append("/");
                    }
                }
                else
                {
                    PathContentSegment contentPathSegment = pathSegment as PathContentSegment;
                    if (contentPathSegment != null)
                    {
                        // Segments are treated as all-or-none. We should never output a partial segment.
                        // If we add any subsegment of this segment to the generated URI, we have to add
                        // the complete match. For example, if the subsegment is "{p1}-{p2}.xml" and we
                        // used a value for {p1}, we have to output the entire segment up to the next "/".
                        // Otherwise we could end up with the partial segment "v1" instead of the entire
                        // segment "v1-v2.xml".
                        bool addedAnySubsegments = false;

                        foreach (PathSubsegment subsegment in contentPathSegment.Subsegments)
                        {
                            PathLiteralSubsegment literalSubsegment = subsegment as PathLiteralSubsegment;
                            if (literalSubsegment != null)
                            {
                                // If it's a literal we hold on to it until we are sure we need to add it
                                pendingPartsAreAllSafe = true;
                                pendingParts.Append(literalSubsegment.Literal);
                            }
                            else
                            {
                                PathParameterSubsegment parameterSubsegment = subsegment as PathParameterSubsegment;
                                if (parameterSubsegment != null)
                                {
                                    if (pendingPartsAreAllSafe)
                                    {
                                        // Accept
                                        if (pendingParts.Length > 0)
                                        {
                                            if (blockAllUriAppends)
                                            {
                                                return null;
                                            }

                                            // Append any pending literals to the URI
                                            uri.Append(pendingParts.ToString());
                                            pendingParts.Length = 0;

                                            addedAnySubsegments = true;
                                        }
                                    }
                                    pendingPartsAreAllSafe = false;

                                    // If it's a parameter, get its value
                                    object acceptedParameterValue;
                                    bool hasAcceptedParameterValue = acceptedValues.TryGetValue(parameterSubsegment.ParameterName, out acceptedParameterValue);
                                    if (hasAcceptedParameterValue)
                                    {
                                        unusedNewValues.Remove(parameterSubsegment.ParameterName);
                                    }

                                    object defaultParameterValue;
                                    defaultValues.TryGetValue(parameterSubsegment.ParameterName, out defaultParameterValue);

                                    if (RoutePartsEqual(acceptedParameterValue, defaultParameterValue))
                                    {
                                        // If the accepted value is the same as the default value, mark it as pending since
                                        // we won't necessarily add it to the URI we generate.
                                        pendingParts.Append(Convert.ToString(acceptedParameterValue, CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        if (blockAllUriAppends)
                                        {
                                            return null;
                                        }

                                        // Add the new part to the URI as well as any pending parts
                                        if (pendingParts.Length > 0)
                                        {
                                            // Append any pending literals to the URI
                                            uri.Append(pendingParts.ToString());
                                            pendingParts.Length = 0;
                                        }
                                        uri.Append(Convert.ToString(acceptedParameterValue, CultureInfo.InvariantCulture));

                                        addedAnySubsegments = true;
                                    }
                                }
                                else
                                {
                                    Contract.Assert(false, "Invalid path subsegment type");
                                }
                            }
                        }

                        if (addedAnySubsegments)
                        {
                            // See comment above about why we add the pending parts
                            if (pendingParts.Length > 0)
                            {
                                if (blockAllUriAppends)
                                {
                                    return null;
                                }

                                // Append any pending literals to the URI
                                uri.Append(pendingParts.ToString());
                                pendingParts.Length = 0;
                            }
                        }
                    }
                    else
                    {
                        Contract.Assert(false, "Invalid path segment type");
                    }
                }
            }

            if (pendingPartsAreAllSafe)
            {
                // Accept
                if (pendingParts.Length > 0)
                {
                    if (blockAllUriAppends)
                    {
                        return null;
                    }

                    // Append any pending literals to the URI
                    uri.Append(pendingParts.ToString());
                }
            }

            // Process constraints keys
            if (constraints != null)
            {
                // If there are any constraints, mark all the keys as being used so that we don't
                // generate query string items for custom constraints that don't appear as parameters
                // in the URI format.
                foreach (var constraintsItem in constraints)
                {
                    unusedNewValues.Remove(constraintsItem.Key);
                }
            }

            // Encode the URI before we append the query string, otherwise we would double encode the query string
            StringBuilder encodedUri = new StringBuilder();
            encodedUri.Append(UriEncode(uri.ToString()));
            uri = encodedUri;

            // Add remaining new values as query string parameters to the URI
            if (unusedNewValues.Count > 0)
            {
                // Generate the query string
                bool firstParam = true;
                foreach (string unusedNewValue in unusedNewValues)
                {
                    object value;
                    if (acceptedValues.TryGetValue(unusedNewValue, out value))
                    {
                        uri.Append(firstParam ? '?' : '&');
                        firstParam = false;
                        uri.Append(Uri.EscapeDataString(unusedNewValue));
                        uri.Append('=');
                        uri.Append(Uri.EscapeDataString(Convert.ToString(value, CultureInfo.InvariantCulture)));
                    }
                }
            }

            return new BoundRouteTemplate
            {
                BoundTemplate = uri.ToString(),
                Values = acceptedValues
            };
        }

        private static string EscapeReservedCharacters(Match m)
        {
            return "%" + Convert.ToUInt16(m.Value[0]).ToString("x2", CultureInfo.InvariantCulture);
        }

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

        private static PathParameterSubsegment GetParameterSubsegment(IList<PathSegment> pathSegments, string parameterName)
        {
            PathParameterSubsegment foundParameterSubsegment = null;

            ForEachParameter(pathSegments, delegate(PathParameterSubsegment parameterSubsegment)
            {
                if (String.Equals(parameterName, parameterSubsegment.ParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    foundParameterSubsegment = parameterSubsegment;
                    return false;
                }
                else
                {
                    return true;
                }
            });

            return foundParameterSubsegment;
        }

        private static bool IsParameterRequired(PathParameterSubsegment parameterSubsegment, IDictionary<string, object> defaultValues, out object defaultValue)
        {
            if (parameterSubsegment.IsCatchAll)
            {
                defaultValue = null;
                return false;
            }

            return !defaultValues.TryGetValue(parameterSubsegment.ParameterName, out defaultValue);
        }

        private static bool IsRoutePartNonEmpty(object routePart)
        {
            string routePartString = routePart as string;
            if (routePartString != null)
            {
                return routePartString.Length > 0;
            }
            return routePart != null;
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

        private static bool RoutePartsEqual(object a, object b)
        {
            string sa = a as string;
            string sb = b as string;
            if (sa != null && sb != null)
            {
                // For strings do a case-insensitive comparison
                return String.Equals(sa, sb, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                if (a != null && b != null)
                {
                    // Explicitly call .Equals() in case it is overridden in the type
                    return a.Equals(b);
                }
                else
                {
                    // At least one of them is null. Return true if they both are
                    return a == b;
                }
            }
        }

        private static string UriEncode(string str)
        {
            string escape = Uri.EscapeUriString(str);
            return Regex.Replace(escape, "([#?])", new MatchEvaluator(EscapeReservedCharacters));
        }
    }
}
