// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DirectiveDescriptorBuilderTest
    {
        [Fact]
        public void Create_BuildsSingleLineDirectiveDescriptor()
        {
            // Act
            var descriptor = DirectiveDescriptorBuilder.Create("custom").Build();

            // Assert
            Assert.Equal(DirectiveDescriptorKind.SingleLine, descriptor.Kind);
        }

        [Fact]
        public void CreateRazorBlock_BuildsRazorBlockDirectiveDescriptor()
        {
            // Act
            var descriptor = DirectiveDescriptorBuilder.CreateRazorBlock("custom").Build();

            // Assert
            Assert.Equal(DirectiveDescriptorKind.RazorBlock, descriptor.Kind);
        }

        [Fact]
        public void CreateCodeBlock_BuildsCodeBlockDirectiveDescriptor()
        {
            // Act
            var descriptor = DirectiveDescriptorBuilder.CreateCodeBlock("custom").Build();

            // Assert
            Assert.Equal(DirectiveDescriptorKind.CodeBlock, descriptor.Kind);
        }

        [Fact]
        public void AddType_AddsToken()
        {
            // Arrange
            var builder = DirectiveDescriptorBuilder.Create("custom");

            // Act
            var descriptor = builder.AddType().Build();

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.Type, token.Kind);
        }

        [Fact]
        public void AddMember_AddsToken()
        {
            // Arrange
            var builder = DirectiveDescriptorBuilder.Create("custom");

            // Act
            var descriptor = builder.AddMember().Build();

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.Member, token.Kind);
        }

        [Fact]
        public void AddString_AddsToken()
        {
            // Arrange
            var builder = DirectiveDescriptorBuilder.Create("custom");

            // Act
            var descriptor = builder.AddString().Build();

            // Assert
            var token = Assert.Single(descriptor.Tokens);
            Assert.Equal(DirectiveTokenKind.String, token.Kind);
        }

        [Fact]
        public void AddX_MaintainsMultipleTokens()
        {
            // Arrange
            var builder = DirectiveDescriptorBuilder.Create("custom");

            // Act
            var descriptor = builder
                .AddType()
                .AddMember()
                .AddString()
                .Build();

            // Assert
            Assert.Collection(descriptor.Tokens,
                token => Assert.Equal(DirectiveTokenKind.Type, token.Kind),
                token => Assert.Equal(DirectiveTokenKind.Member, token.Kind),
                token => Assert.Equal(DirectiveTokenKind.String, token.Kind));
        }
    }
}
