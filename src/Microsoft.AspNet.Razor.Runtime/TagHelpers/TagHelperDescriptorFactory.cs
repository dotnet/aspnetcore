// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Factory for <see cref="TagHelperDescriptor"/>s from <see cref="ITypeInfo"/>s.
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

        private static readonly ITypeInfo StringTypeInfo = new RuntimeTypeInfo(typeof(string).GetTypeInfo());

        // TODO: Investigate if we should cache TagHelperDescriptors for types:
        // https://github.com/aspnet/Razor/issues/165

        public static ICollection<char> InvalidNonWhitespaceNameCharacters { get; } = new HashSet<char>(
            new[] { '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*' });

        /// <summary>
        /// Creates a <see cref="TagHelperDescriptor"/> from the given <paramref name="typeInfo"/>.
        /// </summary>
        /// <param name="assemblyName">The assembly name that contains <paramref name="type"/>.</param>
        /// <param name="typeInfo">The <see cref="ITypeInfo"/> to create a <see cref="TagHelperDescriptor"/> from.
        /// </param>
        /// <param name="designTime">Indicates if the returned <see cref="TagHelperDescriptor"/>s should include
        /// design time specific information.</param>
        /// <param name="errorSink">The <see cref="ErrorSink"/> used to collect <see cref="RazorError"/>s encountered
        /// when creating <see cref="TagHelperDescriptor"/>s for the given <paramref name="typeInfo"/>.</param>
        /// <returns>
        /// A collection of <see cref="TagHelperDescriptor"/>s that describe the given <paramref name="typeInfo"/>.
        /// </returns>
        public static IEnumerable<TagHelperDescriptor> CreateDescriptors(
            string assemblyName,
            [NotNull] ITypeInfo typeInfo,
            bool designTime,
            [NotNull] ErrorSink errorSink)
        {
            if (ShouldSkipDescriptorCreation(designTime, typeInfo))
            {
                return Enumerable.Empty<TagHelperDescriptor>();
            }

            var attributeDescriptors = GetAttributeDescriptors(typeInfo, designTime, errorSink);
            var targetElementAttributes = GetValidTargetElementAttributes(typeInfo, errorSink);
            var allowedChildren = GetAllowedChildren(typeInfo, errorSink);

            var tagHelperDescriptors =
                BuildTagHelperDescriptors(
                    typeInfo,
                    assemblyName,
                    attributeDescriptors,
                    targetElementAttributes,
                    allowedChildren,
                    designTime);

            return tagHelperDescriptors.Distinct(TagHelperDescriptorComparer.Default);
        }

        private static IEnumerable<TargetElementAttribute> GetValidTargetElementAttributes(
            ITypeInfo typeInfo,
            ErrorSink errorSink)
        {
            var targetElementAttributes = typeInfo.GetCustomAttributes<TargetElementAttribute>();

            return targetElementAttributes.Where(attribute => ValidTargetElementAttributeNames(attribute, errorSink));
        }

        private static IEnumerable<TagHelperDescriptor> BuildTagHelperDescriptors(
            ITypeInfo typeInfo,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            IEnumerable<TargetElementAttribute> targetElementAttributes,
            IEnumerable<string> allowedChildren,
            bool designTime)
        {
            TagHelperDesignTimeDescriptor typeDesignTimeDescriptor = null;

#if !DNXCORE50
            if (designTime)
            {
                var runtimeTypeInfo = typeInfo as RuntimeTypeInfo;
                if (runtimeTypeInfo != null)
                {
                    typeDesignTimeDescriptor =
                        TagHelperDesignTimeDescriptorFactory.CreateDescriptor(runtimeTypeInfo.TypeInfo.AsType());
                }
            }
#endif

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
                        requiredAttributes: Enumerable.Empty<string>(),
                        allowedChildren: allowedChildren,
                        tagStructure: default(TagStructure),
                        designTimeDescriptor: typeDesignTimeDescriptor)
                };
            }

            return targetElementAttributes.Select(
                attribute =>
                    BuildTagHelperDescriptor(
                        typeName,
                        assemblyName,
                        attributeDescriptors,
                        attribute,
                        allowedChildren,
                        typeDesignTimeDescriptor));
        }

        private static IEnumerable<string> GetAllowedChildren(ITypeInfo typeInfo, ErrorSink errorSink)
        {
            var restrictChildrenAttribute = typeInfo
                .GetCustomAttributes<RestrictChildrenAttribute>()
                .FirstOrDefault();
            if (restrictChildrenAttribute == null)
            {
                return null;
            }

            var allowedChildren = restrictChildrenAttribute.ChildTagNames;
            var validAllowedChildren = GetValidAllowedChildren(allowedChildren, typeInfo.FullName, errorSink);

            if (validAllowedChildren.Any())
            {
                return validAllowedChildren;
            }
            else
            {
                // All allowed children were invalid, return null to indicate that any child is acceptable.
                return null;
            }
        }

        // Internal for unit testing
        internal static IEnumerable<string> GetValidAllowedChildren(
            IEnumerable<string> allowedChildren,
            string tagHelperName,
            ErrorSink errorSink)
        {
            var validAllowedChildren = new List<string>();

            foreach (var name in allowedChildren)
            {
                var valid = TryValidateName(
                    name,
                    whitespaceError: Resources.FormatTagHelperDescriptorFactory_InvalidRestrictChildrenAttributeNameNullWhitespace(
                        nameof(RestrictChildrenAttribute),
                        tagHelperName),
                    characterErrorBuilder: (invalidCharacter) =>
                        Resources.FormatTagHelperDescriptorFactory_InvalidRestrictChildrenAttributeName(
                            nameof(RestrictChildrenAttribute),
                            name,
                            tagHelperName,
                            invalidCharacter),
                    errorSink: errorSink);

                if (valid)
                {
                    validAllowedChildren.Add(name);
                }
            }

            return validAllowedChildren;
        }

        private static TagHelperDescriptor BuildTagHelperDescriptor(
            string typeName,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            TargetElementAttribute targetElementAttribute,
            IEnumerable<string> allowedChildren,
            TagHelperDesignTimeDescriptor designTimeDescriptor)
        {
            var requiredAttributes = GetCommaSeparatedValues(targetElementAttribute.Attributes);

            return BuildTagHelperDescriptor(
                targetElementAttribute.Tag,
                typeName,
                assemblyName,
                attributeDescriptors,
                requiredAttributes,
                allowedChildren,
                targetElementAttribute.TagStructure,
                designTimeDescriptor);
        }

        private static TagHelperDescriptor BuildTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            IEnumerable<string> requiredAttributes,
            IEnumerable<string> allowedChildren,
            TagStructure tagStructure,
            TagHelperDesignTimeDescriptor designTimeDescriptor)
        {
            return new TagHelperDescriptor
            {
                TagName = tagName,
                TypeName = typeName,
                AssemblyName = assemblyName,
                Attributes = attributeDescriptors,
                RequiredAttributes = requiredAttributes,
                AllowedChildren = allowedChildren,
                TagStructure = tagStructure,
                DesignTimeDescriptor = designTimeDescriptor
            };
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
            if (!targetingAttributes &&
                string.Equals(
                    name,
                    TagHelperDescriptorProvider.ElementCatchAllTarget,
                    StringComparison.OrdinalIgnoreCase))
            {
                // '*' as the entire name is OK in the TargetElement catch-all case.
                return true;
            }
            else if (targetingAttributes &&
                name.EndsWith(
                    TagHelperDescriptorProvider.RequiredAttributeWildcardSuffix,
                    StringComparison.OrdinalIgnoreCase))
            {
                // A single '*' at the end of a required attribute is valid; everywhere else is invalid. Strip it from
                // the end so we can validate the rest of the name.
                name = name.Substring(0, name.Length - 1);
            }

            var targetName = targetingAttributes ?
                Resources.TagHelperDescriptorFactory_Attribute :
                Resources.TagHelperDescriptorFactory_Tag;

            var validName = TryValidateName(
                name,
                whitespaceError: Resources.FormatTargetElementAttribute_NameCannotBeNullOrWhitespace(targetName),
                characterErrorBuilder: (invalidCharacter) =>
                    Resources.FormatTargetElementAttribute_InvalidName(targetName.ToLower(), name, invalidCharacter),
                errorSink: errorSink);

            return validName;
        }

        private static bool TryValidateName(
            string name,
            string whitespaceError,
            Func<char, string> characterErrorBuilder,
            ErrorSink errorSink)
        {
            var validName = true;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorSink.OnError(SourceLocation.Zero, whitespaceError, length: 0);

                validName = false;
            }
            else
            {
                foreach (var character in name)
                {
                    if (char.IsWhiteSpace(character) ||
                        InvalidNonWhitespaceNameCharacters.Contains(character))
                    {
                        var error = characterErrorBuilder(character);
                        errorSink.OnError(SourceLocation.Zero, error, length: 0);

                        validName = false;
                    }
                }
            }

            return validName;
        }

        private static IEnumerable<TagHelperAttributeDescriptor> GetAttributeDescriptors(
            ITypeInfo type,
            bool designTime,
            ErrorSink errorSink)
        {
            var attributeDescriptors = new List<TagHelperAttributeDescriptor>();

            // Keep indexer descriptors separate to avoid sorting the combined list later.
            var indexerDescriptors = new List<TagHelperAttributeDescriptor>();

            var accessibleProperties = type.Properties.Where(IsAccessibleProperty);
            foreach (var property in accessibleProperties)
            {
                if (ShouldSkipDescriptorCreation(designTime, property))
                {
                    continue;
                }

                var attributeNameAttribute = property
                    .GetCustomAttributes<HtmlAttributeNameAttribute>()
                    .FirstOrDefault();
                var hasExplicitName =
                    attributeNameAttribute != null && !string.IsNullOrEmpty(attributeNameAttribute.Name);
                var attributeName = hasExplicitName ? attributeNameAttribute.Name : ToHtmlCase(property.Name);

                TagHelperAttributeDescriptor mainDescriptor = null;
                if (property.HasPublicSetter)
                {
                    mainDescriptor = ToAttributeDescriptor(property, attributeName, designTime);
                    if (!ValidateTagHelperAttributeDescriptor(mainDescriptor, type, errorSink))
                    {
                        // HtmlAttributeNameAttribute.Name is invalid. Ignore this property completely.
                        continue;
                    }
                }
                else if (hasExplicitName)
                {
                    // Specified HtmlAttributeNameAttribute.Name though property has no public setter.
                    errorSink.OnError(
                        SourceLocation.Zero,
                        Resources.FormatTagHelperDescriptorFactory_InvalidAttributeNameNotNullOrEmpty(
                            type.FullName,
                            property.Name,
                            typeof(HtmlAttributeNameAttribute).FullName,
                            nameof(HtmlAttributeNameAttribute.Name)),
                        length: 0);
                    continue;
                }

                bool isInvalid;
                var indexerDescriptor = ToIndexerAttributeDescriptor(
                    property,
                    attributeNameAttribute,
                    parentType: type,
                    errorSink: errorSink,
                    defaultPrefix: attributeName + "-",
                    designTime: designTime,
                    isInvalid: out isInvalid);
                if (indexerDescriptor != null &&
                    !ValidateTagHelperAttributeDescriptor(indexerDescriptor, type, errorSink))
                {
                    isInvalid = true;
                }

                if (isInvalid)
                {
                    // The property type or HtmlAttributeNameAttribute.DictionaryAttributePrefix (or perhaps the
                    // HTML-casing of the property name) is invalid. Ignore this property completely.
                    continue;
                }

                if (mainDescriptor != null)
                {
                    attributeDescriptors.Add(mainDescriptor);
                }

                if (indexerDescriptor != null)
                {
                    indexerDescriptors.Add(indexerDescriptor);
                }
            }

            attributeDescriptors.AddRange(indexerDescriptors);

            return attributeDescriptors;
        }

        // Internal for testing.
        internal static bool ValidateTagHelperAttributeDescriptor(
            TagHelperAttributeDescriptor attributeDescriptor,
            ITypeInfo parentType,
            ErrorSink errorSink)
        {
            string nameOrPrefix;
            if (attributeDescriptor.IsIndexer)
            {
                nameOrPrefix = Resources.TagHelperDescriptorFactory_Prefix;
            }
            else if (string.IsNullOrEmpty(attributeDescriptor.Name))
            {
                errorSink.OnError(
                    SourceLocation.Zero,
                    Resources.FormatTagHelperDescriptorFactory_InvalidAttributeNameNullOrEmpty(
                        parentType.FullName,
                        attributeDescriptor.PropertyName),
                    length: 0);

                return false;
            }
            else
            {
                nameOrPrefix = Resources.TagHelperDescriptorFactory_Name;
            }

            return ValidateTagHelperAttributeNameOrPrefix(
                attributeDescriptor.Name,
                parentType,
                attributeDescriptor.PropertyName,
                errorSink,
                nameOrPrefix);
        }

        private static bool ShouldSkipDescriptorCreation(bool designTime, IMemberInfo memberInfo)
        {
            if (designTime)
            {
                var editorBrowsableAttribute = memberInfo
                    .GetCustomAttributes<EditorBrowsableAttribute>()
                    .FirstOrDefault();

                return editorBrowsableAttribute != null &&
                    editorBrowsableAttribute.State == EditorBrowsableState.Never;
            }

            return false;
        }

        private static bool ValidateTagHelperAttributeNameOrPrefix(
            string attributeNameOrPrefix,
            ITypeInfo parentType,
            string propertyName,
            ErrorSink errorSink,
            string nameOrPrefix)
        {
            if (string.IsNullOrEmpty(attributeNameOrPrefix))
            {
                // ValidateTagHelperAttributeDescriptor validates Name is non-null and non-empty. The empty string is
                // valid for DictionaryAttributePrefix and null is impossible at this point because it means "don't
                // create a descriptor". (Empty DictionaryAttributePrefix is a corner case which would bind every
                // attribute of a target element. Likely not particularly useful but unclear what minimum length
                // should be required and what scenarios a minimum length would break.)
                return true;
            }

            if (string.IsNullOrWhiteSpace(attributeNameOrPrefix))
            {
                // Provide a single error if the entire name is whitespace, not an error per character.
                errorSink.OnError(
                    SourceLocation.Zero,
                    Resources.FormatTagHelperDescriptorFactory_InvalidAttributeNameOrPrefixWhitespace(
                        parentType.FullName,
                        propertyName,
                        nameOrPrefix),
                    length: 0);

                return false;
            }

            // data-* attributes are explicitly not implemented by user agents and are not intended for use on
            // the server; therefore it's invalid for TagHelpers to bind to them.
            if (attributeNameOrPrefix.StartsWith(DataDashPrefix, StringComparison.OrdinalIgnoreCase))
            {
                errorSink.OnError(
                    SourceLocation.Zero,
                    Resources.FormatTagHelperDescriptorFactory_InvalidAttributeNameOrPrefixStart(
                        parentType.FullName,
                        propertyName,
                        nameOrPrefix,
                        attributeNameOrPrefix,
                        DataDashPrefix),
                    length: 0);

                return false;
            }

            var isValid = true;
            foreach (var character in attributeNameOrPrefix)
            {
                if (char.IsWhiteSpace(character) || InvalidNonWhitespaceNameCharacters.Contains(character))
                {
                    errorSink.OnError(
                        SourceLocation.Zero,
                        Resources.FormatTagHelperDescriptorFactory_InvalidAttributeNameOrPrefixCharacter(
                            parentType.FullName,
                            propertyName,
                            nameOrPrefix,
                            attributeNameOrPrefix,
                            character),
                    length: 0);

                    isValid = false;
                }
            }

            return isValid;
        }

        private static TagHelperAttributeDescriptor ToAttributeDescriptor(
            IPropertyInfo property,
            string attributeName,
            bool designTime)
        {
            return ToAttributeDescriptor(
                property,
                attributeName,
                property.PropertyType.FullName,
                isIndexer: false,
                designTime: designTime);
        }

        private static TagHelperAttributeDescriptor ToIndexerAttributeDescriptor(
            IPropertyInfo property,
            HtmlAttributeNameAttribute attributeNameAttribute,
            ITypeInfo parentType,
            ErrorSink errorSink,
            string defaultPrefix,
            bool designTime,
            out bool isInvalid)
        {
            isInvalid = false;
            var hasPublicSetter = property.HasPublicSetter;
            var dictionaryTypeArguments = property.PropertyType.GetGenericDictionaryParameters();
            if (!StringTypeInfo.Equals(dictionaryTypeArguments?[0]))
            {
                if (attributeNameAttribute?.DictionaryAttributePrefix != null)
                {
                    // DictionaryAttributePrefix is not supported unless associated with an
                    // IDictionary<string, TValue> property.
                    isInvalid = true;
                    errorSink.OnError(
                        SourceLocation.Zero,
                        Resources.FormatTagHelperDescriptorFactory_InvalidAttributePrefixNotNull(
                            parentType.FullName,
                            property.Name,
                            nameof(HtmlAttributeNameAttribute),
                            nameof(HtmlAttributeNameAttribute.DictionaryAttributePrefix),
                            "IDictionary<string, TValue>"),
                        length: 0);
                }
                else if (attributeNameAttribute != null && !hasPublicSetter)
                {
                    // Associated an HtmlAttributeNameAttribute with a non-dictionary property that lacks a public
                    // setter.
                    isInvalid = true;
                    errorSink.OnError(
                        SourceLocation.Zero,
                        Resources.FormatTagHelperDescriptorFactory_InvalidAttributeNameAttribute(
                            parentType.FullName,
                            property.Name,
                            nameof(HtmlAttributeNameAttribute),
                            "IDictionary<string, TValue>"),
                        length: 0);
                }

                return null;
            }
            else if (!hasPublicSetter &&
                attributeNameAttribute != null &&
                !attributeNameAttribute.DictionaryAttributePrefixSet)
            {
                // Must set DictionaryAttributePrefix when using HtmlAttributeNameAttribute with a dictionary property
                // that lacks a public setter.
                isInvalid = true;
                errorSink.OnError(
                    SourceLocation.Zero,
                    Resources.FormatTagHelperDescriptorFactory_InvalidAttributePrefixNull(
                        parentType.FullName,
                        property.Name,
                        nameof(HtmlAttributeNameAttribute),
                        nameof(HtmlAttributeNameAttribute.DictionaryAttributePrefix),
                        "IDictionary<string, TValue>"),
                    length: 0);

                return null;
            }

            // Potential prefix case. Use default prefix (based on name)?
            var useDefault = attributeNameAttribute == null || !attributeNameAttribute.DictionaryAttributePrefixSet;

            var prefix = useDefault ? defaultPrefix : attributeNameAttribute.DictionaryAttributePrefix;
            if (prefix == null)
            {
                // DictionaryAttributePrefix explicitly set to null. Ignore.
                return null;
            }

            return ToAttributeDescriptor(
                property,
                attributeName: prefix,
                typeName: dictionaryTypeArguments[1].FullName,
                isIndexer: true,
                designTime: designTime);
        }

        private static TagHelperAttributeDescriptor ToAttributeDescriptor(
            IPropertyInfo property,
            string attributeName,
            string typeName,
            bool isIndexer,
            bool designTime)
        {
            TagHelperAttributeDesignTimeDescriptor propertyDesignTimeDescriptor = null;

#if !DNXCORE50
            if (designTime)
            {
                var runtimeProperty = property as RuntimePropertyInfo;
                if (runtimeProperty != null)
                {
                    propertyDesignTimeDescriptor =
                        TagHelperDesignTimeDescriptorFactory.CreateAttributeDescriptor(runtimeProperty.Property);
                }
            }
#endif

            return new TagHelperAttributeDescriptor
            {
                Name = attributeName,
                PropertyName = property.Name,
                TypeName = typeName,
                IsIndexer = isIndexer,
                DesignTimeDescriptor = propertyDesignTimeDescriptor
            };
        }

        private static bool IsAccessibleProperty(IPropertyInfo property)
        {
            // Accessible properties are those with public getters and without [HtmlAttributeNotBound].
            return property.HasPublicGetter &&
                property.GetCustomAttributes<HtmlAttributeNotBoundAttribute>().FirstOrDefault() == null;
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