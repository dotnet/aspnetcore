// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DesignTimeDirectiveTargetExtensionTest
    {
        [Fact]
        public void WriteDesignTimeDirective_NoChildren_WritesEmptyMethod_WithPragma()
        {
            // Arrange
            var extension = new DesignTimeDirectiveTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter()
            };

            var node = new DesignTimeDirectiveIRNode();

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
}
#pragma warning restore 219
",
                    csharp,
                    ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDesignTimeDirective_WithTypeToken_WritesLambda()
        {
            // Arrange
            var extension = new DesignTimeDirectiveTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                CodeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("test content", "test.cshtml"))
            };

            var node = new DesignTimeDirectiveIRNode();
            var token = new DirectiveTokenIRNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "System.String",
                Descriptor = new DirectiveTokenDescriptor()
                {
                    Kind = DirectiveTokenKind.Type
                }
            };
            node.Children.Add(token);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
System.String __typeHelper = null;
}
))();
}
#pragma warning restore 219
",
                    csharp,
                    ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDesignTimeDirective_WithNamespaceToken_WritesLambda()
        {
            // Arrange
            var extension = new DesignTimeDirectiveTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                CodeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("test content", "test.cshtml"))
            };

            var node = new DesignTimeDirectiveIRNode();
            var token = new DirectiveTokenIRNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "System.Collections.Generic",
                Descriptor = new DirectiveTokenDescriptor()
                {
                    Kind = DirectiveTokenKind.Namespace
                }
            };
            node.Children.Add(token);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
global::System.Object __typeHelper = nameof(System.Collections.Generic);
}
))();
}
#pragma warning restore 219
",
                    csharp,
                    ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDesignTimeDirective_WithMemberToken_WritesLambda()
        {
            // Arrange
            var extension = new DesignTimeDirectiveTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                CodeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("test content", "test.cshtml"))
            };

            var node = new DesignTimeDirectiveIRNode();
            var token = new DirectiveTokenIRNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "Foo",
                Descriptor = new DirectiveTokenDescriptor()
                {
                    Kind = DirectiveTokenKind.Member
                }
            };
            node.Children.Add(token);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
global::System.Object Foo = null;
}
))();
}
#pragma warning restore 219
",
                    csharp,
                    ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDesignTimeDirective_WithStringToken_WritesLambda()
        {
            // Arrange
            var extension = new DesignTimeDirectiveTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                CodeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("test content", "test.cshtml"))
            };

            var node = new DesignTimeDirectiveIRNode();
            var token = new DirectiveTokenIRNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "Value",
                Descriptor = new DirectiveTokenDescriptor()
                {
                    Kind = DirectiveTokenKind.String
                }
            };
            var tokenWithQuotedContent = new DirectiveTokenIRNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "\"Value\"",
                Descriptor = new DirectiveTokenDescriptor()
                {
                    Kind = DirectiveTokenKind.String
                }
            };
            node.Children.Add(token);
            node.Children.Add(tokenWithQuotedContent);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
global::System.Object __typeHelper = ""Value"";
}
))();
((System.Action)(() => {
global::System.Object __typeHelper = ""Value"";
}
))();
}
#pragma warning restore 219
",
                    csharp,
                    ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDesignTimeDirective_ChildrenWithNoSource_WritesEmptyMethod_WithPragma()
        {
            // Arrange
            var extension = new DesignTimeDirectiveTargetExtension();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                CodeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("test content", "test.cshtml"))
            };

            var node = new DesignTimeDirectiveIRNode();
            var token = new DirectiveTokenIRNode()
            {
                Content = "Value",
                Descriptor = new DirectiveTokenDescriptor()
                {
                    Kind = DirectiveTokenKind.String
                }
            };
            node.Children.Add(token);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
}
#pragma warning restore 219
",
                    csharp,
                    ignoreLineEndingDifferences: true);
        }
    }
}
