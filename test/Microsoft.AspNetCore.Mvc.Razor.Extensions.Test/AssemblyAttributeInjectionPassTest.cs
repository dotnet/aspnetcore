// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class AssemblyAttributeInjectionPassTest
    {
        [Fact]
        public void Execute_NoOps_IfNamespaceNodeIsMissing()
        {
            // Arrange
            var irDocument = new DocumentIntermediateNode();
            var pass = new AssemblyAttributeInjectionPass
            {
                Engine = RazorEngine.Create(),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Empty(irDocument.Children);
        }

        [Fact]
        public void Execute_NoOps_IfNamespaceNodeHasEmptyContent()
        {
            // Arrange
            var irDocument = new DocumentIntermediateNode();
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode() { Content = string.Empty };
            @namespace.Annotations[CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace;
            builder.Push(@namespace);

            var pass = new AssemblyAttributeInjectionPass
            {
                Engine = RazorEngine.Create(),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Collection(irDocument.Children,
                node => Assert.Same(@namespace, node));
        }

        [Fact]
        public void Execute_NoOps_IfClassNameNodeIsMissing()
        {
            // Arrange
            var irDocument = new DocumentIntermediateNode();
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode() { Content = "SomeNamespace" };
            builder.Push(@namespace);

            var pass = new AssemblyAttributeInjectionPass
            {
                Engine = RazorEngine.Create(),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Collection(irDocument.Children,
                node => Assert.Same(@namespace, node));
        }

        [Fact]
        public void Execute_NoOps_IfClassNameIsEmpty()
        {
            // Arrange
            var irDocument = new DocumentIntermediateNode();
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Content = "SomeNamespace",
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
            };
            builder.Add(@class);

            var pass = new AssemblyAttributeInjectionPass
            {
                Engine = RazorEngine.Create(),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Collection(irDocument.Children,
                node => Assert.Same(@namespace, node));
        }

        [Fact]
        public void Execute_NoOps_IfDocumentIsNotViewOrPage()
        {
            // Arrange
            var irDocument = new DocumentIntermediateNode
            {
                DocumentKind = "Default",
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode() { Content = "SomeNamespace" };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                ClassName = "SomeName",
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
            };
            builder.Add(@class);

            var pass = new AssemblyAttributeInjectionPass
            {
                Engine = RazorEngine.Create(),
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Collection(irDocument.Children,
                node => Assert.Same(@namespace, node));
        }

        [Fact]
        public void Execute_AddsRazorViewAttribute_ToViews()
        {
            // Arrange
            var expectedAttribute = "[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@\"/Views/Index.cshtml\", typeof(SomeNamespace.SomeName))]";
            var irDocument = new DocumentIntermediateNode
            {
                DocumentKind = MvcViewDocumentClassifierPass.MvcViewDocumentKind,
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Content = "SomeNamespace",
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace
                },
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                ClassName = "SomeName",
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
            };
            builder.Add(@class);

            var pass = new AssemblyAttributeInjectionPass
            {
                Engine = RazorEngine.Create(),
            };
            var document = TestRazorCodeDocument.CreateEmpty();
            document.SetRelativePath("/Views/Index.cshtml");

            // Act
            pass.Execute(document, irDocument);

            // Assert
            Assert.Collection(irDocument.Children,
                node =>
                {
                    var csharpCode = Assert.IsType<CSharpCodeIntermediateNode>(node);
                    var token = Assert.IsType<IntermediateToken>(Assert.Single(csharpCode.Children));
                    Assert.Equal(TokenKind.CSharp, token.Kind);
                    Assert.Equal(expectedAttribute, token.Content);
                },
                node => Assert.Same(@namespace, node));
        }

        [Fact]
        public void Execute_EscapesViewPathWhenAddingAttributeToViews()
        {
            // Arrange
            var expectedAttribute = "[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@\"\\test\\\"\"Index.cshtml\", typeof(SomeNamespace.SomeName))]";
            var irDocument = new DocumentIntermediateNode
            {
                DocumentKind = MvcViewDocumentClassifierPass.MvcViewDocumentKind,
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Content = "SomeNamespace",
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace
                },
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                ClassName = "SomeName",
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
            };
            builder.Add(@class);

            var pass = new AssemblyAttributeInjectionPass
            {
                Engine = RazorEngine.Create(),
            };
            var document = TestRazorCodeDocument.CreateEmpty();
            document.SetRelativePath("\\test\\\"Index.cshtml");

            // Act
            pass.Execute(document, irDocument);

            // Assert
            Assert.Collection(irDocument.Children,
                node =>
                {
                    var csharpCode = Assert.IsType<CSharpCodeIntermediateNode>(node);
                    var token = Assert.IsType<IntermediateToken>(Assert.Single(csharpCode.Children));
                    Assert.Equal(TokenKind.CSharp, token.Kind);
                    Assert.Equal(expectedAttribute, token.Content);
                },
                node => Assert.Same(@namespace, node));
        }

        [Fact]
        public void Execute_AddsRazorPagettribute_ToPage()
        {
            // Arrange
            var expectedAttribute = "[assembly:global::Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.RazorPageAttribute(@\"/Views/Index.cshtml\", typeof(SomeNamespace.SomeName), null)]";
            var irDocument = new DocumentIntermediateNode
            {
                DocumentKind = RazorPageDocumentClassifierPass.RazorPageDocumentKind,
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var pageDirective = new DirectiveIntermediateNode
            {
                Directive = PageDirective.Directive,
            };
            builder.Add(pageDirective);

            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Content = "SomeNamespace",
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace
                },
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                ClassName = "SomeName",
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
            };
            builder.Add(@class);

            var pass = new AssemblyAttributeInjectionPass
            {
                Engine = RazorEngine.Create(),
            };
            var document = TestRazorCodeDocument.CreateEmpty();
            document.SetRelativePath("/Views/Index.cshtml");

            // Act
            pass.Execute(document, irDocument);

            // Assert
            Assert.Collection(irDocument.Children,
                node => Assert.Same(pageDirective, node),
                node =>
                {
                    var csharpCode = Assert.IsType<CSharpCodeIntermediateNode>(node);
                    var token = Assert.IsType<IntermediateToken>(Assert.Single(csharpCode.Children));
                    Assert.Equal(TokenKind.CSharp, token.Kind);
                    Assert.Equal(expectedAttribute, token.Content);
                },
                node => Assert.Same(@namespace, node));
        }

        [Fact]
        public void Execute_EscapesViewPathAndRouteWhenAddingAttributeToPage()
        {
            // Arrange
            var expectedAttribute = "[assembly:global::Microsoft.AspNetCore.Mvc.Razor.Compilation.RazorViewAttribute(@\"\\test\\\"\"Index.cshtml\", typeof(SomeNamespace.SomeName))]";
            var irDocument = new DocumentIntermediateNode
            {
                DocumentKind = MvcViewDocumentClassifierPass.MvcViewDocumentKind,
            };
            var builder = IntermediateNodeBuilder.Create(irDocument);
            var @namespace = new NamespaceDeclarationIntermediateNode
            {
                Content = "SomeNamespace",
                Annotations =
                {
                    [CommonAnnotations.PrimaryNamespace] = CommonAnnotations.PrimaryNamespace
                },
            };
            builder.Push(@namespace);
            var @class = new ClassDeclarationIntermediateNode
            {
                ClassName = "SomeName",
                Annotations =
                {
                    [CommonAnnotations.PrimaryClass] = CommonAnnotations.PrimaryClass,
                },
            };

            builder.Add(@class);

            var pass = new AssemblyAttributeInjectionPass
            {
                Engine = RazorEngine.Create(),
            };
            var document = TestRazorCodeDocument.CreateEmpty();
            document.SetRelativePath("\\test\\\"Index.cshtml");

            // Act
            pass.Execute(document, irDocument);

            // Assert
            Assert.Collection(irDocument.Children,
                node =>
                {
                    var csharpCode = Assert.IsType<CSharpCodeIntermediateNode>(node);
                    var token = Assert.IsType<IntermediateToken>(Assert.Single(csharpCode.Children));
                    Assert.Equal(TokenKind.CSharp, token.Kind);
                    Assert.Equal(expectedAttribute, token.Content);
                },
                node => Assert.Same(@namespace, node));
        }

        private RazorEngine CreateEngine()
        {
            return RazorEngine.Create(b =>
            {
                // Notice we're not registering the InjectDirective.Pass here so we can run it on demand.
                b.Features.Add(new AssemblyAttributeInjectionPass());
            });
        }

        private DocumentIntermediateNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorDocumentClassifierPhase)
                {
                    break;
                }
            }

            return codeDocument.GetDocumentIntermediateNode();
        }
    }
}
