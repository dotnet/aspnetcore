// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Hosting;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

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
    public void PopulateFeature_PopulatesRazorCompiledItemsFromTypeAssembly()
    {
        // Arrange
        var item1 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item1" && i.Type == typeof(TestView));
        var item2 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item2" && i.Type == typeof(TestPage));

        var assembly = new TestAssembly(new[]
        {
                new RazorCompiledItemAttribute(typeof(TestView), "mvc.1.0.razor-page", "Item1"),
                new RazorCompiledItemAttribute(typeof(TestView), "mvc.1.0.razor-view", "Item1"),
            });

        var part1 = new AssemblyPart(assembly);
        var part2 = new Mock<ApplicationPart>();
        part2
            .As<IRazorCompiledItemProvider>()
            .Setup(p => p.CompiledItems)
            .Returns(new[] { item1, item2, });
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

    [Fact]
    public void PopulateFeature_ReplacesWithHotRelaodedItems()
    {
        // Arrange
        var item1 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item1" && i.Type == typeof(TestView));
        var item2 = Mock.Of<RazorCompiledItem>(i => i.Identifier == "Item2" && i.Type == typeof(TestPage) && i.Kind == "mvc.1.0.razor-page");

        var applicationPart = new TestRazorCompiledItemProvider
        {
            CompiledItems = new[] { item1, item2 }
        };
        var featureProvider = new RazorCompiledItemFeatureProvider();

        // Act - 1
        var feature = new ViewsFeature();
        featureProvider.PopulateFeature(new[] { applicationPart }, feature);

        // Assert - 1
        Assert.Equal(new[] { item1, item2 }, feature.ViewDescriptors.Select(d => d.Item));

        // Act - 2
        featureProvider.UpdateCache(new[] { typeof(object), typeof(TestReloadedPage) });

        // Assert - 2
        feature = new ViewsFeature();
        featureProvider.PopulateFeature(new[] { applicationPart }, feature);
        Assert.Collection(
            feature.ViewDescriptors,
            item =>
            {
                Assert.Same(item.Item, item1);
            },
            item =>
            {
                Assert.Same(typeof(TestReloadedPage), item.Item.Type);
                Assert.Same("Item2", item.Item.Identifier);
                Assert.Equal("mvc.1.0.razor-page", item.Item.Kind);
                Assert.Contains(typeof(RouteAttribute), item.Item.Metadata.Select(m => m.GetType())); // Verify we pick up new attributes
            });

    }

    private class TestRazorCompiledItemProvider : ApplicationPart, IRazorCompiledItemProvider
    {
        public IEnumerable<RazorCompiledItem> CompiledItems { get; set; }

        public override string Name => "Test";
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

    [Route("some-route")]
    [RazorCompiledItemMetadata("RouteTemplate", "new-route")]
    [RazorCompiledItemMetadata("Identifier", "Item2")]
    private class TestReloadedPage { }
}
