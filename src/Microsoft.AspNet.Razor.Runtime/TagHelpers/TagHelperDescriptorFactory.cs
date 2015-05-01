// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Factory for <see cref="TagHelperDescriptor"/>s from <see cref="Type"/>s.
    /// </summary>
    public static class TagHelperDescriptorFactory
    {
        private const string DataDashPrefix = "data-";
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

        public static ICollection<char> InvalidNonWhitespaceNameCharacters { get; } = new HashSet<char>(
            new[] { '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'' });

        /// <summary>
        /// Creates a <see cref="TagHelperDescriptor"/> from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="assemblyName">The assembly name that contains <paramref name="type"/>.</param>
        /// <param name="type">The type to create a <see cref="TagHelperDescriptor"/> from.</param>
        /// <returns>A <see cref="TagHelperDescriptor"/> that describes the given <paramref name="type"/>.</returns>
        public static IEnumerable<TagHelperDescriptor> CreateDescriptors(
            string assemblyName,
            [NotNull] Type type,
            [NotNull] ErrorSink errorSink)
        {
            var typeInfo = type.GetTypeInfo();
            var attributeDescriptors = GetAttributeDescriptors(type, errorSink);
            var targetElementAttributes = GetValidTargetElementAttributes(typeInfo, errorSink);
            var tagHelperDescriptors =
                BuildTagHelperDescriptors(
                    typeInfo,
                    assemblyName,
                    attributeDescriptors,
                    targetElementAttributes);

            return tagHelperDescriptors.Distinct(TagHelperDescriptorComparer.Default);
        }

        private static IEnumerable<TargetElementAttribute> GetValidTargetElementAttributes(
            TypeInfo typeInfo,
            ErrorSink errorSink)
        {
            var targetElementAttributes = typeInfo.GetCustomAttributes<TargetElementAttribute>(inherit: false);

            return targetElementAttributes.Where(attribute => ValidTargetElementAttributeNames(attribute, errorSink));
        }

        private static IEnumerable<TagHelperDescriptor> BuildTagHelperDescriptors(
            TypeInfo typeInfo,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            IEnumerable<TargetElementAttribute> targetElementAttributes)
        {
            var typeName = typeInfo.FullName;

            // If there isn't an attribute specifying the tag name derive it from the name
            if (!targetElementAttributes.Any())
            {
                var name = typeInfo.Name;

                if (name.EndsWith(TagHelperNameEnding, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - TagHelperNameEnding.Length);
                }

                return new[]
                {
                    BuildTagHelperDescriptor(
                        ToHtmlCase(name),
                        typeName,
                        assemblyName,
                        attributeDescriptors,
                        requiredAttributes: Enumerable.Empty<string>())
                };
            }

            return targetElementAttributes.Select(
                attribute => BuildTagHelperDescriptor(typeName, assemblyName, attributeDescriptors, attribute));
        }

        private static TagHelperDescriptor BuildTagHelperDescriptor(
            string typeName,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            TargetElementAttribute targetElementAttribute)
        {
            var requiredAttributes = GetCommaSeparatedValues(targetElementAttribute.Attributes);

            return BuildTagHelperDescriptor(
                targetElementAttribute.Tag,
                typeName,
                assemblyName,
                attributeDescriptors,
                requiredAttributes);
        }

        private static TagHelperDescriptor BuildTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            IEnumerable<string> requiredAttributes)
        {
            return new TagHelperDescriptor(
                prefix: string.Empty,
                tagName: tagName,
                typeName: typeName,
                assemblyName: assemblyName,
                attributes: attributeDescriptors,
                requiredAttributes: requiredAttributes);
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal static IEnumerable<string> GetCommaSeparatedValues(string text)
        {
            // We don't want to remove empty entries, need to notify users of invalid values.
            return text?.Split(',').Select(tagName => tagName.Trim()) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal static bool ValidTargetElementAttributeNames(
            TargetElementAttribute attribute,
            ErrorSink errorSink)
        {
            var validTagName = ValidateName(attribute.Tag, targetingAttributes: false, errorSink: errorSink);
            var validAttributeNames = true;
            var attributeNames = GetCommaSeparatedValues(attribute.Attributes);

            foreach (var attributeName in attributeNames)
            {
                if (!ValidateName(attributeName, targetingAttributes: true, errorSink: errorSink))
                {
                    validAttributeNames = false;
                }
            }

            return validTagName && validAttributeNames;
        }

        private static bool ValidateName(
            string name,
            bool targetingAttributes,
            ErrorSink errorSink)
        {
            var targetName = targetingAttributes ?
                Resources.TagHelperDescriptorFactory_Attribute :
                Resources.TagHelperDescriptorFactory_Tag;
            var validName = true;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorSink.OnError(
                    SourceLocation.Zero,
                    Resources.FormatTargetElementAttribute_NameCannotBeNullOrWhitespace(targetName));

                validName = false;
            }
            else
            {
                foreach (var character in name)
                {
                    if (char.IsWhiteSpace(character) ||
                        InvalidNonWhitespaceNameCharacters.Contains(character))
                    {
                        errorSink.OnError(
                            SourceLocation.Zero,
                            Resources.FormatTargetElementAttribute_InvalidName(
                                targetName.ToLower(),
                                name,
                                character));

                        validName = false;
                    }
                }
            }

            return validName;
        }

        private static IEnumerable<TagHelperAttributeDescriptor> GetAttributeDescriptors(
            Type type,
            ErrorSink errorSink)
        {
            var accessibleProperties = type.GetRuntimeProperties().Where(IsAccessibleProperty);
            var attributeDescriptors = new List<TagHelperAttributeDescriptor>();

            foreach (var property in accessibleProperties)
            {
                var descriptor = ToAttributeDescriptor(property);
                if (ValidateTagHelperAttributeDescriptor(descriptor, type, errorSink))
                {
                    attributeDescriptors.Add(descriptor);
                }
            }

            return attributeDescriptors;
        }

        private static bool ValidateTagHelperAttributeDescriptor(
            TagHelperAttributeDescriptor attributeDescriptor,
            Type parentType,
            ErrorSink errorSink)
        {
            // data-* attributes are explicitly not implemented by user agents and are not intended for use on
            // the server; therefore it's invalid for TagHelpers to bind to them.
            if (attributeDescriptor.Name.StartsWith(DataDashPrefix, StringComparison.OrdinalIgnoreCase))
            {
                errorSink.OnError(
                    SourceLocation.Zero,
                    Resources.FormatTagHelperDescriptorFactory_InvalidBoundAttributeName(
                        attributeDescriptor.PropertyName,
                        parentType.FullName,
                        DataDashPrefix));

                return false;
            }

            return true;
        }

        private static TagHelperAttributeDescriptor ToAttributeDescriptor(PropertyInfo property)
        {
            var attributeNameAttribute = property.GetCustomAttribute<HtmlAttributeNameAttribute>(inherit: false);
            var attributeName = attributeNameAttribute != null ?
                                attributeNameAttribute.Name :
                                ToHtmlCase(property.Name);

            return new TagHelperAttributeDescriptor(attributeName, property.Name, property.PropertyType.FullName);
        }

        private static bool IsAccessibleProperty(PropertyInfo property)
        {
            // Accessible properties are those with public getters and setters and without [HtmlAttributeNotBound].
            return property.GetMethod?.IsPublic == true &&
                property.SetMethod?.IsPublic == true &&
                property.GetCustomAttribute<HtmlAttributeNotBoundAttribute>(inherit: false) == null;
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