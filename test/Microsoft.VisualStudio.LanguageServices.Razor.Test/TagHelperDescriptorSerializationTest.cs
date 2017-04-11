// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
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
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("string"),
                },
                ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                {
                    builder => builder
                        .RequireAttribute(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                        .RequireAttribute(attribute => attribute
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
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("string"),
                },
                ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                {
                    builder => builder
                        .RequireAttribute(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                        .RequireAttribute(attribute => attribute
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
                            new RazorDiagnosticDescriptor("id", () => "Test Message 1", RazorDiagnosticSeverity.Error), new SourceSpan(null, 10, 20, 30, 40)))
                        .AddDiagnostic(RazorDiagnostic.Create(new RazorError("Test Message 2", 10, 20, 30, 40)));
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
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
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
                        .AsDictionary("dict-prefix-", "string"),
                },
                ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                {
                    builder => builder
                        .RequireAttribute(attribute => attribute
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
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<ITagHelperBoundAttributeDescriptorBuilder>> attributes = null,
            IEnumerable<Action<TagMatchingRuleBuilder>> ruleBuilders = null,
            Action<TagHelperDescriptorBuilder> configureAction = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BindAttribute(attributeBuilder);
                }
            }

            if (ruleBuilders != null)
            {
                foreach (var ruleBuilder in ruleBuilders)
                {
                    builder.TagMatchingRule(innerRuleBuilder => {
                        innerRuleBuilder.RequireTagName(tagName);
                        ruleBuilder(innerRuleBuilder);
                    });
                }
            }
            else
            {
                builder.TagMatchingRule(ruleBuilder => ruleBuilder.RequireTagName(tagName));
            }

            configureAction?.Invoke(builder);

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
