// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;

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
        /// <paramref name="attributeName"/>.</remarks>
        public static void CopyHtmlAttribute(
            [NotNull] this TagHelperOutput tagHelperOutput,
            [NotNull] string attributeName,
            [NotNull] TagHelperContext context)
        {
            if (!tagHelperOutput.Attributes.ContainsName(attributeName))
            {
                IEnumerable<IReadOnlyTagHelperAttribute> entries;

                // We look for the original attribute so we can restore the exact attribute name the user typed.
                // Approach also ignores changes made to tagHelperOutput[attributeName].
                if (!context.AllAttributes.TryGetAttributes(attributeName, out entries))
                {
                    throw new ArgumentException(
                        Resources.FormatTagHelperOutput_AttributeDoesNotExist(attributeName, nameof(TagHelperContext)),
                        nameof(attributeName));
                }

                foreach (var entry in entries)
                {
                    tagHelperOutput.Attributes.Add(entry.Name, entry.Value);
                }
            }
        }

        /// <summary>
        /// Merges the given <paramref name="tagBuilder"/>'s <see cref="TagBuilder.Attributes"/> into the
        /// <paramref name="tagHelperOutput"/>.
        /// </summary>
        /// <param name="tagHelperOutput">The <see cref="TagHelperOutput"/> this method extends.</param>
        /// <param name="tagBuilder">The <see cref="TagBuilder"/> to merge attributes from.</param>
        /// <remarks>Existing <see cref="TagHelperOutput.Attributes"/> on the given <paramref name="tagHelperOutput"/>
        /// are not overridden; "class" attributes are merged with spaces.</remarks>
        public static void MergeAttributes(
            [NotNull] this TagHelperOutput tagHelperOutput,
            [NotNull] TagBuilder tagBuilder)
        {
            foreach (var attribute in tagBuilder.Attributes)
            {
                if (!tagHelperOutput.Attributes.ContainsName(attribute.Key))
                {
                    tagHelperOutput.Attributes.Add(attribute.Key, attribute.Value);
                }
                else if (attribute.Key.Equals("class", StringComparison.OrdinalIgnoreCase))
                {
                    TagHelperAttribute classAttribute;

                    if (tagHelperOutput.Attributes.TryGetAttribute("class", out classAttribute))
                    {
                        tagHelperOutput.Attributes["class"] = classAttribute.Value + " " + attribute.Value;
                    }
                    else
                    {
                        tagHelperOutput.Attributes.Add("class", attribute.Value);
                    }
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
            [NotNull] this TagHelperOutput tagHelperOutput,
            [NotNull] IEnumerable<TagHelperAttribute> attributes)
        {
            foreach (var attribute in attributes.ToArray())
            {
                tagHelperOutput.Attributes.Remove(attribute);
            }
        }
    }
}