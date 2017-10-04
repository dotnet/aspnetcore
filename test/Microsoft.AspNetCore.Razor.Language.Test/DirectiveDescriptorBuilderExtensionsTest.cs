// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DirectiveDescriptorBuilderExtensionsTest
    {
        [Fact]
        public void AddMemberToken_AddsToken()
        {
            // Arrange & Act
            var descriptor = DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, b => b.AddMemberToken());

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.Member, token.Kind);
            Assert.False(token.Optional);
            Assert.Null(token.Name);
            Assert.Null(token.Description);
        }

        [Fact]
        public void AddNamespaceToken_AddsToken()
        {
            // Arrange & Act
            var descriptor = DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, b => b.AddNamespaceToken("Name", "Description"));

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.Namespace, token.Kind);
            Assert.False(token.Optional);
            Assert.Equal("Name", token.Name);
            Assert.Equal("Description", token.Description);
        }

        [Fact]
        public void AddStringToken_AddsToken()
        {
            // Arrange & Act
            var descriptor = DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, b => b.AddStringToken());

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.String, token.Kind);
            Assert.False(token.Optional);
            Assert.Null(token.Name);
            Assert.Null(token.Description);
        }

        [Fact]
        public void AddTypeToken_AddsToken()
        {
            // Arrange & Act
            var descriptor = DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, b => b.AddTypeToken("Name", "Description"));

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.Type, token.Kind);
            Assert.False(token.Optional);
            Assert.Equal("Name", token.Name);
            Assert.Equal("Description", token.Description);
        }

        [Fact]
        public void AddOptionalTypeToken_AddsToken()
        {
            // Arrange & Act
            var descriptor = DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, b => b.AddOptionalTypeToken());

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.Type, token.Kind);
            Assert.True(token.Optional);
            Assert.Null(token.Name);
            Assert.Null(token.Description);
        }

        [Fact]
        public void AddOptionalMemberToken_AddsToken()
        {
            // Arrange & Act
            var descriptor = DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, b => b.AddOptionalMemberToken("Name", "Description"));

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.Member, token.Kind);
            Assert.True(token.Optional);
            Assert.Equal("Name", token.Name);
            Assert.Equal("Description", token.Description);
        }

        [Fact]
        public void AddOptionalNamespaceToken_AddsToken()
        {
            // Arrange & Act
            var descriptor = DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, b => b.AddOptionalNamespaceToken());

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.Namespace, token.Kind);
            Assert.True(token.Optional);
            Assert.Null(token.Name);
            Assert.Null(token.Description);
        }

        [Fact]
        public void AddOptionalStringToken_AddsToken()
        {
            // Arrange & Act
            var descriptor = DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, b => b.AddOptionalStringToken("Name", "Description"));

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.String, token.Kind);
            Assert.True(token.Optional);
            Assert.Equal("Name", token.Name);
            Assert.Equal("Description", token.Description);
        }
    }
}
