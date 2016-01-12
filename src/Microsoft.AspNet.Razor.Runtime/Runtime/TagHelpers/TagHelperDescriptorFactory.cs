// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Factory for <see cref="TagHelperDescriptor"/>s from <see cref="Type"/>s.
    /// </summary>
    public class TagHelperDescriptorFactory
    {
        private const string DataDashPrefix = "data-";
        private const string TagHelperNameEnding = "TagHelper";
        private const string HtmlCaseRegexReplacement = "-$1$2";

        // This matches the following AFTER the start of the input string (MATCH).
        // Any letter/number followed by an uppercase letter then lowercase letter: 1(Aa), a(Aa), A(Aa)
        // Any lowercase letter followed by an uppercase letter: a(A)
        // Each match is then prefixed by a "-" via the ToHtmlCase method.
        private static readonly Regex HtmlCaseRegex =
            new Regex(
                "(?<!^)((?<=[a-zA-Z0-9])[A-Z][a-z])|((?<=[a-z])[A-Z])",
                RegexOptions.None,
                Constants.RegexMatchTimeout);

#if !DOTNET5_4
        private readonly TagHelperDesignTimeDescriptorFactory _designTimeDescriptorFactory;
#endif
        private readonly bool _designTime;

        // TODO: Investigate if we should cache TagHelperDescriptors for types:
        // https://github.com/aspnet/Razor/issues/165

        public static ICollection<char> InvalidNonWhitespaceNameCharacters { get; } = new HashSet<char>(
            new[] { '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*' });

        /// <summary>
        /// Instantiates a new <see cref="TagHelperDescriptorFactory"/>.
        /// </summary>
        /// <param name="designTime">
        /// Indicates if <see cref="TagHelperDescriptor"/>s should be created for design time.
        /// </param>
        public TagHelperDescriptorFactory(bool designTime)
        {
#if !DOTNET5_4
            if (designTime)
            {
                _designTimeDescriptorFactory = new TagHelperDesignTimeDescriptorFactory();
            }
#endif

            _designTime = designTime;
        }

        /// <summary>
        /// Creates a <see cref="TagHelperDescriptor"/> from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="assemblyName">The assembly name that contains <paramref name="type"/>.</param>
        /// <param name="type">The <see cref="Type"/> to create a <see cref="TagHelperDescriptor"/> from.
        /// </param>
        /// <param name="errorSink">The <see cref="ErrorSink"/> used to collect <see cref="RazorError"/>s encountered
        /// when creating <see cref="TagHelperDescriptor"/>s for the given <paramref name="type"/>.</param>
        /// <returns>
        /// A collection of <see cref="TagHelperDescriptor"/>s that describe the given <paramref name="type"/>.
        /// </returns>
        public virtual IEnumerable<TagHelperDescriptor> CreateDescriptors(
            string assemblyName,
            Type type,
            ErrorSink errorSink)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (errorSink == null)
            {
                throw new ArgumentNullException(nameof(errorSink));
            }

            if (ShouldSkipDescriptorCreation(type.GetTypeInfo()))
            {
                return Enumerable.Empty<TagHelperDescriptor>();
            }

            var attributeDescriptors = GetAttributeDescriptors(type, errorSink);
            var targetElementAttributes = GetValidHtmlTargetElementAttributes(type, errorSink);
            var allowedChildren = GetAllowedChildren(type, errorSink);

            var tagHelperDescriptors =
                BuildTagHelperDescriptors(
                    type,
                    assemblyName,
                    attributeDescriptors,
                    targetElementAttributes,
                    allowedChildren);

            return tagHelperDescriptors.Distinct(TagHelperDescriptorComparer.Default);
        }

        private static IEnumerable<HtmlTargetElementAttribute> GetValidHtmlTargetElementAttributes(
            Type type,
            ErrorSink errorSink)
        {
            var targetElementAttributes = type
                .GetTypeInfo()
                .GetCustomAttributes<HtmlTargetElementAttribute>(inherit: false);
            return targetElementAttributes.Where(
                attribute => ValidHtmlTargetElementAttributeNames(attribute, errorSink));
        }

        private IEnumerable<TagHelperDescriptor> BuildTagHelperDescriptors(
            Type type,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            IEnumerable<HtmlTargetElementAttribute> targetElementAttributes,
            IEnumerable<string> allowedChildren)
        {
            TagHelperDesignTimeDescriptor typeDesignTimeDescriptor = null;

#if !DOTNET5_4
            if (_designTime)
            {
                typeDesignTimeDescriptor = _designTimeDescriptorFactory.CreateDescriptor(type);
            }
#endif

            var typeName = type.FullName;

            // If there isn't an attribute specifying the tag name derive it from the name
            if (!targetElementAttributes.Any())
            {
                var name = type.Name;

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
                        parentTag: null,
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

        private static IEnumerable<string> GetAllowedChildren(Type type, ErrorSink errorSink)
        {
            var restrictChildrenAttribute = type.GetTypeInfo().GetCustomAttribute<RestrictChildrenAttribute>(inherit: false);
            if (restrictChildrenAttribute == null)
            {
                return null;
            }

            var allowedChildren = restrictChildrenAttribute.ChildTags;
            var validAllowedChildren = GetValidAllowedChildren(allowedChildren, type.FullName, errorSink);

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
                    whitespaceError:
                        Resources.FormatTagHelperDescriptorFactory_InvalidRestrictChildrenAttributeNameNullWhitespace(
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
            HtmlTargetElementAttribute targetElementAttribute,
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
                targetElementAttribute.ParentTag,
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
            string parentTag,
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
                RequiredParent = parentTag,
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
        internal static bool ValidHtmlTargetElementAttributeNames(
            HtmlTargetElementAttribute attribute,
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

            var validParentTagName = ValidateParentTagName(attribute.ParentTag, errorSink);

            return validTagName && validAttributeNames && validParentTagName;
        }

        /// <summary>
        /// Internal for unit testing.
        /// </summary>
        internal static bool ValidateParentTagName(string parentTag, ErrorSink errorSink)
        {
            return parentTag == null ||
                TryValidateName(
                    parentTag,
                    Resources.FormatHtmlTargetElementAttribute_NameCannotBeNullOrWhitespace(
                        Resources.TagHelperDescriptorFactory_ParentTag),
                    characterErrorBuilder: (invalidCharacter) =>
                        Resources.FormatHtmlTargetElementAttribute_InvalidName(
                            Resources.TagHelperDescriptorFactory_ParentTag.ToLower(),
                            parentTag,
                            invalidCharacter),
                    errorSink: errorSink);
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
                // '*' as the entire name is OK in the HtmlTargetElement catch-all case.
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
                whitespaceError: Resources.FormatHtmlTargetElementAttribute_NameCannotBeNullOrWhitespace(targetName),
                characterErrorBuilder: (invalidCharacter) =>
                    Resources.FormatHtmlTargetElementAttribute_InvalidName(
                        targetName.ToLower(),
                        name,
                        invalidCharacter),
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

        private IEnumerable<TagHelperAttributeDescriptor> GetAttributeDescriptors(Type type, ErrorSink errorSink)
        {
            var attributeDescriptors = new List<TagHelperAttributeDescriptor>();

            // Keep indexer descriptors separate to avoid sorting the combined list later.
            var indexerDescriptors = new List<TagHelperAttributeDescriptor>();

            var accessibleProperties = type.GetRuntimeProperties().Where(IsAccessibleProperty);
            foreach (var property in accessibleProperties)
            {
                if (ShouldSkipDescriptorCreation(property))
                {
                    continue;
                }

                var attributeNameAttribute = property
                    .GetCustomAttributes<HtmlAttributeNameAttribute>(inherit: false)
                    .FirstOrDefault();
                var hasExplicitName =
                    attributeNameAttribute != null && !string.IsNullOrEmpty(attributeNameAttribute.Name);
                var attributeName = hasExplicitName ? attributeNameAttribute.Name : ToHtmlCase(property.Name);

                TagHelperAttributeDescriptor mainDescriptor = null;
                if (property.SetMethod != null && property.SetMethod.IsPublic)
                {
                    mainDescriptor = ToAttributeDescriptor(property, attributeName);
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
            Type parentType,
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

        private bool ShouldSkipDescriptorCreation(MemberInfo memberInfo)
        {
            if (_designTime)
            {
                var editorBrowsableAttribute = memberInfo.GetCustomAttribute<EditorBrowsableAttribute>(inherit: false);

                return editorBrowsableAttribute != null &&
                    editorBrowsableAttribute.State == EditorBrowsableState.Never;
            }

            return false;
        }

        private static bool ValidateTagHelperAttributeNameOrPrefix(
            string attributeNameOrPrefix,
            Type parentType,
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

        private TagHelperAttributeDescriptor ToAttributeDescriptor(PropertyInfo property, string attributeName)
        {
            return ToAttributeDescriptor(
                property,
                attributeName,
                property.PropertyType.FullName,
                isIndexer: false,
                isStringProperty: typeof(string) == property.PropertyType);
        }

        private TagHelperAttributeDescriptor ToIndexerAttributeDescriptor(
            PropertyInfo property,
            HtmlAttributeNameAttribute attributeNameAttribute,
            Type parentType,
            ErrorSink errorSink,
            string defaultPrefix,
            out bool isInvalid)
        {
            isInvalid = false;
            var hasPublicSetter = property.SetMethod != null && property.SetMethod.IsPublic;
            var dictionaryTypeArguments = ClosedGenericMatcher.ExtractGenericInterface(
                property.PropertyType,
                typeof(IDictionary<,>))
                ?.GenericTypeArguments
                .Select(type => type.IsGenericParameter ? null : type)
                .ToArray();
            if (dictionaryTypeArguments?[0] != typeof(string))
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
                isStringProperty: typeof(string) == dictionaryTypeArguments[1]);
        }

        private TagHelperAttributeDescriptor ToAttributeDescriptor(
            PropertyInfo property,
            string attributeName,
            string typeName,
            bool isIndexer,
            bool isStringProperty)
        {
            TagHelperAttributeDesignTimeDescriptor propertyDesignTimeDescriptor = null;

#if !DOTNET5_4
            if (_designTime)
            {
                propertyDesignTimeDescriptor = _designTimeDescriptorFactory.CreateAttributeDescriptor(property);
            }
#endif

            return new TagHelperAttributeDescriptor
            {
                Name = attributeName,
                PropertyName = property.Name,
                IsEnum = property.PropertyType.GetTypeInfo().IsEnum,
                TypeName = typeName,
                IsStringProperty = isStringProperty,
                IsIndexer = isIndexer,
                DesignTimeDescriptor = propertyDesignTimeDescriptor
            };
        }

        private static bool IsAccessibleProperty(PropertyInfo property)
        {
            // Accessible properties are those with public getters and without [HtmlAttributeNotBound].
            return property.GetIndexParameters().Length == 0 &&
                property.GetMethod != null &&
                property.GetMethod.IsPublic &&
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