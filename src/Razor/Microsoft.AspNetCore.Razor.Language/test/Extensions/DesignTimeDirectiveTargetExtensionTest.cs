// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            var csharp = context.CodeWriter.GenerateCode();
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
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
#nullable restore
#line 1 ""test.cshtml""
System.String __typeHelper = default!;

#line default
#line hidden
#nullable disable
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
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
#nullable restore
#line 1 ""test.cshtml""
global::System.Object __typeHelper = nameof(System.Collections.Generic);

#line default
#line hidden
#nullable disable
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
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
#nullable restore
#line 1 ""test.cshtml""
global::System.Object Foo = null!;

#line default
#line hidden
#nullable disable
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
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
#nullable restore
#line 1 ""test.cshtml""
global::System.Object __typeHelper = ""Value"";

#line default
#line hidden
#nullable disable
}
))();
((System.Action)(() => {
#nullable restore
#line 1 ""test.cshtml""
global::System.Object __typeHelper = ""Value"";

#line default
#line hidden
#nullable disable
}
))();
}
#pragma warning restore 219
",
                    csharp,
                    ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDesignTimeDirective_WithBooleanToken_WritesLambda()
        {
            // Arrange
            var extension = new DesignTimeDirectiveTargetExtension();
            var context = TestCodeRenderingContext.CreateDesignTime();
            
            var node = new DesignTimeDirectiveIntermediateNode();
            var token = new DirectiveTokenIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 5),
                Content = "true",
                DirectiveToken = DirectiveTokenDescriptor.CreateToken(DirectiveTokenKind.Boolean),
            };
            node.Children.Add(token);

            // Act
            extension.WriteDesignTimeDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"#pragma warning disable 219
private void __RazorDirectiveTokenHelpers__() {
((System.Action)(() => {
#nullable restore
#line 1 ""test.cshtml""
global::System.Boolean __typeHelper = true;

#line default
#line hidden
#nullable disable
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
            var csharp = context.CodeWriter.GenerateCode();
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
