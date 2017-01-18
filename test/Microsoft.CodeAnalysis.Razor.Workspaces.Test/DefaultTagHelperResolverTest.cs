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

        // In practice MVC will provide a marker attribute for ViewComponents. To prevent a circular reference between MVC and Razor
        // we can use a test class as a marker.
        private static INamedTypeSymbol TestViewComponentAttributeSymbol { get; } = Compilation.GetTypeByMetadataName(typeof(TestViewComponentAttribute).FullName);
        private static INamedTypeSymbol TestNonViewComponentAttributeSymbol { get; } = Compilation.GetTypeByMetadataName(typeof(TestNonViewComponentAttribute).FullName);

        [Fact]
        public void IsTagHelper_PlainTagHelper_ReturnsTrue()
        {
            // Arrange
            var testVisitor = new DefaultTagHelperResolver.TagHelperVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
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
            var testVisitor = new DefaultTagHelperResolver.TagHelperVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
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
            var testVisitor = new DefaultTagHelperResolver.TagHelperVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
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
            var testVisitor = new DefaultTagHelperResolver.TagHelperVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
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
            var testVisitor = new DefaultTagHelperResolver.TagHelperVisitor(ITagHelperSymbol, new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_InternalTagHelper).FullName);

            // Act
            var isTagHelper = testVisitor.IsTagHelper(tagHelperSymbol);

            // Assert
            Assert.False(isTagHelper);
        }

        [Fact]
        public void GetTagHelpers_NestedTagHelpersAreNotFound()
        {
            // Arrange
            var resolver = new DefaultTagHelperResolver(designTime: false);
            var expectedTypeName = typeof(DefaultTagHelperResolver).FullName + "." + nameof(Invalid_NestedPublicTagHelper);

            // Act
            var descriptors = resolver.GetTagHelpers(Compilation);

            // Assert
            var matchingDescriptors = descriptors
                .Where(descriptor => string.Equals(descriptor.TypeName, expectedTypeName, StringComparison.Ordinal));
            Assert.Empty(matchingDescriptors);
        }

        [Fact]
        public void IsViewComponent_PlainViewComponent_ReturnsTrue()
        {
            // Arrange
            var testVisitor = new DefaultTagHelperResolver.ViewComponentVisitor(
                TestViewComponentAttributeSymbol,
                TestNonViewComponentAttributeSymbol,
                new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_PlainViewComponent).FullName);

            // Act
            var isViewComponent = testVisitor.IsViewComponent(tagHelperSymbol);

            // Assert
            Assert.True(isViewComponent);
        }

        [Fact]
        public void IsViewComponent_DecoratedViewComponent_ReturnsTrue()
        {
            // Arrange
            var testVisitor = new DefaultTagHelperResolver.ViewComponentVisitor(
                TestViewComponentAttributeSymbol,
                TestNonViewComponentAttributeSymbol,
                new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_DecoratedVC).FullName);

            // Act
            var isViewComponent = testVisitor.IsViewComponent(tagHelperSymbol);

            // Assert
            Assert.True(isViewComponent);
        }

        [Fact]
        public void IsViewComponent_InheritedViewComponent_ReturnsTrue()
        {
            // Arrange
            var testVisitor = new DefaultTagHelperResolver.ViewComponentVisitor(
                TestViewComponentAttributeSymbol,
                TestNonViewComponentAttributeSymbol,
                new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Valid_InheritedVC).FullName);

            // Act
            var isViewComponent = testVisitor.IsViewComponent(tagHelperSymbol);

            // Assert
            Assert.True(isViewComponent);
        }

        [Fact]
        public void IsViewComponent_AbstractViewComponent_ReturnsFalse()
        {
            // Arrange
            var testVisitor = new DefaultTagHelperResolver.ViewComponentVisitor(
                TestViewComponentAttributeSymbol,
                TestNonViewComponentAttributeSymbol,
                new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_AbstractViewComponent).FullName);

            // Act
            var isViewComponent = testVisitor.IsViewComponent(tagHelperSymbol);

            // Assert
            Assert.False(isViewComponent);
        }

        [Fact]
        public void IsViewComponent_GenericViewComponent_ReturnsFalse()
        {
            // Arrange
            var testVisitor = new DefaultTagHelperResolver.ViewComponentVisitor(
                TestViewComponentAttributeSymbol,
                TestNonViewComponentAttributeSymbol,
                new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_GenericViewComponent<>).FullName);

            // Act
            var isViewComponent = testVisitor.IsViewComponent(tagHelperSymbol);

            // Assert
            Assert.False(isViewComponent);
        }

        [Fact]
        public void IsViewComponent_InternalViewComponent_ReturnsFalse()
        {
            // Arrange
            var testVisitor = new DefaultTagHelperResolver.ViewComponentVisitor(
                TestViewComponentAttributeSymbol,
                TestNonViewComponentAttributeSymbol,
                new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_InternalViewComponent).FullName);

            // Act
            var isViewComponent = testVisitor.IsViewComponent(tagHelperSymbol);

            // Assert
            Assert.False(isViewComponent);
        }

        [Fact]
        public void GetTagHelpers_NestedViewComponentTagHelpersAreFound()
        {
            // Arrange
            var resolver = new DefaultTagHelperResolver(
                designTime: false, 
                viewComponentAssembly: typeof(DefaultTagHelperResolverTest).Assembly.GetName().Name);
            var expectedTypeName = "__Generated__" + nameof(Valid_NestedPublicViewComponent) + "TagHelper";

            // Act
            var descriptors = resolver.GetTagHelpers(Compilation);

            // Assert
            Assert.Single(descriptors, descriptor => string.Equals(descriptor.TypeName, expectedTypeName, StringComparison.Ordinal));
        }

        [Fact]
        public void IsViewComponent_DecoratedNonViewComponent_ReturnsFalse()
        {
            // Arrange
            var testVisitor = new DefaultTagHelperResolver.ViewComponentVisitor(
                TestViewComponentAttributeSymbol,
                TestNonViewComponentAttributeSymbol,
                new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_DecoratedViewComponent).FullName);

            // Act
            var isViewComponent = testVisitor.IsViewComponent(tagHelperSymbol);

            // Assert
            Assert.False(isViewComponent);
        }

        [Fact]
        public void IsViewComponent_InheritedNonViewComponent_ReturnsFalse()
        {
            // Arrange
            var testVisitor = new DefaultTagHelperResolver.ViewComponentVisitor(
                TestViewComponentAttributeSymbol,
                TestNonViewComponentAttributeSymbol,
                new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_InheritedViewComponent).FullName);

            // Act
            var isViewComponent = testVisitor.IsViewComponent(tagHelperSymbol);

            // Assert
            Assert.False(isViewComponent);
        }

        public class Invalid_NestedPublicTagHelper : TagHelper
        {
        }

        public class Valid_NestedPublicViewComponent
        {
            public string Invoke(string foo) => null;
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

    public abstract class Invalid_AbstractViewComponent
    {
    }

    public class Invalid_GenericViewComponent<T>
    {
    }

    internal class Invalid_InternalViewComponent
    {
    }

    public class Valid_PlainViewComponent
    {
    }

    [TestViewComponent]
    public class Valid_DecoratedVC
    {
    }

    public class Valid_InheritedVC : Valid_DecoratedVC
    {
    }

    [TestNonViewComponent]
    public class Invalid_DecoratedViewComponent
    {
    }

    [TestViewComponent]
    public class Invalid_InheritedViewComponent : Invalid_DecoratedViewComponent
    {
    }

    public class TestViewComponentAttribute : Attribute
    {
    }

    public class TestNonViewComponentAttribute : Attribute
    {
    }
}
