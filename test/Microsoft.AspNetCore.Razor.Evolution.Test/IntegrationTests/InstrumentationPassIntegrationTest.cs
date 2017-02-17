// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
{
    public class InstrumentationPassIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void BasicTest()
        {
            // Arrange
            var descriptors = new[]
            {
                new TagHelperDescriptor
                {
                    TagName = "p",
                    TypeName = "PTagHelper",
                    AssemblyName = "TestAssembly",
                },
                new TagHelperDescriptor
                {
                    TagName = "form",
                    TypeName = "FormTagHelper",
                    AssemblyName = "TestAssembly",
                },
                new TagHelperDescriptor
                {
                    TagName = "input",
                    TypeName = "InputTagHelper",
                    AssemblyName = "TestAssembly",
                    Attributes = new[] 
                    {
                        new TagHelperAttributeDescriptor
                        {
                            Name = "value",
                            PropertyName = "FooProp",
                            TypeName = "System.String"      // Gets preallocated
                        },
                        new TagHelperAttributeDescriptor
                        {
                            Name = "date",
                            PropertyName = "BarProp",
                            TypeName = "System.DateTime"   // Doesn't get preallocated
                        }
                    }
                }
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
    }
}
