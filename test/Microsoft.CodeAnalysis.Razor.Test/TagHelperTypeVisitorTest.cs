// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Generic;
using Xunit;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    public class TagHelperTypeVisitorTest
    {
        private static readonly Assembly _assembly = typeof(TagHelperTypeVisitorTest).GetTypeInfo().Assembly;

        private static Compilation Compilation { get; } = TestCompilation.Create(_assembly);

        private static INamedTypeSymbol ITagHelperSymbol { get; } = Compilation.GetTypeByMetadataName(TagHelperTypes.ITagHelper);

        [Fact]
        public void IsTagHelper_PlainTagHelper_ReturnsTrue()
        {
            // Arrange
            var testVisitor = new TagHelperTypeVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_PlainTagHelper).FullName);

            // Act
            var isTagHelper = testVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.True(isTagHelper);
        }

        [Fact]
        public void IsTagHelper_InheritedTagHelper_ReturnsTrue()
        {
            // Arrange
            var testVisitor = new TagHelperTypeVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_InheritedTagHelper).FullName);

            // Act
            var isTagHelper = testVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.True(isTagHelper);
        }

        [Fact]
        public void IsTagHelper_AbstractTagHelper_ReturnsFalse()
        {
            // Arrange
            var testVisitor = new TagHelperTypeVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_AbstractTagHelper).FullName);

            // Act
            var isTagHelper = testVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.False(isTagHelper);
        }

        [Fact]
        public void IsTagHelper_GenericTagHelper_ReturnsFalse()
        {
            // Arrange
            var testVisitor = new TagHelperTypeVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_GenericTagHelper<>).FullName);

            // Act
            var isTagHelper = testVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.False(isTagHelper);
        }

        [Fact]
        public void IsTagHelper_InternalTagHelper_ReturnsFalse()
        {
            // Arrange
            var testVisitor = new TagHelperTypeVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_InternalTagHelper).FullName);

            // Act
            var isTagHelper = testVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.False(isTagHelper);
        }




        public class Invalid_NestedPublicTagHelper : TagHelper
        {
        }

        public class Valid_NestedPublicViewComponent
        {
            public string Invoke(string foo) => null;
        }
    }
}
