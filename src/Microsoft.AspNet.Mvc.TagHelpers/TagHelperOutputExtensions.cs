// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    /// <summary>
    /// Utility related extensions for <see cref="TagHelperOutput"/>.
    /// </summary>
    public static class TagHelperOutputExtensions
    {
        /// <summary>
        /// Copies a user-provided attribute from <paramref name="context"/>'s 
        /// <see cref="TagHelperContext.AllAttributes"/> to <paramref name="tagHelperOutput"/>'s
        /// <see cref="TagHelperOutput.Attributes"/>.
        /// </summary>
        /// <param name="tagHelperOutput">The <see cref="TagHelperOutput"/> this method extends.</param>
        /// <param name="attributeName">The name of the bound attribute.</param>
        /// <param name="context">The <see cref="TagHelperContext"/>.</param>
        /// <remarks>Only copies the attribute if <paramref name="tagHelperOutput"/>'s 
        /// <see cref="TagHelperOutput.Attributes"/> does not contain an attribute with the given 
        /// <paramref name="attributeName"/></remarks>
        public static void CopyHtmlAttribute(this TagHelperOutput tagHelperOutput,
                                             string attributeName,
                                             TagHelperContext context)
        {
            // We look for the original attribute so we can restore the exact attribute name the user typed.
            var entry = context.AllAttributes.First(attribute =>
                attribute.Key.Equals(attributeName, StringComparison.OrdinalIgnoreCase));

            if (!tagHelperOutput.Attributes.ContainsKey(entry.Key))
            {
                tagHelperOutput.Attributes.Add(entry.Key, entry.Value.ToString());
            }
        }

        /// <summary>
        /// Returns all attributes from <paramref name="tagHelperOutput"/>'s 
        /// <see cref="TagHelperOutput.Attributes"/> that have the given <paramref name="prefix"/>.
        /// </summary>
        /// <param name="tagHelperOutput">The <see cref="TagHelperOutput"/> this method extends.</param>
        /// <param name="prefix">A prefix to look for.</param>
        /// <returns><see cref="KeyValuePair{string, string}"/>s with <see cref="KeyValuePair{string, string}.Key"/>
        /// starting with the given <paramref name="prefix"/>.</returns>
        public static IEnumerable<KeyValuePair<string, string>> FindPrefixedAttributes(
            this TagHelperOutput tagHelperOutput, string prefix)
        {
            // TODO: We will not need this method once https://github.com/aspnet/Razor/issues/89 is completed.

            // We're only interested in HTML attributes that have the desired prefix.
            var prefixedAttributes = tagHelperOutput.Attributes
                .Where(attribute => attribute.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return prefixedAttributes;
        }

        /// <summary>
        /// Merges the given <paramref name="tagBuilder"/>'s <see cref="TagBuilder.Attributes"/> into the 
        /// <paramref name="tagHelperOutput"/>.
        /// </summary>
        /// <param name="tagHelperOutput">The <see cref="TagHelperOutput"/> this method extends.</param>
        /// <param name="tagBuilder">The <see cref="TagBuilder"/> to merge attributes from.</param>
        /// <remarks>Existing <see cref="TagHelperOutput.Attributes"/> on the given <paramref name="tagHelperOutput"/>
        /// are not overridden; "class" attributes are merged with spaces.</remarks>
        public static void MergeAttributes(this TagHelperOutput tagHelperOutput, TagBuilder tagBuilder)
        {
            foreach (var attribute in tagBuilder.Attributes)
            {
                if (!tagHelperOutput.Attributes.ContainsKey(attribute.Key))
                {
                    tagHelperOutput.Attributes.Add(attribute.Key, attribute.Value);
                }
                else if (attribute.Key.Equals("class", StringComparison.OrdinalIgnoreCase))
                {
                    tagHelperOutput.Attributes["class"] += " " + attribute.Value;
                }
            }
        }

        /// <summary>
        /// Removes the given <paramref name="attributes"/> from <paramref name="tagHelperOutput"/>'s
        /// <see cref="TagHelperOutput.Attributes"/>.
        /// </summary>
        /// <param name="tagHelperOutput">The <see cref="TagHelperOutput"/> this method extends.</param>
        /// <param name="attributes">Attributes to remove.</param>
        public static void RemoveRange(
            this TagHelperOutput tagHelperOutput, IEnumerable<KeyValuePair<string, string>> attributes)
        {
            foreach (var attribute in attributes)
            {
                tagHelperOutput.Attributes.Remove(attribute);
            }
        }
    }
}