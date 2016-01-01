// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Internal;

namespace Microsoft.AspNet.Routing.Template
{
    public class TemplateMatcher
    {
        private const string SeparatorString = "/";
        private const char SeparatorChar = '/';

        // Perf: This is a cache to avoid looking things up in 'Defaults' each request.
        private readonly bool[] _hasDefaultValue;
        private readonly object[] _defaultValues;

        private static readonly char[] Delimiters = new char[] { SeparatorChar };

        public TemplateMatcher(
            RouteTemplate template,
            RouteValueDictionary defaults)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            Template = template;
            Defaults = defaults ?? new RouteValueDictionary();

            // Perf: cache the default value for each parameter (other than complex segments).
            _hasDefaultValue = new bool[Template.Segments.Count];
            _defaultValues = new object[Template.Segments.Count];

            for (var i = 0; i < Template.Segments.Count; i++)
            {
                var segment = Template.Segments[i];
                if (!segment.IsSimple)
                {
                    continue;
                }

                var part = segment.Parts[0];
                if (!part.IsParameter)
                {
                    continue;
                }

                object value;
                if (Defaults.TryGetValue(part.Name, out value))
                {
                    _hasDefaultValue[i] = true;
                    _defaultValues[i] = value;
                }
            }
        }

        public RouteValueDictionary Defaults { get; }

        public RouteTemplate Template { get; }

