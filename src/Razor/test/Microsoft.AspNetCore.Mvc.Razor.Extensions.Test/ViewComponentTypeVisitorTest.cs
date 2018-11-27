// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ViewComponentTypeVisitorTest
    {
        private static readonly Assembly _assembly = typeof(ViewComponentTypeVisitorTest).GetTypeInfo().Assembly;

        private static Compilation Compilation { get; } = TestCompilation.Create(_assembly);

        // In practice MVC will provide a marker attribute for ViewComponents. To prevent a circular reference between MVC and Razor
        // we can use a test class as a marker.
        private static INamedTypeSymbol TestViewComponentAttributeSymbol { get; } = Compilation.GetTypeByMetadataName(typeof(TestViewComponentAttribute).FullName);
        private static INamedTypeSymbol TestNonViewComponentAttributeSymbol { get; } = Compilation.GetTypeByMetadataName(typeof(TestNonViewComponentAttribute).FullName);

        [Fact]
        public void IsViewComponent_PlainViewComponent_ReturnsTrue()
        {
            // Arrange
            var testVisitor = new ViewComponentTypeVisitor(
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
            var testVisitor = new ViewComponentTypeVisitor(
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
            var testVisitor = new ViewComponentTypeVisitor(
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
            var testVisitor = new ViewComponentTypeVisitor(
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
            var testVisitor = new ViewComponentTypeVisitor(
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
            var testVisitor = new ViewComponentTypeVisitor(
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
        public void IsViewComponent_DecoratedNonViewComponent_ReturnsFalse()
        {
            // Arrange
            var testVisitor = new ViewComponentTypeVisitor(
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
            var testVisitor = new ViewComponentTypeVisitor(
                TestViewComponentAttributeSymbol,
                TestNonViewComponentAttributeSymbol,
                new List<INamedTypeSymbol>());
            var tagHelperSymbol = Compilation.GetTypeByMetadataName(typeof(Invalid_InheritedViewComponent).FullName);

            // Act
            var isViewComponent = testVisitor.IsViewComponent(tagHelperSymbol);

            // Assert
            Assert.False(isViewComponent);
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
}
