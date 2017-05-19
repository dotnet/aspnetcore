// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class TagHelpersIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void SimpleTagHelpers()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly")
            };

            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }

        [Fact]
        public void TagHelpersWithBoundAttributes()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("bound")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                    })
            };

            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }

        [Fact]
        public void NestedTagHelpers()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "p",
                    typeName: "PTagHelper",
                    assemblyName: "TestAssembly"),
                CreateTagHelperDescriptor(
                    tagName: "form",
                    typeName: "FormTagHelper",
                    assemblyName: "TestAssembly"),
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("value")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                    })
            };

            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BindAttribute(attributeBuilder);
                }
            }

            builder.TagMatchingRule(ruleBuilder => ruleBuilder.RequireTagName(tagName));

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
