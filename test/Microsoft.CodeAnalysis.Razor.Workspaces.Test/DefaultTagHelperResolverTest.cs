// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis.Razor.Workspaces.Test;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    public class DefaultTagHelperResolverTest
    {
        private static Compilation Compilation { get; } = TestCompilation.Create();

        private static INamedTypeSymbol ITagHelperSymbol { get; } = Compilation.GetTypeByMetadataName(TagHelperTypes.ITagHelper);

        private DefaultTagHelperResolver.Visitor TestVisitor => new DefaultTagHelperResolver.Visitor(ITagHelperSymbol, new List<INamedTypeSymbol>());

        [Fact]
        public void IsTagHelper_PlainTagHelper_ReturnsTrue()
        {
            // Arrange
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_PlainTagHelper).FullName);

            // Act
            var isTagHelper = TestVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.True(isTagHelper);
        }

        [Fact]
        public void IsTagHelper_InheritedTagHelper_ReturnsTrue()
        {
            // Arrange
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_InheritedTagHelper).FullName);

            // Act
            var isTagHelper = TestVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.True(isTagHelper);
        }

        [Fact]
        public void IsTagHelper_AbstractTagHelper_ReturnsFalse()
        {
            // Arrange
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_AbstractTagHelper).FullName);

            // Act
            var isTagHelper = TestVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.False(isTagHelper);
        }

        [Fact]
        public void IsTagHelper_GenericTagHelper_ReturnsFalse()
        {
            // Arrange
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_GenericTagHelper<>).FullName);

            // Act
            var isTagHelper = TestVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.False(isTagHelper);
        }

        [Fact]
        public void IsTagHelper_InternalTagHelper_ReturnsFalse()
        {
            // Arrange
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_InternalTagHelper).FullName);

            // Act
            var isTagHelper = TestVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.False(isTagHelper);
        }

        [Fact]
        public void GetTagHelpers_NestedTagHelpersAreNotFound()
        {
            // Arrange
            var resolver = new DefaultTagHelperResolver(designTime: false);

            // Act
            var descriptors = resolver.GetTagHelpers(Compilation);

            // Assert
            var matchingDescriptors = descriptors
                .Where(descriptor => string.Equals(descriptor.TypeName, typeof(Invalid_NestedPublicTagHelper).FullName, StringComparison.Ordinal));
            Assert.Empty(matchingDescriptors);
        }

        public class Invalid_NestedPublicTagHelper : TagHelper
        {
        }
    }

    public abstract class Invalid_AbstractTagHelper : TagHelper
    {
    }

    public class Invalid_GenericTagHelper<T> : TagHelper
    {
    }

    internal class Invalid_InternalTagHelper : TagHelper
    {
    }

    public class Valid_PlainTagHelper : TagHelper
    {
    }

    public class Valid_InheritedTagHelper : Valid_PlainTagHelper
    {
    }
}
