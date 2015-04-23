// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    /// <summary>
    /// Methods for determining how an <see cref="ITagHelper"/> should run based on the attributes that were specified.
    /// </summary>
    public static class AttributeMatcher
    {
        /// <summary>
        /// Determines the modes a <see cref="ITagHelper" /> can run in based on which modes have all their required
        /// attributes present, non null, non empty, and non whitepsace.
        /// </summary>
        /// <typeparam name="TMode">The type representing the <see cref="ITagHelper" />'s modes.</typeparam>
        /// <param name="context">The <see cref="TagHelperContext"/>.</param>
        /// <param name="modeInfos">The modes and their required attributes.</param>
        /// <returns>The <see cref="ModeMatchResult{TMode}"/>.</returns>
        public static ModeMatchResult<TMode> DetermineMode<TMode>(
            [NotNull] TagHelperContext context,
            [NotNull] IEnumerable<ModeAttributes<TMode>> modeInfos)
        {
            // true == full match, false == partial match
            var matchedAttributes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var result = new ModeMatchResult<TMode>();

            foreach (var modeInfo in modeInfos)
            {
                var modeAttributes = GetPresentMissingAttributes(context, modeInfo.Attributes);

                if (modeAttributes.Present.Any())
                {
                    if (!modeAttributes.Missing.Any())
                    {
                        // A complete match, mark the attribute as fully matched
                        foreach (var attribute in modeAttributes.Present)
                        {
                            matchedAttributes[attribute] = true;
                        }

                        result.FullMatches.Add(ModeMatchAttributes.Create(modeInfo.Mode, modeInfo.Attributes));
                    }
                    else
                    {
                        // A partial match, mark the attribute as partially matched if not already fully matched
                        foreach (var attribute in modeAttributes.Present)
                        {
                            bool attributeMatch;
                            if (!matchedAttributes.TryGetValue(attribute, out attributeMatch))
                            {
                                matchedAttributes[attribute] = false;
                            }
                        }

                        result.PartialMatches.Add(ModeMatchAttributes.Create(
                            modeInfo.Mode, modeAttributes.Present, modeAttributes.Missing));
                    }
                }
            }

            // Build the list of partially matched attributes (those with partial matches but no full matches)
            foreach (var attribute in matchedAttributes.Keys)
            {
                if (!matchedAttributes[attribute])
                {
                    result.PartiallyMatchedAttributes.Add(attribute);
                }
            }

            return result;
        }

        private static PresentMissingAttributes GetPresentMissingAttributes(
            TagHelperContext context,
            IEnumerable<string> requiredAttributes)
        {
            // Check for all attribute values
            var presentAttributes = new List<string>();
            var missingAttributes = new List<string>();

            foreach (var attribute in requiredAttributes)
            {
                if (!context.AllAttributes.ContainsName(attribute) ||
                    context.AllAttributes[attribute] == null ||
                    (typeof(string).IsAssignableFrom(context.AllAttributes[attribute].Value.GetType()) &&
                    string.IsNullOrWhiteSpace(context.AllAttributes[attribute].Value as string)))
                {
                    // Missing attribute!
                    missingAttributes.Add(attribute);
                }
                else
                {
                    presentAttributes.Add(attribute);
                }
            }

            return new PresentMissingAttributes { Present = presentAttributes, Missing = missingAttributes };
        }

        private class PresentMissingAttributes
        {
            public IEnumerable<string> Present { get; set; }

            public IEnumerable<string> Missing { get; set; }
        }
    }
}