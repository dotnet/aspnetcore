// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class MetadataAttributePassTest
    {
        [Fact]
        public void Execute_NullCodeGenerationOptions_Noops()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new MetadataAttributePass()
            {
                Engine = engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = new DocumentIntermediateNode();

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            NoChildren(irDocument);
        }

        [Fact]
        public void Execute_SuppressMetadataAttributes_Noops()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new MetadataAttributePass()
            {
                Engine = engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.Create(o =>
                {
                    o.SuppressMetadataAttributes = true;
                }),
            };

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            NoChildren(irDocument);
        }

        [Fact]
        public void Execute_NoNamespaceSet_Noops()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new MetadataAttributePass()
            {
                Engine = engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = new DocumentIntermediateNode()
            {
                DocumentKind = "test",
                Options = RazorCodeGenerationOptions.Create((o) => { }),
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace,
                },
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
                ClassName = "Test",
            };
            builder.Add(@class);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            SingleChild<NamespaceDeclarationIntermediateNode>(irDocument);
        }

        [Fact]
        public void Execute_NoClassNameSet_Noops()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new MetadataAttributePass()
            {
                Engine = engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = new DocumentIntermediateNode()
            {
                DocumentKind = "test",
                Options = RazorCodeGenerationOptions.Create((o) => { }),
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace,
                },
                Content = "Some.Namespace"
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
            };
            builder.Add(@class);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            SingleChild<NamespaceDeclarationIntermediateNode>(irDocument);
        }

        [Fact]
        public void Execute_NoDocumentKind_Noops()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new MetadataAttributePass()
            {
                Engine = engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = new DocumentIntermediateNode();
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace,
                },
                Content = "Some.Namespace"
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
                ClassName = "Test",
            };
            builder.Add(@class);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            SingleChild<NamespaceDeclarationIntermediateNode>(irDocument);
        }

        [Fact]
        public void Execute_NoIdentifier_Noops()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new MetadataAttributePass()
            {
                Engine = engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create("", new RazorSourceDocumentProperties(null, null));
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = new DocumentIntermediateNode()
            {
                DocumentKind = "test",
                Options = RazorCodeGenerationOptions.Create((o) => { }),
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace,
                },
                Content = "Some.Namespace"
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
                ClassName = "Test",
            };
            builder.Add(@class);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            SingleChild<NamespaceDeclarationIntermediateNode>(irDocument);
        }

        [Fact]
        public void Execute_HasRequiredInfo_AddsItemAndSourceChecksum()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new MetadataAttributePass()
            {
                Engine = engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create("", new RazorSourceDocumentProperties(null, "Foo\\Bar.cshtml"));
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = new DocumentIntermediateNode()
            {
                DocumentKind = "test",
                Options = RazorCodeGenerationOptions.Create((o) => { }),
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace,
                },
                Content = "Some.Namespace"
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
                ClassName = "Test",
            };
            builder.Add(@class);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Assert.Equal(2, irDocument.Children.Count);

            var item = Assert.IsType<RazorCompiledItemAttributeIntermediateNode>(irDocument.Children[0]);
            Assert.Equal("/Foo/Bar.cshtml", item.Identifier);
            Assert.Equal("test", item.Kind);
            Assert.Equal("Some.Namespace.Test", item.TypeName);

            Assert.Equal(2, @namespace.Children.Count);
            var checksum = Assert.IsType<RazorSourceChecksumAttributeIntermediateNode>(@namespace.Children[0]);
            Assert.NotNull(checksum.Checksum); // Not verifying the checksum here
            Assert.Equal("SHA1", checksum.ChecksumAlgorithm);
            Assert.Equal("/Foo/Bar.cshtml", checksum.Identifier);
        }

        [Fact]
        public void Execute_HasRequiredInfo_AndImport_AddsItemAndSourceChecksum()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new MetadataAttributePass()
            {
                Engine = engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create("", new RazorSourceDocumentProperties(null, "Foo\\Bar.cshtml"));
            var import = TestRazorSourceDocument.Create("@using System", new RazorSourceDocumentProperties(null, "Foo\\Import.cshtml"));
            var codeDocument = RazorCodeDocument.Create(sourceDocument, new[] { import, });

            var irDocument = new DocumentIntermediateNode()
            {
                DocumentKind = "test",
                Options = RazorCodeGenerationOptions.Create((o) => { }),
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace,
                },
                Content = "Some.Namespace"
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
                ClassName = "Test",
            };
            builder.Add(@class);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Assert.Equal(2, irDocument.Children.Count);

            var item = Assert.IsType<RazorCompiledItemAttributeIntermediateNode>(irDocument.Children[0]);
            Assert.Equal("/Foo/Bar.cshtml", item.Identifier);
            Assert.Equal("test", item.Kind);
            Assert.Equal("Some.Namespace.Test", item.TypeName);

            Assert.Equal(3, @namespace.Children.Count);
            var checksum = Assert.IsType<RazorSourceChecksumAttributeIntermediateNode>(@namespace.Children[0]);
            Assert.NotNull(checksum.Checksum); // Not verifying the checksum here
            Assert.Equal("SHA1", checksum.ChecksumAlgorithm);
            Assert.Equal("/Foo/Bar.cshtml", checksum.Identifier);

            checksum = Assert.IsType<RazorSourceChecksumAttributeIntermediateNode>(@namespace.Children[1]);
            Assert.NotNull(checksum.Checksum); // Not verifying the checksum here
            Assert.Equal("SHA1", checksum.ChecksumAlgorithm);
            Assert.Equal("/Foo/Import.cshtml", checksum.Identifier);
        }

        private static RazorEngine CreateEngine()
        {
            return RazorEngine.Create(b =>
            {
                b.Features.Add(new DefaultMetadataIdentifierFeature());
            });
        }
    }
}