        public RouteValueDictionary Match(PathString path)
        {
            var i = 0;
            var pathTokenizer = new PathTokenizer(path);

            // Perf: We do a traversal of the request-segments + route-segments twice.
            //
            // For most segment-types, we only really need to any work on one of the two passes.
            //
            // On the first pass, we're just looking to see if there's anything that would disqualify us from matching.
            // The most common case would be a literal segment that doesn't match.
            //
            // On the second pass, we're almost certainly going to match the URL, so go ahead and allocate the 'values'
            // and start capturing strings. 
            foreach (var requestSegment in pathTokenizer)
            {
                var routeSegment = Template.GetSegment(i++);
                if (routeSegment == null && requestSegment.Length > 0)
                {
                    // If pathSegment is null, then we're out of route segments. All we can match is the empty
                    // string.
                    return null;
                }
                else if (routeSegment.IsSimple && routeSegment.Parts[0].IsLiteral)
                {
                    // This is a literal segment, so we need to match the text, or the route isn't a match.
                    var part = routeSegment.Parts[0];
                    if (!requestSegment.Equals(part.Text, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }
                else if (routeSegment.IsSimple && routeSegment.Parts[0].IsCatchAll)
                {
                    // Nothing to validate for a catch-all - it can match any string, including the empty string.
                    //
                    // Also, a catch-all has to be the last part, so we're done.
                    break;
                }
                else if (routeSegment.IsSimple && routeSegment.Parts[0].IsParameter)
                {
                    // For a parameter, validate that it's a has some length, or we have a default, or it's optional.
                    var part = routeSegment.Parts[0];
                    if (requestSegment.Length == 0 &&
                        !_hasDefaultValue[i] &&
                        !part.IsOptional)
                    {
                        // There's no value for this parameter, the route can't match.
                        return null;
                    }
                }
                else
                {
                    Debug.Assert(!routeSegment.IsSimple);

                    // Don't attempt to validate a complex segment at this point other than being non-emtpy,
                    // do it in the second pass.
                }
            }

            for (; i < Template.Segments.Count; i++)
            {
                // We've matched the request path so far, but still have remaining route segments. These need
                // to be all single-part parameter segments with default values or else they won't match.
                var routeSegment = Template.GetSegment(i);
                Debug.Assert(routeSegment != null);

                if (!routeSegment.IsSimple)
                {
                    // If the segment is a complex segment, it MUST contain literals, and we've parsed the full
                    // path so far, so it can't match.
                    return null;
                }

                var part = routeSegment.Parts[0];
                if (part.IsLiteral)
                {
                    // If the segment is a simple literal - which need the URL to provide a value, so we don't match.
                    return null;
                }

                if (part.IsCatchAll)
                {
                    // Nothing to validate for a catch-all - it can match any string, including the empty string.
                    //
                    // Also, a catch-all has to be the last part, so we're done.
                    break;
                }

                // If we get here, this is a simple segment with a parameter. We need it to be optional, or for the
                // defaults to have a value.
                Debug.Assert(routeSegment.IsSimple && part.IsParameter);
                if (!_hasDefaultValue[i] && !part.IsOptional)
                {
                    // There's no default for this (non-optional) parameter so it can't match.
                    return null;
                }
            }

            // At this point we've very likely got a match, so start capturing values for real.
            var values = new RouteValueDictionary();

            i = 0;
            foreach (var requestSegment in pathTokenizer)
            {
                var routeSegment = Template.GetSegment(i++);

                if (routeSegment.IsSimple && routeSegment.Parts[0].IsCatchAll)
                {
                    // A catch-all captures til the end of the string.
                    var part = routeSegment.Parts[0];
                    var captured = requestSegment.Buffer.Substring(requestSegment.Offset);
                    if (captured.Length > 0)
                    {
                        values.Add(part.Name, captured);
                    }
                    else
                    {
                        // It's ok for a catch-all to produce a null value, so we don't check _hasDefaultValue.
                        values.Add(part.Name, _defaultValues[i]);
                    }

                    // A catch-all has to be the last part, so we're done.
                    break;
                }
                else if (routeSegment.IsSimple && routeSegment.Parts[0].IsParameter)
                {
                    // A simple parameter captures the whole segment, or a default value if nothing was
                    // provided.
                    var part = routeSegment.Parts[0];
                    if (requestSegment.Length > 0)
                    {
                        values.Add(part.Name, requestSegment.ToString());
                    }
                    else
                    {
                        if (_hasDefaultValue[i])
                        {
                            values.Add(part.Name, _defaultValues[i]);
                        }
                    }
                }
                else if (!routeSegment.IsSimple)
                {
                    if (!MatchComplexSegment(routeSegment, requestSegment.ToString(), Defaults, values))
                    {
                        return null;
                    }
                }
            }

            for (; i < Template.Segments.Count; i++)
            {
                // We've matched the request path so far, but still have remaining route segments. We already know these
                // are simple parameters that either have a default, or don't need to produce a value.
                var routeSegment = Template.GetSegment(i);
                Debug.Assert(routeSegment != null);
                Debug.Assert(routeSegment.IsSimple);

                var part = routeSegment.Parts[0];
                Debug.Assert(part.IsParameter);
   
                // It's ok for a catch-all to produce a null value
                if (_hasDefaultValue[i] || part.IsCatchAll)
                {
                    values.Add(part.Name, _defaultValues[i]);
                }
            }

            // Copy all remaining default values to the route data
            foreach (var kvp in Defaults)
            {
                if (!values.ContainsKey(kvp.Key))
                {
                    values.Add(kvp.Key, kvp.Value);
                }
            }

            return values;
        }

        private bool MatchComplexSegment(
            TemplateSegment routeSegment,
            string requestSegment,
            IReadOnlyDictionary<string, object> defaults,
            RouteValueDictionary values)
        {
            var indexOfLastSegment = routeSegment.Parts.Count - 1;

            // We match the request to the template starting at the rightmost parameter
            // If the last segment of template is optional, then request can match the 
            // template with or without the last parameter. So we start with regular matching,
            // but if it doesn't match, we start with next to last parameter. Example:
            // Template: {p1}/{p2}.{p3?}. If the request is foo/bar.moo it will match right away
            // giving p3 value of moo. But if the request is foo/bar, we start matching from the
            // rightmost giving p3 the value of bar, then we end up not matching the segment.
            // In this case we start again from p2 to match the request and we succeed giving
            // the value bar to p2
            if (routeSegment.Parts[indexOfLastSegment].IsOptional &&
                routeSegment.Parts[indexOfLastSegment - 1].IsOptionalSeperator)
            {
                if (MatchComplexSegmentCore(routeSegment, requestSegment, Defaults, values, indexOfLastSegment))
                {
                    return true;
                }
                else
                {
                    if (requestSegment.EndsWith(
                        routeSegment.Parts[indexOfLastSegment - 1].Text,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    return MatchComplexSegmentCore(
                        routeSegment,
                        requestSegment,
                        Defaults,
                        values,
                        indexOfLastSegment - 2);
                }
            }
            else
            {
                return MatchComplexSegmentCore(routeSegment, requestSegment, Defaults, values, indexOfLastSegment);
            }
        }

        private bool MatchComplexSegmentCore(
            TemplateSegment routeSegment,
            string requestSegment,
            IReadOnlyDictionary<string, object> defaults,
            RouteValueDictionary values,
            int indexOfLastSegmentUsed)
        {
            Debug.Assert(routeSegment != null);
            Debug.Assert(routeSegment.Parts.Count > 1);

            // Find last literal segment and get its last index in the string
            var lastIndex = requestSegment.Length;

            TemplatePart parameterNeedsValue = null; // Keeps track of a parameter segment that is pending a value
            TemplatePart lastLiteral = null; // Keeps track of the left-most literal we've encountered

            var outValues = new RouteValueDictionary();

            while (indexOfLastSegmentUsed >= 0)
            {
                var newLastIndex = lastIndex;

                var part = routeSegment.Parts[indexOfLastSegmentUsed];
                if (part.IsParameter)
                {
                    // Hold on to the parameter so that we can fill it in when we locate the next literal
                    parameterNeedsValue = part;
                }
                else
                {
                    Debug.Assert(part.IsLiteral);
                    lastLiteral = part;

                    var startIndex = lastIndex - 1;
                    // If we have a pending parameter subsegment, we must leave at least one character for that
                    if (parameterNeedsValue != null)
                    {
                        startIndex--;
                    }

                    if (startIndex < 0)
                    {
                        return false;
                    }

                    var indexOfLiteral = requestSegment.LastIndexOf(
                        part.Text,
                        startIndex,
                        StringComparison.OrdinalIgnoreCase);
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

                if ((parameterNeedsValue != null) &&
                    (((lastLiteral != null) && (part.IsLiteral)) || (indexOfLastSegmentUsed == 0)))
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
                            Debug.Assert(false, "indexOfLastSegementUsed should always be 0 from the check above");
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

                    var parameterValueString = requestSegment.Substring(parameterStartIndex, parameterTextLength);

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
                        outValues.Add(parameterNeedsValue.Name, parameterValueString);
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
            if (lastIndex == 0 || routeSegment.Parts[0].IsParameter)
            {
                foreach (var item in outValues)
                {
                    values.Add(item.Key, item.Value);
                }

                return true;
            }

            return false;
        }
    }
}
