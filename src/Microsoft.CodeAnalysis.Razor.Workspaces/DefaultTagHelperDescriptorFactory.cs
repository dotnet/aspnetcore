// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Microsoft.AspNetCore.Razor.Evolution;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTagHelperDescriptorFactory
    {
        private const string DataDashPrefix = "data-";
        private const string TagHelperNameEnding = "TagHelper";
        private const string HtmlCaseRegexReplacement = "-$1$2";
        private const char RequiredAttributeWildcardSuffix = '*';

        // This matches the following AFTER the start of the input string (MATCH).
        // Any letter/number followed by an uppercase letter then lowercase letter: 1(Aa), a(Aa), A(Aa)
        // Any lowercase letter followed by an uppercase letter: a(A)
        // Each match is then prefixed by a "-" via the ToHtmlCase method.
        private static readonly Regex HtmlCaseRegex =
            new Regex(
                "(?<!^)((?<=[a-zA-Z0-9])[A-Z][a-z])|((?<=[a-z])[A-Z])",
                RegexOptions.None,
                TimeSpan.FromMilliseconds(500));

        private readonly INamedTypeSymbol _htmlAttributeNameAttributeSymbol;
        private readonly INamedTypeSymbol _htmlAttributeNotBoundAttributeSymbol;
        private readonly INamedTypeSymbol _htmlTargetElementAttributeSymbol;
        private readonly INamedTypeSymbol _iDictionarySymbol;
        private readonly INamedTypeSymbol _restrictChildrenAttributeSymbol;

        public static ICollection<char> InvalidNonWhitespaceNameCharacters { get; } = new HashSet<char>(
            new[] { '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*' });

        public DefaultTagHelperDescriptorFactory(Compilation compilation)
        {
            Compilation = compilation;

            _htmlAttributeNameAttributeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.HtmlAttributeNameAttribute);
            _htmlAttributeNotBoundAttributeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.HtmlAttributeNotBoundAttribute);
            _htmlTargetElementAttributeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.HtmlTargetElementAttribute);
            _restrictChildrenAttributeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.RestrictChildrenAttribute);

            _iDictionarySymbol = Compilation.GetTypeByMetadataName(TagHelperTypes.IDictionary);
        }

        protected Compilation Compilation { get; }

        /// <inheritdoc />
        public virtual IEnumerable<TagHelperDescriptor> CreateDescriptors(
            string assemblyName,
            INamedTypeSymbol type,
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

        private IEnumerable<AttributeData> GetValidHtmlTargetElementAttributes(
            INamedTypeSymbol typeSymbol,
            ErrorSink errorSink)
        {
            var targetElementAttributes = typeSymbol.GetAttributes().Where(a => a.AttributeClass == _htmlTargetElementAttributeSymbol);
            return targetElementAttributes.Where(a => ValidHtmlTargetElementAttributeNames(a, errorSink));
        }

        private IEnumerable<TagHelperDescriptor> BuildTagHelperDescriptors(
            INamedTypeSymbol type,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            IEnumerable<AttributeData> targetElementAttributes,
            IEnumerable<string> allowedChildren)
        {
            TagHelperDesignTimeDescriptor typeDesignTimeDescriptor = null;

            var typeName = GetFullName(type);

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
                        requiredAttributeDescriptors: Enumerable.Empty<TagHelperRequiredAttributeDescriptor>(),
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

        private IEnumerable<string> GetAllowedChildren(INamedTypeSymbol type, ErrorSink errorSink)
        {
            var restrictChildrenAttribute = type.GetAttributes().Where(a => a.AttributeClass == _restrictChildrenAttributeSymbol).FirstOrDefault();
            if (restrictChildrenAttribute == null)
            {
                return null;
            }

            var allowedChildren = new List<string>();
            allowedChildren.Add((string)restrictChildrenAttribute.ConstructorArguments[0].Value);

            if (restrictChildrenAttribute.ConstructorArguments.Length == 2)
            {
                foreach (var value in restrictChildrenAttribute.ConstructorArguments[1].Values)
                {
                    allowedChildren.Add((string)value.Value);
                }
            }
            
            var validAllowedChildren = GetValidAllowedChildren(allowedChildren, GetFullName(type), errorSink);

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
                    whitespaceError: "invalid",
                    characterErrorBuilder: (invalidCharacter) => "invalid",
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
            AttributeData targetElementAttribute,
            IEnumerable<string> allowedChildren,
            TagHelperDesignTimeDescriptor designTimeDescriptor)
        {
            IEnumerable<TagHelperRequiredAttributeDescriptor> requiredAttributeDescriptors;
            TryGetRequiredAttributeDescriptors(
                HtmlTargetElementAttribute_Attributes(targetElementAttribute),
                errorSink: null,
                descriptors: out requiredAttributeDescriptors);

            return BuildTagHelperDescriptor(
                HtmlTargetElementAttribute_Tag(targetElementAttribute),
                typeName,
                assemblyName,
                attributeDescriptors,
                requiredAttributeDescriptors,
                allowedChildren,
                HtmlTargetElementAttribute_ParentTag(targetElementAttribute),
                HtmlTargetElementAttribute_TagStructure(targetElementAttribute),
                designTimeDescriptor);
        }

        private static string HtmlTargetElementAttribute_Attributes(AttributeData attibute)
        {
            foreach (var kvp in attibute.NamedArguments)
            {
                if (kvp.Key == TagHelperTypes.HtmlTargetElement.Attributes)
                {
                    return (string)kvp.Value.Value;
                }
            }

            return null;
        }

        private static string HtmlTargetElementAttribute_ParentTag(AttributeData attibute)
        {
            foreach (var kvp in attibute.NamedArguments)
            {
                if (kvp.Key == TagHelperTypes.HtmlTargetElement.ParentTag)
                {
                    return (string)kvp.Value.Value;
                }
            }

            return null;
        }

        private static string HtmlTargetElementAttribute_Tag(AttributeData attibute)
        {
            if (attibute.ConstructorArguments.Length == 0)
            {
                return null;
            }
            else
            {
                return (string)attibute.ConstructorArguments[0].Value;
            }
        }

        private static TagStructure HtmlTargetElementAttribute_TagStructure(AttributeData attibute)
        {
            foreach (var kvp in attibute.NamedArguments)
            {
                if (kvp.Key == TagHelperTypes.HtmlTargetElement.TagStructure)
                {
                    return (TagStructure)kvp.Value.Value;
                }
            }

            return TagStructure.Unspecified;
        }

        private static TagHelperDescriptor BuildTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            IEnumerable<TagHelperRequiredAttributeDescriptor> requiredAttributeDescriptors,
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
                RequiredAttributes = requiredAttributeDescriptors,
                AllowedChildren = allowedChildren,
                RequiredParent = parentTag,
                TagStructure = tagStructure,
                DesignTimeDescriptor = designTimeDescriptor
            };
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal static bool ValidHtmlTargetElementAttributeNames(
            AttributeData attribute,
            ErrorSink errorSink)
        {
            var validTagName = ValidateName(HtmlTargetElementAttribute_Tag(attribute), targetingAttributes: false, errorSink: errorSink);
            IEnumerable<TagHelperRequiredAttributeDescriptor> requiredAttributeDescriptors;
            var validRequiredAttributes = TryGetRequiredAttributeDescriptors(HtmlTargetElementAttribute_Attributes(attribute), errorSink, out requiredAttributeDescriptors);
            var validParentTagName = ValidateParentTagName(HtmlTargetElementAttribute_ParentTag(attribute), errorSink);

            return validTagName && validRequiredAttributes && validParentTagName;
        }

        /// <summary>
        /// Internal for unit testing.
        /// </summary>
        internal static bool ValidateParentTagName(string parentTag, ErrorSink errorSink)
        {
            return parentTag == null ||
                TryValidateName(
                    parentTag,
                    "invalid",
                    characterErrorBuilder: (invalidCharacter) => "invalid",
                    errorSink: errorSink);
        }

        private static bool TryGetRequiredAttributeDescriptors(
            string requiredAttributes,
            ErrorSink errorSink,
            out IEnumerable<TagHelperRequiredAttributeDescriptor> descriptors)
        {
            var parser = new RequiredAttributeParser(requiredAttributes);

            return parser.TryParse(errorSink, out descriptors);
        }

        private static bool ValidateName(string name, bool targetingAttributes, ErrorSink errorSink)
        {
            if (!targetingAttributes &&
                string.Equals(
                    name,
                    "*",
                    StringComparison.OrdinalIgnoreCase))
            {
                // '*' as the entire name is OK in the HtmlTargetElement catch-all case.
                return true;
            }

            var targetName = targetingAttributes ? "invalid" : "invalid";

            var validName = TryValidateName(
                name,
                whitespaceError: "invalid",
                characterErrorBuilder: (invalidCharacter) => "invalid",
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

        private IEnumerable<TagHelperAttributeDescriptor> GetAttributeDescriptors(INamedTypeSymbol type, ErrorSink errorSink)
        {

            var attributeDescriptors = new List<TagHelperAttributeDescriptor>();

            // Keep indexer descriptors separate to avoid sorting the combined list later.
            var indexerDescriptors = new List<TagHelperAttributeDescriptor>();

            var accessibleProperties = type.GetMembers().OfType<IPropertySymbol>().Where(IsAccessibleProperty);
            foreach (var property in accessibleProperties)
            {
                var attributeNameAttribute = property
                    .GetAttributes()
                    .Where(a => a.AttributeClass == _htmlAttributeNameAttributeSymbol)
                    .FirstOrDefault();

                bool hasExplicitName;
                string attributeName;
                if (attributeNameAttribute == null || 
                    attributeNameAttribute.ConstructorArguments.Length == 0 || 
                    string.IsNullOrEmpty((string)attributeNameAttribute.ConstructorArguments[0].Value))
                {
                    hasExplicitName = false;
                    attributeName = ToHtmlCase(property.Name);
                }
                else
                {
                    hasExplicitName = true;
                    attributeName = (string)attributeNameAttribute.ConstructorArguments[0].Value;
                }

                TagHelperAttributeDescriptor mainDescriptor = null;
                if (property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public)
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
                        "invalid",
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
            INamedTypeSymbol parentType,
            ErrorSink errorSink)
        {
            string nameOrPrefix;
            if (attributeDescriptor.IsIndexer)
            {
                nameOrPrefix = "invalid";
            }
            else if (string.IsNullOrEmpty(attributeDescriptor.Name))
            {
                errorSink.OnError(
                    SourceLocation.Zero,
                    "invalid",
                    length: 0);

                return false;
            }
            else
            {
                nameOrPrefix = "invalid";
            }

            return ValidateTagHelperAttributeNameOrPrefix(
                attributeDescriptor.Name,
                parentType,
                attributeDescriptor.PropertyName,
                errorSink,
                nameOrPrefix);
        }

        private static bool ValidateTagHelperAttributeNameOrPrefix(
            string attributeNameOrPrefix,
            INamedTypeSymbol parentType,
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
                    "invalid",
                    length: 0);

                return false;
            }

            // data-* attributes are explicitly not implemented by user agents and are not intended for use on
            // the server; therefore it's invalid for TagHelpers to bind to them.
            if (attributeNameOrPrefix.StartsWith(DataDashPrefix, StringComparison.OrdinalIgnoreCase))
            {
                errorSink.OnError(
                    SourceLocation.Zero,
                    "invalid",
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
                        "invalid",
                    length: 0);

                    isValid = false;
                }
            }

            return isValid;
        }

        private TagHelperAttributeDescriptor ToAttributeDescriptor(IPropertySymbol property, string attributeName)
        {
            return ToAttributeDescriptor(
                property,
                attributeName,
                GetFullName(property.Type),
                isIndexer: false,
                isStringProperty: property.Type.SpecialType == SpecialType.System_String);
        }

        private TagHelperAttributeDescriptor ToIndexerAttributeDescriptor(
            IPropertySymbol property,
            AttributeData attributeNameAttribute,
            INamedTypeSymbol parentType,
            ErrorSink errorSink,
            string defaultPrefix,
            out bool isInvalid)
        {
            isInvalid = false;
            var hasPublicSetter = property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public;


            string dictionaryAttributePrefix = null;
            bool dictionaryAttributePrefixSet = false;

            if (attributeNameAttribute != null)
            {
                foreach (var argument in attributeNameAttribute.NamedArguments)
                {
                    if (argument.Key == TagHelperTypes.HtmlAttributeName.DictionaryAttributePrefix)
                    {
                        dictionaryAttributePrefix = (string)argument.Value.Value;
                        dictionaryAttributePrefixSet = true;
                        break;
                    }
                }
            }

            INamedTypeSymbol dictionaryType;
            if ((property.Type as INamedTypeSymbol)?.ConstructedFrom == _iDictionarySymbol)
            {
                dictionaryType = (INamedTypeSymbol)property.Type;
            }
            else if (property.Type.AllInterfaces.Any(s => s.ConstructedFrom == _iDictionarySymbol))
            {
                dictionaryType = property.Type.AllInterfaces.First(s => s.ConstructedFrom == _iDictionarySymbol);
            }
            else
            {
                dictionaryType = null;
            }
            
            if (dictionaryType == null ||
                dictionaryType.TypeArguments[0].SpecialType != SpecialType.System_String)
            {
                if (dictionaryAttributePrefix != null)
                {
                    // DictionaryAttributePrefix is not supported unless associated with an
                    // IDictionary<string, TValue> property.
                    isInvalid = true;
                    errorSink.OnError(
                        SourceLocation.Zero,
                        "invalid",
                        length: 0);
                }
                else if (attributeNameAttribute != null && !hasPublicSetter)
                {
                    // Associated an HtmlAttributeNameAttribute with a non-dictionary property that lacks a public
                    // setter.
                    isInvalid = true;
                    errorSink.OnError(
                        SourceLocation.Zero,
                        "invalid",
                        length: 0);
                }

                return null;
            }
            else if (
                !hasPublicSetter &&
                attributeNameAttribute != null &&
                !dictionaryAttributePrefixSet)
            {
                // Must set DictionaryAttributePrefix when using HtmlAttributeNameAttribute with a dictionary property
                // that lacks a public setter.
                isInvalid = true;
                errorSink.OnError(
                    SourceLocation.Zero,
                    "invalid",
                    length: 0);

                return null;
            }

            // Potential prefix case. Use default prefix (based on name)?
            var useDefault = attributeNameAttribute == null || !dictionaryAttributePrefixSet;

            var prefix = useDefault ? defaultPrefix : dictionaryAttributePrefix;
            if (prefix == null)
            {
                // DictionaryAttributePrefix explicitly set to null. Ignore.
                return null;
            }

            return ToAttributeDescriptor(
                property,
                attributeName: prefix,
                typeName: dictionaryType == null ? null : GetFullName(dictionaryType.TypeArguments[1]),
                isIndexer: true,
                isStringProperty: dictionaryType == null ? false : dictionaryType.TypeArguments[1].SpecialType == SpecialType.System_String);
        }

        private TagHelperAttributeDescriptor ToAttributeDescriptor(
            IPropertySymbol property,
            string attributeName,
            string typeName,
            bool isIndexer,
            bool isStringProperty)
        {
            return new TagHelperAttributeDescriptor
            {
                Name = attributeName,
                PropertyName = property.Name,
                IsEnum = property.Type.TypeKind == TypeKind.Enum,
                TypeName = typeName,
                IsStringProperty = isStringProperty,
                IsIndexer = isIndexer,
            };
        }

        private bool IsAccessibleProperty(IPropertySymbol property)
        {
            // Accessible properties are those with public getters and without [HtmlAttributeNotBound].
            return property.Parameters.Length == 0 &&
                property.GetMethod != null &&
                property.GetMethod.DeclaredAccessibility == Accessibility.Public &&
                property.GetAttributes().Where(a => a.AttributeClass == _htmlAttributeNotBoundAttributeSymbol).FirstOrDefault() == null;
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

        private static string GetFullName(ITypeSymbol type)
        {
            return type.ContainingNamespace.ToDisplayString() + "." + type.Name;
        }

        // Internal for testing
        internal class RequiredAttributeParser
        {
            private static readonly IReadOnlyDictionary<char, TagHelperRequiredAttributeValueComparison> CssValueComparisons =
                new Dictionary<char, TagHelperRequiredAttributeValueComparison>
                {
                    { '=', TagHelperRequiredAttributeValueComparison.FullMatch },
                    { '^', TagHelperRequiredAttributeValueComparison.PrefixMatch },
                    { '$', TagHelperRequiredAttributeValueComparison.SuffixMatch }
                };
            private static readonly char[] InvalidPlainAttributeNameCharacters = { ' ', '\t', ',', RequiredAttributeWildcardSuffix };
            private static readonly char[] InvalidCssAttributeNameCharacters = (new[] { ' ', '\t', ',', ']' })
                .Concat(CssValueComparisons.Keys)
                .ToArray();
            private static readonly char[] InvalidCssQuotelessValueCharacters = { ' ', '\t', ']' };

            private int _index;
            private string _requiredAttributes;

            public RequiredAttributeParser(string requiredAttributes)
            {
                _requiredAttributes = requiredAttributes;
            }

            private char Current => _requiredAttributes[_index];

            private bool AtEnd => _index >= _requiredAttributes.Length;

            public bool TryParse(
                ErrorSink errorSink,
                out IEnumerable<TagHelperRequiredAttributeDescriptor> requiredAttributeDescriptors)
            {
                if (string.IsNullOrEmpty(_requiredAttributes))
                {
                    requiredAttributeDescriptors = Enumerable.Empty<TagHelperRequiredAttributeDescriptor>();
                    return true;
                }

                requiredAttributeDescriptors = null;
                var descriptors = new List<TagHelperRequiredAttributeDescriptor>();

                PassOptionalWhitespace();

                do
                {
                    TagHelperRequiredAttributeDescriptor descriptor;
                    if (At('['))
                    {
                        descriptor = ParseCssSelector(errorSink);
                    }
                    else
                    {
                        descriptor = ParsePlainSelector(errorSink);
                    }

                    if (descriptor == null)
                    {
                        // Failed to create the descriptor due to an invalid required attribute.
                        return false;
                    }
                    else
                    {
                        descriptors.Add(descriptor);
                    }

                    PassOptionalWhitespace();

                    if (At(','))
                    {
                        _index++;

                        if (!EnsureNotAtEnd(errorSink))
                        {
                            return false;
                        }
                    }
                    else if (!AtEnd)
                    {
                        errorSink.OnError(
                            SourceLocation.Zero,
                            "invalid",
                            length: 0);
                        return false;
                    }

                    PassOptionalWhitespace();
                }
                while (!AtEnd);

                requiredAttributeDescriptors = descriptors;
                return true;
            }

            private TagHelperRequiredAttributeDescriptor ParsePlainSelector(ErrorSink errorSink)
            {
                var nameEndIndex = _requiredAttributes.IndexOfAny(InvalidPlainAttributeNameCharacters, _index);
                string attributeName;

                var nameComparison = TagHelperRequiredAttributeNameComparison.FullMatch;
                if (nameEndIndex == -1)
                {
                    attributeName = _requiredAttributes.Substring(_index);
                    _index = _requiredAttributes.Length;
                }
                else
                {
                    attributeName = _requiredAttributes.Substring(_index, nameEndIndex - _index);
                    _index = nameEndIndex;

                    if (_requiredAttributes[nameEndIndex] == RequiredAttributeWildcardSuffix)
                    {
                        nameComparison = TagHelperRequiredAttributeNameComparison.PrefixMatch;

                        // Move past wild card
                        _index++;
                    }
                }

                TagHelperRequiredAttributeDescriptor descriptor = null;
                if (ValidateName(attributeName, targetingAttributes: true, errorSink: errorSink))
                {
                    descriptor = new TagHelperRequiredAttributeDescriptor
                    {
                        Name = attributeName,
                        NameComparison = nameComparison
                    };
                }

                return descriptor;
            }

            private string ParseCssAttributeName(ErrorSink errorSink)
            {
                var nameStartIndex = _index;
                var nameEndIndex = _requiredAttributes.IndexOfAny(InvalidCssAttributeNameCharacters, _index);
                nameEndIndex = nameEndIndex == -1 ? _requiredAttributes.Length : nameEndIndex;
                _index = nameEndIndex;

                var attributeName = _requiredAttributes.Substring(nameStartIndex, nameEndIndex - nameStartIndex);

                return attributeName;
            }

            private TagHelperRequiredAttributeValueComparison? ParseCssValueComparison(ErrorSink errorSink)
            {
                Debug.Assert(!AtEnd);
                TagHelperRequiredAttributeValueComparison valueComparison;

                if (CssValueComparisons.TryGetValue(Current, out valueComparison))
                {
                    var op = Current;
                    _index++;

                    if (op != '=' && At('='))
                    {
                        // Two length operator (ex: ^=). Move past the second piece
                        _index++;
                    }
                    else if (op != '=') // We're at an incomplete operator (ex: [foo^]
                    {
                        errorSink.OnError(
                            SourceLocation.Zero,
                            "invalid",
                            length: 0);
                        return null;
                    }
                }
                else if (!At(']'))
                {
                    errorSink.OnError(
                        SourceLocation.Zero,
                        "invalid",
                        length: 0);
                    return null;
                }

                return valueComparison;
            }

            private string ParseCssValue(ErrorSink errorSink)
            {
                int valueStart;
                int valueEnd;
                if (At('\'') || At('"'))
                {
                    var quote = Current;

                    // Move past the quote
                    _index++;

                    valueStart = _index;
                    valueEnd = _requiredAttributes.IndexOf(quote, _index);
                    if (valueEnd == -1)
                    {
                        errorSink.OnError(
                            SourceLocation.Zero,
                            "invalid",
                            length: 0);
                        return null;
                    }
                    _index = valueEnd + 1;
                }
                else
                {
                    valueStart = _index;
                    var valueEndIndex = _requiredAttributes.IndexOfAny(InvalidCssQuotelessValueCharacters, _index);
                    valueEnd = valueEndIndex == -1 ? _requiredAttributes.Length : valueEndIndex;
                    _index = valueEnd;
                }

                var value = _requiredAttributes.Substring(valueStart, valueEnd - valueStart);

                return value;
            }

            private TagHelperRequiredAttributeDescriptor ParseCssSelector(ErrorSink errorSink)
            {
                Debug.Assert(At('['));

                // Move past '['.
                _index++;
                PassOptionalWhitespace();

                var attributeName = ParseCssAttributeName(errorSink);

                PassOptionalWhitespace();

                if (!EnsureNotAtEnd(errorSink))
                {
                    return null;
                }

                if (!ValidateName(attributeName, targetingAttributes: true, errorSink: errorSink))
                {
                    // Couldn't parse a valid attribute name.
                    return null;
                }

                var valueComparison = ParseCssValueComparison(errorSink);

                if (!valueComparison.HasValue)
                {
                    return null;
                }

                PassOptionalWhitespace();

                if (!EnsureNotAtEnd(errorSink))
                {
                    return null;
                }

                var value = ParseCssValue(errorSink);

                if (value == null)
                {
                    // Couldn't parse value
                    return null;
                }

                PassOptionalWhitespace();

                if (At(']'))
                {
                    // Move past the ending bracket.
                    _index++;
                }
                else if (AtEnd)
                {
                    errorSink.OnError(
                        SourceLocation.Zero,
                        "invalid",
                        length: 0);
                    return null;
                }
                else
                {
                    errorSink.OnError(
                        SourceLocation.Zero,
                        "invalid",
                        length: 0);
                    return null;
                }

                return new TagHelperRequiredAttributeDescriptor
                {
                    Name = attributeName,
                    NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                    Value = value,
                    ValueComparison = valueComparison.Value,
                };
            }

            private bool EnsureNotAtEnd(ErrorSink errorSink)
            {
                if (AtEnd)
                {
                    errorSink.OnError(
                        SourceLocation.Zero,
                        "invalid",
                        length: 0);

                    return false;
                }

                return true;
            }

            private bool At(char c)
            {
                return !AtEnd && Current == c;
            }

            private void PassOptionalWhitespace()
            {
                while (!AtEnd && (Current == ' ' || Current == '\t'))
                {
                    _index++;
                }
            }
        }
    }
}