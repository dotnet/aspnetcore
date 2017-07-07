// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class DesignTimeDirectiveTargetExtensionTest
    {
        [Fact]
        public void WriteDesignTimeDirective_NoChildren_WritesEmptyMethod_WithPragma()
        {
            // Arrange
            var extension = new DesignTimeDirectiveTargetExtension();
            var context = TestCodeRenderingContext.CreateDesignTime();

            var node = new DesignTimeDirectiveIntermediateNode();

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
            var context = TestCodeRenderingContext.CreateDesignTime();

            var node = new DesignTimeDirectiveIntermediateNode();
            var token = new DirectiveTokenIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "System.String",
                DirectiveToken = DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Type),
            };
            node.Children.Add(token);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
System.String __typeHelper = default(System.String);
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
            var context = TestCodeRenderingContext.CreateDesignTime();

            var node = new DesignTimeDirectiveIntermediateNode();
            var token = new DirectiveTokenIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "System.Collections.Generic",
                DirectiveToken = DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Namespace),
            };
            node.Children.Add(token);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
            var context = TestCodeRenderingContext.CreateDesignTime();

            var node = new DesignTimeDirectiveIntermediateNode();
            var token = new DirectiveTokenIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "Foo",
                DirectiveToken = DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Member),
            };
            node.Children.Add(token);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
            var context = TestCodeRenderingContext.CreateDesignTime();

            var node = new DesignTimeDirectiveIntermediateNode();
            var token = new DirectiveTokenIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "Value",
                DirectiveToken = DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.String),
            };
            var tokenWithQuotedContent = new DirectiveTokenIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "\"Value\"",
                DirectiveToken = DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.String),
            };
            node.Children.Add(token);
            node.Children.Add(tokenWithQuotedContent);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
            var context = TestCodeRenderingContext.CreateDesignTime();

            var node = new DesignTimeDirectiveIntermediateNode();
            var token = new DirectiveTokenIntermediateNode()
            {
                Content = "Value",
                DirectiveToken = DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.String),
            };
            node.Children.Add(token);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
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
