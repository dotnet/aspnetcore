// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Extensions;
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
        public void TryComputeNamespace_RootNamespaceNotSet_ReturnsNull()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Test.cshtml", relativePath: "Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Null(@namespace);
        }

        [Fact]
        public void TryComputeNamespace_RelativePathNull_ReturnsNull()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Test.cshtml", relativePath: null);
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Null(@namespace);
        }

        [Fact]
        public void TryComputeNamespace_FilePathNull_ReturnsNull()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: null, relativePath: "Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Null(@namespace);
        }

        [Fact]
        public void TryComputeNamespace_RelativePathLongerThanFilePath_ReturnsNull()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Test.cshtml", relativePath: "Some\\invalid\\relative\\path\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Null(@namespace);
        }

        [Fact]
        public void TryComputeNamespace_ComputesNamespace()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(filePath: "C:\\Hello\\Components\\Test.cshtml", relativePath: "\\Components\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            codeDocument.SetCodeGenerationOptions(RazorCodeGenerationOptions.Create(c =>
            {
                c.RootNamespace = "Hello";
            }));

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal("Hello.Components", @namespace);
        }

        [Fact]
        public void TryComputeNamespace_UsesIROptions_ComputesNamespace()
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
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal("Hello.Components", @namespace);
        }

        [Fact]
        public void TryComputeNamespace_NoRootNamespaceFallback_ReturnsNull()
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
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: false, out var @namespace);

            // Assert
            Assert.Null(@namespace);
        }

        [Fact]
        public void TryComputeNamespace_PrefersOptionsFromCodeDocument_ComputesNamespace()
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
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal("World.Components", @namespace);
        }

        [Fact]
        public void TryComputeNamespace_SanitizesNamespaceName()
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
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal("Hel_o.World.Components_with_space", @namespace);
        }

        [Fact]
        public void TryComputeNamespace_RespectsNamespaceDirective()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(
                content: "@namespace My.Custom.NS",
                filePath: "C:\\Hello\\Components\\Test.cshtml",
                relativePath: "\\Components\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            codeDocument.SetFileKind(FileKinds.Component);
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(sourceDocument, RazorParserOptions.Create(options =>
            {
                options.Directives.Add(NamespaceDirective.Directive);
            })));

            var documentNode = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.Create(c =>
                {
                    c.RootNamespace = "Hello.World";
                })
            };
            codeDocument.SetDocumentIntermediateNode(documentNode);

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal("My.Custom.NS", @namespace);
        }

        [Fact]
        public void TryComputeNamespace_RespectsImportsNamespaceDirective()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(
                filePath: "C:\\Hello\\Components\\Test.cshtml",
                relativePath: "\\Components\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            codeDocument.SetFileKind(FileKinds.Component);
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(sourceDocument, RazorParserOptions.Create(options =>
            {
                options.Directives.Add(NamespaceDirective.Directive);
            })));

            var importSourceDocument = TestRazorSourceDocument.Create(
                content: "@namespace My.Custom.NS",
                filePath: "C:\\Hello\\_Imports.razor",
                relativePath: "\\_Imports.razor");
            codeDocument.SetImportSyntaxTrees(new[]
            {
                RazorSyntaxTree.Parse(importSourceDocument, RazorParserOptions.Create(options =>
                {
                    options.Directives.Add(NamespaceDirective.Directive);
                }))
            });

            var documentNode = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.Create(c =>
                {
                    c.RootNamespace = "Hello.World";
                })
            };
            codeDocument.SetDocumentIntermediateNode(documentNode);

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal("My.Custom.NS.Components", @namespace);
        }

        [Fact]
        public void TryComputeNamespace_RespectsImportsNamespaceDirective_SameFolder()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(
                filePath: "C:\\Hello\\Components\\Test.cshtml",
                relativePath: "\\Components\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            codeDocument.SetFileKind(FileKinds.Component);
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(sourceDocument, RazorParserOptions.Create(options =>
            {
                options.Directives.Add(NamespaceDirective.Directive);
            })));

            var importSourceDocument = TestRazorSourceDocument.Create(
                content: "@namespace My.Custom.NS",
                filePath: "C:\\Hello\\Components\\_Imports.razor",
                relativePath: "\\Components\\_Imports.razor");
            codeDocument.SetImportSyntaxTrees(new[]
            {
                RazorSyntaxTree.Parse(importSourceDocument, RazorParserOptions.Create(options =>
                {
                    options.Directives.Add(NamespaceDirective.Directive);
                }))
            });

            var documentNode = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.Create(c =>
                {
                    c.RootNamespace = "Hello.World";
                })
            };
            codeDocument.SetDocumentIntermediateNode(documentNode);

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal("My.Custom.NS", @namespace);
        }

        [Fact]
        public void TryComputeNamespace_OverrideImportsNamespaceDirective()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(
                content: "@namespace My.Custom.OverrideNS",
                filePath: "C:\\Hello\\Components\\Test.cshtml",
                relativePath: "\\Components\\Test.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            codeDocument.SetFileKind(FileKinds.Component);
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(sourceDocument, RazorParserOptions.Create(options =>
            {
                options.Directives.Add(NamespaceDirective.Directive);
            })));

            var importSourceDocument = TestRazorSourceDocument.Create(
                content: "@namespace My.Custom.NS",
                filePath: "C:\\Hello\\_Imports.razor",
                relativePath: "\\_Imports.razor");
            codeDocument.SetImportSyntaxTrees(new[]
            {
                RazorSyntaxTree.Parse(importSourceDocument, RazorParserOptions.Create(options =>
                {
                    options.Directives.Add(NamespaceDirective.Directive);
                }))
            });

            var documentNode = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.Create(c =>
                {
                    c.RootNamespace = "Hello.World";
                })
            };
            codeDocument.SetDocumentIntermediateNode(documentNode);

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal("My.Custom.OverrideNS", @namespace);
        }

        [Theory]
        [InlineData("/", "foo.cshtml", "Base")]
        [InlineData("/", "foo/bar.cshtml", "Base.foo")]
        [InlineData("/", "foo/bar/baz.cshtml", "Base.foo.bar")]
        [InlineData("/foo/", "bar/baz.cshtml", "Base.bar")]
        [InlineData("/Foo/", "bar/baz.cshtml", "Base.bar")]
        [InlineData("c:\\", "foo.cshtml", "Base")]
        [InlineData("c:\\", "foo\\bar.cshtml", "Base.foo")]
        [InlineData("c:\\", "foo\\bar\\baz.cshtml", "Base.foo.bar")]
        [InlineData("c:\\foo\\", "bar\\baz.cshtml", "Base.bar")]
        [InlineData("c:\\Foo\\", "bar\\baz.cshtml", "Base.bar")]
        public void TryComputeNamespace_ComputesNamespaceWithSuffix(string basePath, string relativePath, string expectedNamespace)
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(
                filePath: Path.Combine(basePath, relativePath),
                relativePath: relativePath);
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(sourceDocument, RazorParserOptions.Create(options =>
            {
                options.Directives.Add(NamespaceDirective.Directive);
            })));

            var importRelativePath = "_ViewImports.cshtml";
            var importSourceDocument = TestRazorSourceDocument.Create(
                content: "@namespace Base",
                filePath: Path.Combine(basePath, importRelativePath),
                relativePath: importRelativePath);
            codeDocument.SetImportSyntaxTrees(new[]
            {
                RazorSyntaxTree.Parse(importSourceDocument, RazorParserOptions.Create(options =>
                {
                    options.Directives.Add(NamespaceDirective.Directive);
                }))
            });

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal(expectedNamespace, @namespace);
        }

        [Fact]
        public void TryComputeNamespace_ForNonRelatedFiles_UsesNamespaceVerbatim()
        {
            // Arrange
            var sourceDocument = TestRazorSourceDocument.Create(
                filePath: "c:\\foo\\bar\\bleh.cshtml",
                relativePath: "bar\\bleh.cshtml");
            var codeDocument = TestRazorCodeDocument.Create(sourceDocument, Array.Empty<RazorSourceDocument>());
            codeDocument.SetSyntaxTree(RazorSyntaxTree.Parse(sourceDocument, RazorParserOptions.Create(options =>
            {
                options.Directives.Add(NamespaceDirective.Directive);
            })));

            var importSourceDocument = TestRazorSourceDocument.Create(
                content: "@namespace Base",
                filePath: "c:\\foo\\baz\\bleh.cshtml",
                relativePath: "baz\\bleh.cshtml");
            codeDocument.SetImportSyntaxTrees(new[]
            {
                RazorSyntaxTree.Parse(importSourceDocument, RazorParserOptions.Create(options =>
                {
                    options.Directives.Add(NamespaceDirective.Directive);
                }))
            });

            // Act
            codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var @namespace);

            // Assert
            Assert.Equal("Base", @namespace);
        }
    }
}
