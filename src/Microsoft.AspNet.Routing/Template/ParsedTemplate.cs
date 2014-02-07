// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Microsoft.AspNet.Routing.Template
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public class ParsedTemplate
    {
        private const string SeparatorString = "/";
        private const char SeparatorChar = '/';

        private static readonly char[] Delimiters = new char[] { SeparatorChar };

        public ParsedTemplate(List<TemplateSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException("segments");
            }

            Segments = segments;
        }

        public List<TemplateSegment> Segments { get; private set; }

        public IDictionary<string, object> Match(string requestPath, IDictionary<string, object> defaults)
        {
            var requestSegments = requestPath.Split(Delimiters);

            if (defaults == null)
            {
                defaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < requestSegments.Length; i++)
            {
                var routeSegment = Segments.Count > i ? Segments[i] : null;
                var requestSegment = requestSegments[i];

                if (routeSegment == null)
                {
                    // If pathSegment is null, then we're out of route segments. All we can match is the empty
                    // string.
                    if (requestSegment.Length > 0)
                    {
                        return null;
                    }
                }
                else if (routeSegment.Parts.Count == 1)
                {
                    // Optimize for the simple case - the segment is made up for a single part
                    var part = routeSegment.Parts[0];
                    if (part.IsLiteral)
                    {
                        if (!part.Text.Equals(requestSegment, StringComparison.OrdinalIgnoreCase))
                        {
                            return null;
                        }
                    }
                    else 
                    {
                        Contract.Assert(part.IsParameter);

                        if (part.IsCatchAll)
                        {
                            var captured = string.Join(SeparatorString, requestSegments, i, requestSegments.Length - i);
                            if (captured.Length > 0)
                            {
                                values.Add(part.Name, captured);
                            }
                            else
                            {
                                // It's ok for a catch-all to produce a null value
                                object defaultValue;
                                defaults.TryGetValue(part.Name, out defaultValue);

                                values.Add(part.Name, defaultValue);
                            }

                            // A catch-all has to be the last part, so we're done.
                            break;
                        }
                        else
                        {
                            if (requestSegment.Length > 0)
                            {
                                values.Add(part.Name, requestSegment);
                            }
                            else
                            {
                                object defaultValue;
                                if (defaults.TryGetValue(part.Name, out defaultValue))
                                {
                                    values.Add(part.Name, defaultValue);
                                }
                                else
                                {
                                    // There's no default for this parameter
                                    return null;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (!MatchComplexSegment(routeSegment, requestSegment, defaults, values))
                    {
                        return null;
                    }
                }
            }

            for (int i = requestSegments.Length; i < Segments.Count; i++)
            {
                // We've matched the request path so far, but still have remaining route segments. These need
                // to be all single-part parameter segments with default values or else they won't match.
                var routeSegment = Segments[i];
                if (routeSegment.Parts.Count > 1)
                {
                    // If it has more than one part it must contain literals, so it can't match.
                    return null;
                }


                var part = routeSegment.Parts[0];
                if (part.IsLiteral)
                {
                    return null;
                }

                Contract.Assert(part.IsParameter);

                // It's ok for a catch-all to produce a null value
                object defaultValue;
                if (defaults.TryGetValue(part.Name, out defaultValue) || part.IsCatchAll)
                {
                    values.Add(part.Name, defaultValue);
                }
                else
                {
                    // There's no default for this (non-catch-all) parameter so it can't match.
                    return null;
                }
            }

            // Copy all remaining default values to the route data
            if (defaults != null)
            {
                foreach (var kvp in defaults)
                {
                    if (!values.ContainsKey(kvp.Key))
                    {
                        values.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return values;
        }

        private bool MatchComplexSegment(TemplateSegment routeSegment, string requestSegment, IDictionary<string, object> defaults, Dictionary<string, object> values)
        {
            Contract.Assert(routeSegment != null);
            Contract.Assert(routeSegment.Parts.Count > 1);

            // Find last literal segment and get its last index in the string
            int lastIndex = requestSegment.Length;
            int indexOfLastSegmentUsed = routeSegment.Parts.Count - 1;

            TemplatePart parameterNeedsValue = null; // Keeps track of a parameter segment that is pending a value
            TemplatePart lastLiteral = null; // Keeps track of the left-most literal we've encountered

            while (indexOfLastSegmentUsed >= 0)
            {
                int newLastIndex = lastIndex;

                var part = routeSegment.Parts[indexOfLastSegmentUsed];
                if (part.IsParameter)
                {
                    // Hold on to the parameter so that we can fill it in when we locate the next literal
                    parameterNeedsValue = part;
                }
                else
                {
                    Contract.Assert(part.IsLiteral);
                    lastLiteral = part;

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

                    int indexOfLiteral = requestSegment.LastIndexOf(part.Text, startIndex, StringComparison.OrdinalIgnoreCase);
                    if (indexOfLiteral == -1)
                    {
                        // If we couldn't find this literal index, this segment cannot match
                        return false;
                    }

                    // If the first subsegment is a literal, it must match at the right-most extent of the request URI.
                    // Without this check if your route had "/Foo/" we'd match the request URI "/somethingFoo/".
                    // This check is related to the check we do at the very end of this function.
                    if (indexOfLastSegmentUsed == (routeSegment.Parts.Count - 1))
                    {
                        if ((indexOfLiteral + part.Text.Length) != requestSegment.Length)
                        {
                            return false;
                        }
                    }

                    newLastIndex = indexOfLiteral;
                }

                if ((parameterNeedsValue != null) && (((lastLiteral != null) && (part.IsLiteral)) || (indexOfLastSegmentUsed == 0)))
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
                        if ((indexOfLastSegmentUsed == 0) && (part.IsParameter))
                        {
                            parameterStartIndex = 0;
                            parameterTextLength = lastIndex;
                        }
                        else
                        {
                            parameterStartIndex = newLastIndex + lastLiteral.Text.Length;
                            parameterTextLength = lastIndex - parameterStartIndex;
                        }
                    }

                    string parameterValueString = requestSegment.Substring(parameterStartIndex, parameterTextLength);

                    if (string.IsNullOrEmpty(parameterValueString))
                    {
                        // If we're here that means we have a segment that contains multiple sub-segments.
                        // For these segments all parameters must have non-empty values. If the parameter
                        // has an empty value it's not a match.
                        return false;
                    }
                    else
                    {
                        // If there's a value in the segment for this parameter, use the subsegment value
                        values.Add(parameterNeedsValue.Name, parameterValueString);
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
            return (lastIndex == 0) || routeSegment.Parts[0].IsParameter;
        }

        private string DebuggerToString()
        {
            return string.Join(SeparatorString, Segments.Select(s => s.DebuggerToString()));
        }
    }
}
