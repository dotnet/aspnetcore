// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Test;

/// <summary>
/// Tests to validate the exact scenario described in the GitHub issue.
/// This demonstrates using RenderFragment contravariance with DynamicComponent.
/// </summary>
public class RenderFragmentIssueScenarioTest
{
    [Fact]
    public void RenderFragment_Contravariance_EnablesDynamicComponentScenario()
    {
        // This test validates the exact scenario from the issue:
        // Using a RenderFragment<IList> where RenderFragment<List<T>> is expected

        // Arrange - Non-generic fragment that renders from the base list type
        RenderFragment<IList<string>> itemsTemplate = (IList<string> models) => innerBuilder =>
        {
            foreach (var item in models)
            {
                innerBuilder.AddContent(0, $"Item: {item}");
            }
        };

        // Simulate dynamically picking a T at runtime
        var itemType = typeof(string);
        var listType = typeof(List<>).MakeGenericType(itemType); // List<string>

        // Act - Create parameters as would be done with DynamicComponent
        // Before contravariance, this would fail because RenderFragment<IList<string>> 
        // couldn't be assigned where RenderFragment<List<string>> was expected
        var parameters = new Dictionary<string, object>
        {
            ["ItemsTemplate"] = itemsTemplate, // ✅ Now works with contravariance!
        };

        // Validate we can cast to the expected type
        var typedFragment = parameters["ItemsTemplate"] as RenderFragment<List<string>>;

        // Assert
        Assert.NotNull(typedFragment);

        // Verify it actually works
        var list = new List<string> { "Product1", "Product2", "Product3" };
        var builder = new RenderTreeBuilder();
        var result = typedFragment(list);
        result(builder);
    }

    [Fact]
    public void RenderFragment_Contravariance_WorksWithPagerComponent()
    {
        // This test simulates a more complete scenario with a pager component
        // that expects RenderFragment<List<Product>> but we provide RenderFragment<IList<Product>>

        // Arrange - Create a base template that works with any IList<Product>
        RenderFragment<IList<Product>> baseTemplate = (IList<Product> items) => innerBuilder =>
        {
            innerBuilder.OpenElement(0, "div");
            foreach (var item in items)
            {
                innerBuilder.OpenElement(1, "span");
                innerBuilder.AddContent(2, item.Name);
                innerBuilder.CloseElement();
            }
            innerBuilder.CloseElement();
        };

        // Act - Use it where List<Product> is expected (contravariance)
        RenderFragment<List<Product>> specificTemplate = baseTemplate;

        var products = new List<Product>
        {
            new Product { Name = "Product 1" },
            new Product { Name = "Product 2" },
            new Product { Name = "Product 3" }
        };

        var builder = new RenderTreeBuilder();
        var result = specificTemplate(products);

        // Assert - Should compile and execute without error
        result(builder);
        Assert.NotNull(result);
    }

    [Fact]
    public void RenderFragment_Contravariance_EliminatesNeedForAdapter()
    {
        // This test demonstrates that we no longer need the complex adapter
        // shown in the issue's "Alternative Designs" section

        // Arrange - Base template
        RenderFragment<IList<string>> baseTemplate = (IList<string> items) => innerBuilder =>
        {
            innerBuilder.AddContent(0, $"Count: {items.Count}");
        };

        // Before contravariance, you'd need CreateTypedTemplate adapter (complex reflection code)
        // Now, direct assignment just works:
        RenderFragment<List<string>> typedTemplate = baseTemplate; // ✅ Simple!

        // Act
        var list = new List<string> { "A", "B", "C" };
        var builder = new RenderTreeBuilder();
        var result = typedTemplate(list);
        result(builder);

        // Assert
        Assert.NotNull(result);
        // The fact that this compiles and runs is the success - no adapter needed!
    }

    // Test classes
    private class Product
    {
        public string Name { get; set; } = string.Empty;
    }
}
