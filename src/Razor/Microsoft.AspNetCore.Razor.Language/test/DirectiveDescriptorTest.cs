// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DirectiveDescriptorTest
    {
        [Fact]
        public void CreateDirective_CreatesDirective_WithProvidedKind()
        {
            // Arrange & Act
            var directive = DirectiveDescriptor.CreateDirective("test", DirectiveKind.SingleLine);

            // Assert
            Assert.Equal("test", directive.Directive);
            Assert.Equal(DirectiveKind.SingleLine, directive.Kind);
        }

        [Fact]
        public void CreateDirective_WithConfigure_CreatesDirective_WithProvidedKind()
        {
            // Arrange
            var called = false;
            Action<IDirectiveDescriptorBuilder> configure = b => { called = true; };

            // Act
            var directive = DirectiveDescriptor.CreateDirective("test", DirectiveKind.SingleLine, configure);

            // Assert
            Assert.Equal("test", directive.Directive);
            Assert.Equal(DirectiveKind.SingleLine, directive.Kind);
            Assert.True(called);
        }

        [Fact]
        public void CreateSingleLineDirective_CreatesSingleLineDirective()
        {
            // Arrange & Act
            var directive = DirectiveDescriptor.CreateSingleLineDirective("test");

            // Assert
            Assert.Equal("test", directive.Directive);
            Assert.Equal(DirectiveKind.SingleLine, directive.Kind);
        }

        [Fact]
        public void CreateSingleLineDirective_WithConfigure_CreatesSingleLineDirective()
        {
            // Arrange
            var called = false;
            Action<IDirectiveDescriptorBuilder> configure = b => { called = true; };

            // Act
            var directive = DirectiveDescriptor.CreateSingleLineDirective("test", configure);

            // Assert
            Assert.Equal("test", directive.Directive);
            Assert.Equal(DirectiveKind.SingleLine, directive.Kind);
            Assert.True(called);
        }

        [Fact]
        public void CreateRazorBlockDirective_CreatesRazorBlockDirective()
        {
            // Arrange & Act
            var directive = DirectiveDescriptor.CreateRazorBlockDirective("test");

            // Assert
            Assert.Equal("test", directive.Directive);
            Assert.Equal(DirectiveKind.RazorBlock, directive.Kind);
        }

        [Fact]
        public void CreateRazorBlockDirective_WithConfigure_CreatesRazorBlockDirective()
        {
            // Arrange
            var called = false;
            Action<IDirectiveDescriptorBuilder> configure = b => { called = true; };

            // Act
            var directive = DirectiveDescriptor.CreateRazorBlockDirective("test", configure);

            // Assert
            Assert.Equal("test", directive.Directive);
            Assert.Equal(DirectiveKind.RazorBlock, directive.Kind);
            Assert.True(called);
        }

        [Fact]
        public void CreateCodeBlockDirective_CreatesCodeBlockDirective()
        {
            // Arrange & Act
            var directive = DirectiveDescriptor.CreateCodeBlockDirective("test");

            // Assert
            Assert.Equal("test", directive.Directive);
            Assert.Equal(DirectiveKind.CodeBlock, directive.Kind);
        }

        [Fact]
        public void CreateCodeBlockDirective_WithConfigure_CreatesCodeBlockDirective()
        {
            // Arrange
            var called = false;
            Action<IDirectiveDescriptorBuilder> configure = b => { called = true; };

            // Act
            var directive = DirectiveDescriptor.CreateCodeBlockDirective("test", configure);

            // Assert
            Assert.Equal("test", directive.Directive);
            Assert.Equal(DirectiveKind.CodeBlock, directive.Kind);
            Assert.True(called);
        }

        [Fact]
        public void Build_ValidatesDirectiveKeyword_EmptyIsInvalid()
        {
            // Arrange & Act
            var ex = Assert.Throws<InvalidOperationException>(() => DirectiveDescriptor.CreateSingleLineDirective(""));

            // Assert
            Assert.Equal("Invalid directive keyword ''. Directives must have a non-empty keyword that consists only of letters.", ex.Message);
        }

        [Fact]
        public void Build_ValidatesDirectiveKeyword_InvalidCharacter()
        {
            // Arrange & Act
            var ex = Assert.Throws<InvalidOperationException>(() => DirectiveDescriptor.CreateSingleLineDirective("test_directive"));

            // Assert
            Assert.Equal("Invalid directive keyword 'test_directive'. Directives must have a non-empty keyword that consists only of letters.", ex.Message);
        }

        [Fact]
        public void Build_ValidatesDirectiveName_NonOptionalTokenFollowsOptionalToken()
        {
            // Arrange & Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => DirectiveDescriptor.CreateSingleLineDirective("test", b => { b.AddOptionalMemberToken(); b.AddMemberToken(); }));

            // Assert
            Assert.Equal("A non-optional directive token cannot follow an optional directive token.", ex.Message);
        }
    }
}
