// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X.IntegrationTests
{
    public class InstrumentationPassIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void BasicTest()
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
                            .TypeName("System.String"),      // Gets preallocated
                        builder => builder
                            .Name("date")
                            .PropertyName("BarProp")
                            .TypeName("System.DateTime"),    // Doesn't get preallocated
                    })
            };

            var engine = CreateProjectEngine(b =>
            {
                b.AddTagHelpers(descriptors);
                b.Features.Add(new InstrumentationPass());
                
                // This test includes templates
                b.AddTargetExtension(new TemplateTargetExtension());
            });

            var projectItem = CreateProjectItemFromFile();

            // Act
            var document = engine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(document.GetDocumentIntermediateNode());

            var csharpDocument = document.GetCSharpDocument();
            AssertCSharpDocumentMatchesBaseline(csharpDocument);
            Assert.Empty(csharpDocument.Diagnostics);
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);
            builder.TypeName(typeName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BoundAttributeDescriptor(attributeBuilder);
                }
            }

            builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
