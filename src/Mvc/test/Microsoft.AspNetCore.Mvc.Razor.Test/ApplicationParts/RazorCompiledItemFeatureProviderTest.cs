// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Hosting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public class RazorCompiledItemFeatureProviderTest
    {
        [Fact]
        public void PopulateFeature_AddsItemsFromProviderTypes()
        {
            // Arrange
            var item1 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item1" && i.Type == typeof(TestView));
            var item2 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item2" && i.Type == typeof(TestPage));
            var part1 = new AssemblyPart(typeof(RazorCompiledItemFeatureProviderTest).Assembly);
            var part2 = new Mock<ApplicationPart>();
            part2
                .As<IRazorCompiledItemProvider>()
                .Setup(p => p.CompiledItems).Returns(new[] { item1, item2, });
            var featureProvider = new RazorCompiledItemFeatureProvider();
            var feature = new ViewsFeature();

            // Act
            featureProvider.PopulateFeature(new[] { part1, part2.Object }, feature);

            // Assert
            Assert.Equal(new[] { item1, item2 }, feature.ViewDescriptors.Select(d => d.Item));
        }

        [Fact]
        public void PopulateFeature_PopulatesRazorViewAttributeFromTypeAssembly()
        {
            // Arrange
            var item1 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item1" && i.Type == typeof(TestView));
            var item2 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item2" && i.Type == typeof(TestPage));

            var attribute1 = new RazorViewAttribute("Item1", typeof(TestView));
            var attribute2 = new RazorViewAttribute("Item2", typeof(TestPage));

            var assembly = new TestAssembly(new[] { attribute1, attribute2 });

            var part1 = new AssemblyPart(assembly);
            var part2 = new Mock<ApplicationPart>();
            part2
                .As<IRazorCompiledItemProvider>()
                .Setup(p => p.CompiledItems).Returns(new[] { item1, item2, });
            var featureProvider = new RazorCompiledItemFeatureProvider();
            var feature = new ViewsFeature();

            // Act
            featureProvider.PopulateFeature(new[] { part1, part2.Object }, feature);

            // Assert
            Assert.Equal(new[] { item1, item2 }, feature.ViewDescriptors.Select(d => d.Item));
        }

        [Fact]
        public void PopulateFeature_AllowsDuplicateItemsFromMultipleParts()
        {
            // Arrange
            var item1 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item" && i.Type == typeof(TestView));
            var item2 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item" && i.Type == typeof(TestPage));
            var part1 = new Mock<ApplicationPart>();
            part1
                .As<IRazorCompiledItemProvider>()
                .Setup(p => p.CompiledItems).Returns(new[] { item1, });
            var part2 = new Mock<ApplicationPart>();
            part2
                .As<IRazorCompiledItemProvider>()
                .Setup(p => p.CompiledItems).Returns(new[] { item2, });
            var featureProvider = new RazorCompiledItemFeatureProvider();
            var feature = new ViewsFeature();

            // Act
            featureProvider.PopulateFeature(new[] { part1.Object, part2.Object }, feature);

            // Assert
            Assert.Equal(new[] { item1, item2 }, feature.ViewDescriptors.Select(d => d.Item));
        }

        [Fact]
        public void PopulateFeature_ThrowsIfTwoItemsFromSamePart_OnlyDifferInCase()
        {
            // Arrange
            var item1 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item");
            var item2 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "item");
            var expected = string.Join(
               Environment.NewLine,
               "The following precompiled view paths differ only in case, which is not supported:",
               "Item",
               "item");
            var part1 = new AssemblyPart(typeof(RazorCompiledItemFeatureProviderTest).Assembly);
            var part2 = new Mock<ApplicationPart>();
            part2
                .As<IRazorCompiledItemProvider>()
                .Setup(p => p.CompiledItems).Returns(new[] { item1, item2, });
            var featureProvider = new RazorCompiledItemFeatureProvider();
            var feature = new ViewsFeature();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => featureProvider.PopulateFeature(new[] { part1, part2.Object }, feature));
            Assert.Equal(expected, ex.Message);
        }

        private class TestAssembly : Assembly
        {
            private readonly object[] _attributes;

            public TestAssembly(object[] attributes)
            {
                _attributes = attributes;
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                return _attributes;
            }
        }

        private class TestView { }

        private class TestPage { }
    }
}
