// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
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
                    attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
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

            var engine = RazorEngine.Create(b =>
            {
                b.AddTagHelpers(descriptors);
                b.Features.Add(new DefaultInstrumentationPass());
            });

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());

            var csharpDocument = document.GetCSharpDocument();
            AssertCSharpDocumentMatchesBaseline(csharpDocument);
            Assert.Empty(csharpDocument.Diagnostics);
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<ITagHelperBoundAttributeDescriptorBuilder>> attributes = null)
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
