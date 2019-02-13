// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// Utility related extensions for <see cref="TagHelperOutput"/>.
    /// </summary>
    public static class TagHelperOutputExtensions
    {
        private static readonly char[] SpaceChars = { '\u0020', '\u0009', '\u000A', '\u000C', '\u000D' };

        /// <summary>
        /// Copies a user-provided attribute from <paramref name="context"/>'s
        /// <see cref="TagHelperContext.AllAttributes"/> to <paramref name="tagHelperOutput"/>'s
        /// <see cref="TagHelperOutput.Attributes"/>.
        /// </summary>
        /// <param name="tagHelperOutput">The <see cref="TagHelperOutput"/> this method extends.</param>
        /// <param name="attributeName">The name of the bound attribute.</param>
        /// <param name="context">The <see cref="TagHelperContext"/>.</param>
        /// <remarks>
        /// <para>
        /// Only copies the attribute if <paramref name="tagHelperOutput"/>'s
        /// <see cref="TagHelperOutput.Attributes"/> does not contain an attribute with the given
        /// <paramref name="attributeName"/>.
        /// </para>
        /// <para>
        /// Duplicate attributes same name in <paramref name="context"/>'s <see cref="TagHelperContext.AllAttributes"/>
        /// or <paramref name="tagHelperOutput"/>'s <see cref="TagHelperOutput.Attributes"/> may result in copied
        /// attribute order not being maintained.
        /// </para></remarks>
        public static void CopyHtmlAttribute(
            this TagHelperOutput tagHelperOutput,
            string attributeName,
            TagHelperContext context)
        {
            if (tagHelperOutput == null)
            {
                throw new ArgumentNullException(nameof(tagHelperOutput));
            }

            if (attributeName == null)
            {
                throw new ArgumentNullException(nameof(attributeName));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!tagHelperOutput.Attributes.ContainsName(attributeName))
            {
                var copiedAttribute = false;

                // We iterate context.AllAttributes backwards since we prioritize TagHelperOutput values occurring
                // before the current context.AllAttribtes[i].
                for (var i = context.AllAttributes.Count - 1; i >= 0; i--)
                {
                    // We look for the original attribute so we can restore the exact attribute name the user typed in
                    // approximately the same position where the user wrote it in the Razor source.
                    if (string.Equals(
                        attributeName,
                        context.AllAttributes[i].Name,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        CopyHtmlAttribute(i, tagHelperOutput, context);
                        copiedAttribute = true;
                    }
                }

                if (!copiedAttribute)
                {
                    throw new ArgumentException(
                        Resources.FormatTagHelperOutput_AttributeDoesNotExist(attributeName, nameof(TagHelperContext)),
                        nameof(attributeName));
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
        public static void MergeAttributes(this TagHelperOutput tagHelperOutput, TagBuilder tagBuilder)
        {
            if (tagHelperOutput == null)
            {
                throw new ArgumentNullException(nameof(tagHelperOutput));
            }

            if (tagBuilder == null)
            {
                throw new ArgumentNullException(nameof(tagBuilder));
            }

            foreach (var attribute in tagBuilder.Attributes)
            {
                if (!tagHelperOutput.Attributes.ContainsName(attribute.Key))
                {
                    tagHelperOutput.Attributes.Add(attribute.Key, attribute.Value);
                }
                else if (string.Equals(attribute.Key, "class", StringComparison.OrdinalIgnoreCase))
                {
                    TagHelperAttribute classAttribute;
                    var found = tagHelperOutput.Attributes.TryGetAttribute("class", out classAttribute);
                    Debug.Assert(found);

                    var newAttribute = new TagHelperAttribute(
                        classAttribute.Name,
                        new ClassAttributeHtmlContent(classAttribute.Value, attribute.Value),
                        classAttribute.ValueStyle);

                    tagHelperOutput.Attributes.SetAttribute(newAttribute);
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
            this TagHelperOutput tagHelperOutput,
            IEnumerable<TagHelperAttribute> attributes)
        {
            if (tagHelperOutput == null)
            {
                throw new ArgumentNullException(nameof(tagHelperOutput));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            foreach (var attribute in attributes.ToArray())
            {
                tagHelperOutput.Attributes.Remove(attribute);
            }
        }

        /// <summary>
        /// Adds the given <paramref name="classValue"/> to the <paramref name="tagHelperOutput"/>'s
        /// <see cref="TagHelperOutput.Attributes"/>.
        /// </summary>
        /// <param name="tagHelperOutput">The <see cref="TagHelperOutput"/> this method extends.</param>
        /// <param name="classValue">The class value to add.</param>
        /// <param name="htmlEncoder">The current HTML encoder.</param>
        public static void AddClass(
            this TagHelperOutput tagHelperOutput,
            string classValue,
            HtmlEncoder htmlEncoder)
        {
            if (tagHelperOutput == null)
            {
                throw new ArgumentNullException(nameof(tagHelperOutput));
            }

            if (string.IsNullOrEmpty(classValue))
            {
                return;
            }

            var encodedSpaceChars = SpaceChars.Where(x => !x.Equals('\u0020')).Select(x => htmlEncoder.Encode(x.ToString())).ToArray();

            if (SpaceChars.Any(classValue.Contains) || encodedSpaceChars.Any(value => classValue.IndexOf(value, StringComparison.Ordinal) >= 0))
            {
                throw new ArgumentException(Resources.ArgumentCannotContainHtmlSpace, nameof(classValue));
            }

            if (!tagHelperOutput.Attributes.TryGetAttribute("class", out TagHelperAttribute classAttribute))
            {
                tagHelperOutput.Attributes.Add("class", classValue);
            }
            else
            {
                var currentClassValue = ExtractClassValue(classAttribute, htmlEncoder);

                var encodedClassValue = htmlEncoder.Encode(classValue);

                if (string.Equals(currentClassValue, encodedClassValue, StringComparison.Ordinal))
                {
                    return;
                }

                var arrayOfClasses = currentClassValue.Split(SpaceChars, StringSplitOptions.RemoveEmptyEntries)
                    .SelectMany(perhapsEncoded => perhapsEncoded.Split(encodedSpaceChars, StringSplitOptions.RemoveEmptyEntries))
                    .ToArray();

                if (arrayOfClasses.Contains(encodedClassValue, StringComparer.Ordinal))
                {
                    return;
                }

                var newClassAttribute = new TagHelperAttribute(
                    classAttribute.Name,
                    new HtmlString($"{currentClassValue} {encodedClassValue}"),
                    classAttribute.ValueStyle);

                tagHelperOutput.Attributes.SetAttribute(newClassAttribute);
            }
        }

        /// <summary>
        /// Removes the given <paramref name="classValue"/> from the <paramref name="tagHelperOutput"/>'s
        /// <see cref="TagHelperOutput.Attributes"/>.
        /// </summary>
        /// <param name="tagHelperOutput">The <see cref="TagHelperOutput"/> this method extends.</param>
        /// <param name="classValue">The class value to remove.</param>
        /// <param name="htmlEncoder">The current HTML encoder.</param>
        public static void RemoveClass(
            this TagHelperOutput tagHelperOutput,
            string classValue,
            HtmlEncoder htmlEncoder)
        {
            if (tagHelperOutput == null)
            {
                throw new ArgumentNullException(nameof(tagHelperOutput));
            }

            var encodedSpaceChars = SpaceChars.Where(x => !x.Equals('\u0020')).Select(x => htmlEncoder.Encode(x.ToString())).ToArray();

            if (SpaceChars.Any(classValue.Contains) || encodedSpaceChars.Any(value => classValue.IndexOf(value, StringComparison.Ordinal) >= 0))
            {
                throw new ArgumentException(Resources.ArgumentCannotContainHtmlSpace, nameof(classValue));
            }

            if (!tagHelperOutput.Attributes.TryGetAttribute("class", out TagHelperAttribute classAttribute))
            {
                return;
            }

            var currentClassValue = ExtractClassValue(classAttribute, htmlEncoder);

            if (string.IsNullOrEmpty(currentClassValue))
            {
                return;
            }

            var encodedClassValue = htmlEncoder.Encode(classValue);

            if (string.Equals(currentClassValue, encodedClassValue, StringComparison.Ordinal))
            {
                tagHelperOutput.Attributes.Remove(tagHelperOutput.Attributes["class"]);
                return;
            }

            if (!currentClassValue.Contains(encodedClassValue))
            {
                return;
            }

            var listOfClasses = currentClassValue.Split(SpaceChars, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(perhapsEncoded => perhapsEncoded.Split(encodedSpaceChars, StringSplitOptions.RemoveEmptyEntries))
                .ToList();

            if (!listOfClasses.Contains(encodedClassValue))
            {
                return;
            }

            listOfClasses.RemoveAll(x => x.Equals(encodedClassValue));

            if (listOfClasses.Any())
            {
                var joinedClasses = new HtmlString(string.Join(" ", listOfClasses));
                tagHelperOutput.Attributes.SetAttribute(classAttribute.Name, joinedClasses);
            }
            else
            {
                tagHelperOutput.Attributes.Remove(tagHelperOutput.Attributes["class"]);
            }
        }

        private static string ExtractClassValue(
            TagHelperAttribute classAttribute,
            HtmlEncoder htmlEncoder)
        {
            string extractedClassValue;
            switch (classAttribute.Value)
            {
                case string valueAsString:
                    extractedClassValue = htmlEncoder.Encode(valueAsString);
                    break;
                case HtmlString valueAsHtmlString:
                    extractedClassValue = valueAsHtmlString.Value;
                    break;
                case IHtmlContent htmlContent:
                    using (var stringWriter = new StringWriter())
                    {
                        htmlContent.WriteTo(stringWriter, htmlEncoder);
                        extractedClassValue = stringWriter.ToString();
                    }
                    break;
                default:
                    extractedClassValue = htmlEncoder.Encode(classAttribute.Value?.ToString());
                    break;
            }
            var currentClassValue = extractedClassValue ?? string.Empty;
            return currentClassValue;
        }

        private static void CopyHtmlAttribute(
            int allAttributeIndex,
            TagHelperOutput tagHelperOutput,
            TagHelperContext context)
        {
            var existingAttribute = context.AllAttributes[allAttributeIndex];

            // Move backwards through context.AllAttributes from the provided index until we find a familiar attribute
            // in tagHelperOutput where we can insert the copied value after the familiar one.
            for (var i = allAttributeIndex - 1; i >= 0; i--)
            {
                var previousName = context.AllAttributes[i].Name;
                var index = IndexOfFirstMatch(previousName, tagHelperOutput.Attributes);
                if (index != -1)
                {
                    tagHelperOutput.Attributes.Insert(index + 1, existingAttribute);
                    return;
                }
            }

            // Move forward through context.AllAttributes from the provided index until we find a familiar attribute in
            // tagHelperOutput where we can insert the copied value.
            for (var i = allAttributeIndex + 1; i < context.AllAttributes.Count; i++)
            {
                var nextName = context.AllAttributes[i].Name;
                var index = IndexOfFirstMatch(nextName, tagHelperOutput.Attributes);
                if (index != -1)
                {
                    tagHelperOutput.Attributes.Insert(index, existingAttribute);
                    return;
                }
            }

            // Couldn't determine the attribute's location, add it to the end.
            tagHelperOutput.Attributes.Add(existingAttribute);
        }

        private static int IndexOfFirstMatch(string name, TagHelperAttributeList attributes)
        {
            for (var i = 0; i < attributes.Count; i++)
            {
                if (string.Equals(name, attributes[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private class ClassAttributeHtmlContent : IHtmlContent
        {
            private readonly object _left;
            private readonly string _right;

            public ClassAttributeHtmlContent(object left, string right)
            {
                _left = left;
                _right = right;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                if (writer == null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }

                if (encoder == null)
                {
                    throw new ArgumentNullException(nameof(encoder));
                }

                // Write out "{left} {right}" in the common nothing-empty case.
                var wroteLeft = false;
                if (_left != null)
                {
                    var htmlContent = _left as IHtmlContent;
                    if (htmlContent != null)
                    {
                        // Ignore case where htmlContent is HtmlString.Empty. At worst, will add a leading space to the
                        // generated attribute value.
                        htmlContent.WriteTo(writer, encoder);
                        wroteLeft = true;
                    }
                    else
                    {
                        var stringValue = _left.ToString();
                        if (!string.IsNullOrEmpty(stringValue))
                        {
                            encoder.Encode(writer, stringValue);
                            wroteLeft = true;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(_right))
                {
                    if (wroteLeft)
                    {
                        writer.Write(' ');
                    }

                    encoder.Encode(writer, _right);
                }
            }
        }
    }
}