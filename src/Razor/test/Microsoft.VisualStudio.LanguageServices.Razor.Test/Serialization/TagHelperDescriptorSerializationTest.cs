// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.LanguageServices.Razor.Serialization;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class TagHelperDescriptorSerializationTest
    {
        [Fact]
        public void TagHelperDescriptor_RoundTripsProperly()
        {
            // Arrange
            var expectedDescriptor = CreateTagHelperDescriptor(
                kind: TagHelperConventions.DefaultKind,
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("string"),
                },
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    builder => builder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-two")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("something")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch))
                        .RequireParentTag("parent-name")
                        .RequireTagStructure(TagStructure.WithoutEndTag),
                },
                configureAction: builder =>
                {
                    builder.AllowChildTag("allowed-child-one");
                    builder.AddMetadata("foo", "bar");
                });

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(expectedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void ViewComponentTagHelperDescriptor_RoundTripsProperly()
        {
            // Arrange
            var expectedDescriptor = CreateTagHelperDescriptor(
                kind: ViewComponentTagHelperConventions.Kind,
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("string"),
                },
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    builder => builder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-two")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("something")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch))
                        .RequireParentTag("parent-name")
                        .RequireTagStructure(TagStructure.WithoutEndTag),
                },
                configureAction: builder =>
                {
                    builder.AllowChildTag("allowed-child-one");
                    builder.AddMetadata("foo", "bar");
                });

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(expectedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void TagHelperDescriptor_WithDiagnostic_RoundTripsProperly()
        {
            // Arrange
            var expectedDescriptor = CreateTagHelperDescriptor(
                kind: TagHelperConventions.DefaultKind,
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("string"),
                },
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    builder => builder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-two")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("something")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch))
                        .RequireParentTag("parent-name"),
                },
                configureAction: builder =>
                {
                    builder.AllowChildTag("allowed-child-one")
                        .AddMetadata("foo", "bar")
                        .AddDiagnostic(RazorDiagnostic.Create(
                            new RazorDiagnosticDescriptor("id", () => "Test Message", RazorDiagnosticSeverity.Error), new SourceSpan(null, 10, 20, 30, 40)));
                });

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(expectedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void TagHelperDescriptor_WithIndexerAttributes_RoundTripsProperly()
        {
            // Arrange
            var expectedDescriptor = CreateTagHelperDescriptor(
                kind: TagHelperConventions.DefaultKind,
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("SomeEnum")
                        .AsEnum()
                        .Documentation("Summary"),
                    builder => builder
                        .Name("test-attribute2")
                        .PropertyName("TestAttribute2")
                        .TypeName("SomeDictionary")
                        .AsDictionaryAttribute("dict-prefix-", "string"),
                },
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    builder => builder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                },
                configureAction: builder =>
                {
                    builder
                        .AllowChildTag("allowed-child-one")
                        .AddMetadata("foo", "bar")
                        .TagOutputHint("Hint");
                });

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(expectedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.Default);
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string kind,
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null,
            IEnumerable<Action<TagMatchingRuleDescriptorBuilder>> ruleBuilders = null,
            Action<TagHelperDescriptorBuilder> configureAction = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(kind, typeName, assemblyName);
            builder.SetTypeName(typeName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BoundAttributeDescriptor(attributeBuilder);
                }
            }

            if (ruleBuilders != null)
            {
                foreach (var ruleBuilder in ruleBuilders)
                {
                    builder.TagMatchingRuleDescriptor(innerRuleBuilder => {
                        innerRuleBuilder.RequireTagName(tagName);
                        ruleBuilder(innerRuleBuilder);
                    });
                }
            }
            else
            {
                builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));
            }

            configureAction?.Invoke(builder);

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
