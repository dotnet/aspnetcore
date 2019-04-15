// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorCodeDocumentExtensionsTest
    {
        [Fact]
        public void GetRazorSyntaxTree_ReturnsSyntaxTree()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorSyntaxTree.Parse(codeDocument.Source);
            codeDocument.Items[typeof(RazorSyntaxTree)] = expected;

            // Act
            var actual = codeDocument.GetSyntaxTree();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetRazorSyntaxTree_SetsSyntaxTree()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorSyntaxTree.Parse(codeDocument.Source);

            // Act
            codeDocument.SetSyntaxTree(expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(RazorSyntaxTree)]);
        }

        [Fact]
        public void GetAndSetImportSyntaxTrees_ReturnsSyntaxTrees()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = new[] { RazorSyntaxTree.Parse(codeDocument.Source), };
            codeDocument.SetImportSyntaxTrees(expected);

            // Act
            var actual = codeDocument.GetImportSyntaxTrees();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void GetAndSetTagHelpers_ReturnsTagHelpers()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = new[] { TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build() };
            codeDocument.SetTagHelpers(expected);

            // Act
            var actual = codeDocument.GetTagHelpers();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void GetIRDocument_ReturnsIRDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = new DocumentIntermediateNode();
            codeDocument.Items[typeof(DocumentIntermediateNode)] = expected;

            // Act
            var actual = codeDocument.GetDocumentIntermediateNode();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetIRDocument_SetsIRDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = new DocumentIntermediateNode();

            // Act
            codeDocument.SetDocumentIntermediateNode((DocumentIntermediateNode)expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(DocumentIntermediateNode)]);
        }

        [Fact]
        public void GetCSharpDocument_ReturnsCSharpDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorCSharpDocument.Create("", RazorCodeGenerationOptions.CreateDefault(), Array.Empty<RazorDiagnostic>());
            codeDocument.Items[typeof(RazorCSharpDocument)] = expected;

            // Act
            var actual = codeDocument.GetCSharpDocument();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetCSharpDocument_SetsCSharpDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorCSharpDocument.Create("", RazorCodeGenerationOptions.CreateDefault(), Array.Empty<RazorDiagnostic>());

            // Act
            codeDocument.SetCSharpDocument(expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(RazorCSharpDocument)]);
        }

        [Fact]
        public void GetTagHelperContext_ReturnsTagHelperContext()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = TagHelperDocumentContext.Create(null, new TagHelperDescriptor[0]);
            codeDocument.Items[typeof(TagHelperDocumentContext)] = expected;

            // Act
            var actual = codeDocument.GetTagHelperContext();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetTagHelperContext_SetsTagHelperContext()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = TagHelperDocumentContext.Create(null, new TagHelperDescriptor[0]);

            // Act
            codeDocument.SetTagHelperContext(expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(TagHelperDocumentContext)]);
        }

        [Fact]
        public void GetParserOptions_ReturnsSuccessfully()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorParserOptions.CreateDefault();
            codeDocument.Items[typeof(RazorParserOptions)] = expected;

            // Act
            var actual = codeDocument.GetParserOptions();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetParserOptions_SetsSuccessfully()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorParserOptions.CreateDefault();

            // Act
            codeDocument.SetParserOptions(expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(RazorParserOptions)]);
        }

        [Fact]
        public void GetCodeGenerationOptions_ReturnsSuccessfully()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorCodeGenerationOptions.CreateDefault();
            codeDocument.Items[typeof(RazorCodeGenerationOptions)] = expected;

            // Act
            var actual = codeDocument.GetCodeGenerationOptions();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetCodeGenerationOptions_SetsSuccessfully()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorCodeGenerationOptions.CreateDefault();

            // Act
            codeDocument.SetCodeGenerationOptions(expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(RazorCodeGenerationOptions)]);
        }

        [Fact]
        public void TryComputeNamespaceAndClass_RootNamespaceNotSet_ReturnsNull()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Test.cshtml", relativePath: "Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());

            // Act
            codeDocument.TryComputeNamespaceAndClass(out var @namespace, out var @class);

            // Assert
            Assert.Null(@namespace);
            Assert.Null(@class);
        }

        [Fact]
        public void TryComputeNamespaceAndClass_RelativePathNull_ReturnsNull()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Test.cshtml", relativePath: null);
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());

            // Act
            codeDocument.TryComputeNamespaceAndClass(out var @namespace, out var @class);

            // Assert
            Assert.Null(@namespace);
            Assert.Null(@class);
        }

        [Fact]
        public void TryComputeNamespaceAndClass_FilePathNull_ReturnsNull()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: null, relativePath: "Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());

            // Act
            codeDocument.TryComputeNamespaceAndClass(out var @namespace, out var @class);

            // Assert
            Assert.Null(@namespace);
            Assert.Null(@class);
        }

        [Fact]
        public void TryComputeNamespaceAndClass_RelativePathLongerThanFilePath_ReturnsNull()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Test.cshtml", relativePath: "Some\\invalid\\relative\\path\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());

            // Act
            codeDocument.TryComputeNamespaceAndClass(out var @namespace, out var @class);

            // Assert
            Assert.Null(@namespace);
            Assert.Null(@class);
        }

        [Fact]
        public void TryComputeNamespaceAndClass_ComputesNamespaceAndClass()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Components\\Test.cshtml", relativePath: "\\Components\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            codeDocument.SetCodeGenerationOptions(RazorCodeGenerationOptions.Create(c =>
            {
                c.RootNamespace = "Hello";
            }));

            // Act
            codeDocument.TryComputeNamespaceAndClass(out var @namespace, out var @class);

            // Assert
            Assert.Equal("Hello.Components", @namespace);
            Assert.Equal("Test", @class);
        }

        [Fact]
        public void TryComputeNamespaceAndClass_UsesIROptions_ComputesNamespaceAndClass()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Components\\Test.cshtml", relativePath: "\\Components\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            var documentNode = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.Create(c =>
                {
                    c.RootNamespace = "Hello";
                })
            };
            codeDocument.SetDocumentIntermediateNode(documentNode);

            // Act
            codeDocument.TryComputeNamespaceAndClass(out var @namespace, out var @class);

            // Assert
            Assert.Equal("Hello.Components", @namespace);
            Assert.Equal("Test", @class);
        }

        [Fact]
        public void TryComputeNamespaceAndClass_PrefersOptionsFromCodeDocument_ComputesNamespaceAndClass()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Components\\Test.cshtml", relativePath: "\\Components\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            codeDocument.SetCodeGenerationOptions(RazorCodeGenerationOptions.Create(c =>
            {
                c.RootNamespace = "World";
            }));
            var documentNode = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.Create(c =>
                {
                    c.RootNamespace = "Hello";
                })
            };
            codeDocument.SetDocumentIntermediateNode(documentNode);

            // Act
            codeDocument.TryComputeNamespaceAndClass(out var @namespace, out var @class);

            // Assert
            Assert.Equal("World.Components", @namespace);
            Assert.Equal("Test", @class);
        }

        [Fact]
        public void TryComputeNamespaceAndClass_SanitizesNamespaceAndClassName()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Components with space\\Test$name.cshtml", relativePath: "\\Components with space\\Test$name.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            var documentNode = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.Create(c =>
                {
                    c.RootNamespace = "Hel?o.World";
                })
            };
            codeDocument.SetDocumentIntermediateNode(documentNode);

            // Act
            codeDocument.TryComputeNamespaceAndClass(out var @namespace, out var @class);

            // Assert
            Assert.Equal("Hel_o.World.Components_with_space", @namespace);
            Assert.Equal("Test_name", @class);
        }
    }
}
