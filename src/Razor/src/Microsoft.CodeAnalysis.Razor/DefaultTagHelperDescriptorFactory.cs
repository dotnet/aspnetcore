// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTagHelperDescriptorFactory
    {
        private const string DataDashPrefix = "data-";
        private const string TagHelperNameEnding = "TagHelper";

        private readonly INamedTypeSymbol _htmlAttributeNameAttributeSymbol;
        private readonly INamedTypeSymbol _htmlAttributeNotBoundAttributeSymbol;
        private readonly INamedTypeSymbol _htmlTargetElementAttributeSymbol;
        private readonly INamedTypeSymbol _outputElementHintAttributeSymbol;
        private readonly INamedTypeSymbol _iDictionarySymbol;
        private readonly INamedTypeSymbol _restrictChildrenAttributeSymbol;
        private readonly INamedTypeSymbol _editorBrowsableAttributeSymbol;

        private static readonly SymbolDisplayFormat FullNameTypeDisplayFormat =
            SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
                .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions & (~SymbolDisplayMiscellaneousOptions.UseSpecialTypes));

        public DefaultTagHelperDescriptorFactory(Compilation compilation, bool includeDocumentation, bool excludeHidden)
        {
            IncludeDocumentation = includeDocumentation;
            ExcludeHidden = excludeHidden;

            _htmlAttributeNameAttributeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.HtmlAttributeNameAttribute);
            _htmlAttributeNotBoundAttributeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.HtmlAttributeNotBoundAttribute);
            _htmlTargetElementAttributeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.HtmlTargetElementAttribute);
            _outputElementHintAttributeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.OutputElementHintAttribute);
            _restrictChildrenAttributeSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.RestrictChildrenAttribute);
            _editorBrowsableAttributeSymbol = compilation.GetTypeByMetadataName(typeof(EditorBrowsableAttribute).FullName);
            _iDictionarySymbol = compilation.GetTypeByMetadataName(TagHelperTypes.IDictionary);
        }

        protected bool ExcludeHidden { get; }

        protected bool IncludeDocumentation { get; }

        /// <inheritdoc />
        public virtual TagHelperDescriptor CreateDescriptor(INamedTypeSymbol type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (ShouldSkipDescriptorCreation(type))
            {
                return null;
            }

            var typeName = GetFullName(type);
            var assemblyName = type.ContainingAssembly.Identity.Name;

            var descriptorBuilder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);
            descriptorBuilder.SetTypeName(typeName);

            AddBoundAttributes(type, descriptorBuilder);
            AddTagMatchingRules(type, descriptorBuilder);
            AddAllowedChildren(type, descriptorBuilder);
            AddDocumentation(type, descriptorBuilder);
            AddTagOutputHint(type, descriptorBuilder);

            var descriptor = descriptorBuilder.Build();

            return descriptor;
        }

        private void AddTagMatchingRules(INamedTypeSymbol type, TagHelperDescriptorBuilder descriptorBuilder)
        {
            var targetElementAttributes = type
                .GetAttributes()
                .Where(attribute => attribute.AttributeClass == _htmlTargetElementAttributeSymbol);

            // If there isn't an attribute specifying the tag name derive it from the name
            if (!targetElementAttributes.Any())
            {
                var name = type.Name;

                if (name.EndsWith(TagHelperNameEnding, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - TagHelperNameEnding.Length);
                }

                descriptorBuilder.TagMatchingRule(ruleBuilder =>
                {
                    var htmlCasedName = HtmlConventions.ToHtmlCase(name);
                    ruleBuilder.TagName = htmlCasedName;
                });

                return;
            }

            foreach (var targetElementAttribute in targetElementAttributes)
            {
                descriptorBuilder.TagMatchingRule(ruleBuilder =>
                {
                    var tagName = HtmlTargetElementAttribute_Tag(targetElementAttribute);
                    ruleBuilder.TagName = tagName;

                    var parentTag = HtmlTargetElementAttribute_ParentTag(targetElementAttribute);
                    ruleBuilder.ParentTag = parentTag;

                    var tagStructure = HtmlTargetElementAttribute_TagStructure(targetElementAttribute);
                    ruleBuilder.TagStructure = tagStructure;

                    var requiredAttributeString = HtmlTargetElementAttribute_Attributes(targetElementAttribute);
                    RequiredAttributeParser.AddRequiredAttributes(requiredAttributeString, ruleBuilder);
                });
            }
        }

        private void AddBoundAttributes(INamedTypeSymbol type, TagHelperDescriptorBuilder builder)
        {
            var accessibleProperties = GetAccessibleProperties(type);
            foreach (var property in accessibleProperties)
            {
                if (ShouldSkipDescriptorCreation(property))
                {
                    continue;
                }

                builder.BindAttribute(attributeBuilder =>
                {
                    ConfigureBoundAttribute(attributeBuilder, property, type);
                });
            }
        }

        private void AddAllowedChildren(INamedTypeSymbol type, TagHelperDescriptorBuilder builder)
        {
            var restrictChildrenAttribute = type.GetAttributes().Where(a => a.AttributeClass == _restrictChildrenAttributeSymbol).FirstOrDefault();
            if (restrictChildrenAttribute == null)
            {
                return;
            }

            builder.AllowChildTag(childTagBuilder => childTagBuilder.Name = (string)restrictChildrenAttribute.ConstructorArguments[0].Value);

            if (restrictChildrenAttribute.ConstructorArguments.Length == 2)
            {
                foreach (var value in restrictChildrenAttribute.ConstructorArguments[1].Values)
                {
                    builder.AllowChildTag(childTagBuilder => childTagBuilder.Name = (string)value.Value);
                }
            }
        }

        private void AddDocumentation(INamedTypeSymbol type, TagHelperDescriptorBuilder builder)
        {
            if (!IncludeDocumentation)
            {
                return;
            }

            var xml = type.GetDocumentationCommentXml();

            if (!string.IsNullOrEmpty(xml))
            {
                builder.Documentation = xml;
            }
        }

        private void AddTagOutputHint(INamedTypeSymbol type, TagHelperDescriptorBuilder builder)
        {
            string outputElementHint = null;
            var outputElementHintAttribute = type.GetAttributes().Where(a => a.AttributeClass == _outputElementHintAttributeSymbol).FirstOrDefault();
            if (outputElementHintAttribute != null)
            {
                outputElementHint = (string)(outputElementHintAttribute.ConstructorArguments[0]).Value;
                builder.TagOutputHint = outputElementHint;
            }
        }

        private void ConfigureBoundAttribute(
            BoundAttributeDescriptorBuilder builder,
            IPropertySymbol property,
            INamedTypeSymbol containingType)
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
                attributeName = HtmlConventions.ToHtmlCase(property.Name);
            }
            else
            {
                hasExplicitName = true;
                attributeName = (string)attributeNameAttribute.ConstructorArguments[0].Value;
            }

            var hasPublicSetter = property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public;
            var typeName = GetFullName(property.Type);
            builder.TypeName = typeName;
            builder.SetPropertyName(property.Name);

            if (hasPublicSetter)
            {
                builder.Name = attributeName;

                if (property.Type.TypeKind == TypeKind.Enum)
                {
                    builder.IsEnum = true;
                }

                if (IncludeDocumentation)
                {
                    var xml = property.GetDocumentationCommentXml();

                    if (!string.IsNullOrEmpty(xml))
                    {
                        builder.Documentation = xml;
                    }
                }
            }
            else if (hasExplicitName && !IsPotentialDictionaryProperty(property))
            {
                // Specified HtmlAttributeNameAttribute.Name though property has no public setter.
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidAttributeNameNullOrEmpty(GetFullName(containingType), property.Name);
                builder.Diagnostics.Add(diagnostic);
            }

            ConfigureDictionaryBoundAttribute(builder, property, containingType, attributeNameAttribute, attributeName, hasPublicSetter);
        }

        private void ConfigureDictionaryBoundAttribute(
            BoundAttributeDescriptorBuilder builder,
            IPropertySymbol property,
            INamedTypeSymbol containingType,
            AttributeData attributeNameAttribute,
            string attributeName,
            bool hasPublicSetter)
        {
            string dictionaryAttributePrefix = null;
            var dictionaryAttributePrefixSet = false;

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

            var dictionaryArgumentTypes = GetDictionaryArgumentTypes(property);
            if (dictionaryArgumentTypes != null)
            {
                var prefix = dictionaryAttributePrefix;
                if (attributeNameAttribute == null || !dictionaryAttributePrefixSet)
                {
                    prefix = attributeName + "-";
                }

                if (prefix != null)
                {
                    var dictionaryValueType = dictionaryArgumentTypes[1];
                    var dictionaryValueTypeName = GetFullName(dictionaryValueType);
                    builder.AsDictionary(prefix, dictionaryValueTypeName);
                }
            }

            var dictionaryKeyType = dictionaryArgumentTypes?[0];

            if (dictionaryKeyType?.SpecialType != SpecialType.System_String)
            {
                if (dictionaryAttributePrefix != null)
                {
                    // DictionaryAttributePrefix is not supported unless associated with an
                    // IDictionary<string, TValue> property.
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNotNull(GetFullName(containingType), property.Name);
                    builder.Diagnostics.Add(diagnostic);
                }

                return;
            }
            else if (!hasPublicSetter && attributeNameAttribute != null && !dictionaryAttributePrefixSet)
            {
                // Must set DictionaryAttributePrefix when using HtmlAttributeNameAttribute with a dictionary property
                // that lacks a public setter.
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidAttributePrefixNull(GetFullName(containingType), property.Name);
                builder.Diagnostics.Add(diagnostic);

                return;
            }
        }

        private IReadOnlyList<ITypeSymbol> GetDictionaryArgumentTypes(IPropertySymbol property)
        {
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

            return dictionaryType?.TypeArguments;
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
                return TagHelperMatchingConventions.ElementCatchAllName;
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

        private bool IsPotentialDictionaryProperty(IPropertySymbol property)
        {
            return
                ((property.Type as INamedTypeSymbol)?.ConstructedFrom == _iDictionarySymbol || property.Type.AllInterfaces.Any(s => s.ConstructedFrom == _iDictionarySymbol)) &&
                GetDictionaryArgumentTypes(property)?[0].SpecialType == SpecialType.System_String;
        }

        private IEnumerable<IPropertySymbol> GetAccessibleProperties(INamedTypeSymbol typeSymbol)
        {
            var accessibleProperties = new Dictionary<string, IPropertySymbol>(StringComparer.Ordinal);
            do
            {
                var members = typeSymbol.GetMembers();
                for (var i = 0; i < members.Length; i++)
                {
                    var property = members[i] as IPropertySymbol;
                    if (property != null &&
                        property.Parameters.Length == 0 &&
                        property.GetMethod != null &&
                        property.GetMethod.DeclaredAccessibility == Accessibility.Public &&
                        property.GetAttributes().Where(a => a.AttributeClass == _htmlAttributeNotBoundAttributeSymbol).FirstOrDefault() == null &&
                        (property.GetAttributes().Any(a => a.AttributeClass == _htmlAttributeNameAttributeSymbol) ||
                        property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public ||
                        IsPotentialDictionaryProperty(property)) &&
                        !accessibleProperties.ContainsKey(property.Name))
                    {
                        accessibleProperties.Add(property.Name, property);
                    }
                }

                typeSymbol = typeSymbol.BaseType;
            }
            while (typeSymbol != null);

            return accessibleProperties.Values;
        }

        private bool ShouldSkipDescriptorCreation(ISymbol symbol)
        {
            if (ExcludeHidden)
            {
                var editorBrowsableAttribute = symbol.GetAttributes().Where(a => a.AttributeClass == _editorBrowsableAttributeSymbol).FirstOrDefault();

                if (editorBrowsableAttribute == null)
                {
                    return false;
                }

                if (editorBrowsableAttribute.ConstructorArguments.Length > 0)
                {
                    return (EditorBrowsableState)editorBrowsableAttribute.ConstructorArguments[0].Value == EditorBrowsableState.Never;
                }
            }

            return false;
        }

        private static string GetFullName(ITypeSymbol type) => type.ToDisplayString(FullNameTypeDisplayFormat);
    }
}