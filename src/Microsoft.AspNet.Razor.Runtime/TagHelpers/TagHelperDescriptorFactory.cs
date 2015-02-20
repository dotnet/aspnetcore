// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Factory for <see cref="TagHelperDescriptor"/>s from <see cref="Type"/>s.
    /// </summary>
    public static class TagHelperDescriptorFactory
    {
        private const string TagHelperNameEnding = "TagHelper";
        private const string HtmlCaseRegexReplacement = "-$1$2";

        // This matches the following AFTER the start of the input string (MATCH).
        // Any letter/number followed by an uppercase letter then lowercase letter: 1(Aa), a(Aa), A(Aa)
        // Any lowercase letter followed by an uppercase letter: a(A)
        // Each match is then prefixed by a "-" via the ToHtmlCase method.
        private static readonly Regex HtmlCaseRegex =
            new Regex("(?<!^)((?<=[a-zA-Z0-9])[A-Z][a-z])|((?<=[a-z])[A-Z])", RegexOptions.None);

        // TODO: Investigate if we should cache TagHelperDescriptors for types:
        // https://github.com/aspnet/Razor/issues/165

        /// <summary>
        /// Creates a <see cref="TagHelperDescriptor"/> from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="assemblyName">The assembly name that contains <paramref name="type"/>.</param>
        /// <param name="type">The type to create a <see cref="TagHelperDescriptor"/> from.</param>
        /// <returns>A <see cref="TagHelperDescriptor"/> that describes the given <paramref name="type"/>.</returns>
        public static IEnumerable<TagHelperDescriptor> CreateDescriptors(string assemblyName, Type type)
        {
            var tagNames = GetTagNames(type);
            var typeName = type.FullName;
            var attributeDescriptors = GetAttributeDescriptors(type);

            return tagNames.Select(tagName =>
                new TagHelperDescriptor(tagName,
                                        typeName,
                                        assemblyName,
                                        attributeDescriptors));
        }

        private static IEnumerable<string> GetTagNames(Type tagHelperType)
        {
            var typeInfo = tagHelperType.GetTypeInfo();
            var attributes = typeInfo.GetCustomAttributes<HtmlElementNameAttribute>(inherit: false);

            // If there isn't an attribute specifying the tag name derive it from the name
            if (!attributes.Any())
            {
                var name = typeInfo.Name;

                if (name.EndsWith(TagHelperNameEnding, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - TagHelperNameEnding.Length);
                }

                return new[] { ToHtmlCase(name) };
            }

            // Remove duplicate tag names.
            return attributes.SelectMany(attribute => attribute.Tags).Distinct();
        }

        private static IEnumerable<TagHelperAttributeDescriptor> GetAttributeDescriptors(Type type)
        {
            var properties = type.GetRuntimeProperties().Where(IsValidProperty);
            var attributeDescriptors = properties.Select(ToAttributeDescriptor);

            return attributeDescriptors;
        }

        private static TagHelperAttributeDescriptor ToAttributeDescriptor(PropertyInfo property)
        {
            var attributeNameAttribute = property.GetCustomAttribute<HtmlAttributeNameAttribute>(inherit: false);
            var attributeName = attributeNameAttribute != null ?
                                attributeNameAttribute.Name :
                                ToHtmlCase(property.Name);

            return new TagHelperAttributeDescriptor(attributeName, property.Name, property.PropertyType.FullName);
        }

        private static bool IsValidProperty(PropertyInfo property)
        {
            return property.GetMethod != null &&
                   property.GetMethod.IsPublic &&
                   property.SetMethod != null &&
                   property.SetMethod.IsPublic;
        }

        /// <summary>
        /// Converts from pascal/camel case to lower kebab-case.
        /// </summary>
        /// <example>
        /// SomeThing => some-thing
        /// capsONInside => caps-on-inside
        /// CAPSOnOUTSIDE => caps-on-outside
        /// ALLCAPS => allcaps
        /// One1Two2Three3 => one1-two2-three3
        /// ONE1TWO2THREE3 => one1two2three3
        /// First_Second_ThirdHi => first_second_third-hi
        /// </example>
        private static string ToHtmlCase(string name)
        {
            return HtmlCaseRegex.Replace(name, HtmlCaseRegexReplacement).ToLowerInvariant();
        }
    }
}