// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Test;

public class RenderFragmentContravarianceTest
{
    [Fact]
    public void RenderFragment_SupportsContravariance_WithBaseClass()
    {
        // Arrange
        var builder = new RenderTreeBuilder();
        RenderFragment<Animal> animalFragment = (Animal animal) => innerBuilder =>
        {
            innerBuilder.AddContent(0, $"Animal: {animal.Name}");
        };

        // Act - Assign to a variable expecting a more derived type (contravariance)
        RenderFragment<Dog> dogFragment = animalFragment;
        var dog = new Dog { Name = "Buddy", Breed = "Golden Retriever" };
        var result = dogFragment(dog);

        // Assert - Should compile and work without exception
        result(builder);
        Assert.NotNull(result);
    }

    [Fact]
    public void RenderFragment_SupportsContravariance_WithInterface()
    {
        // Arrange
        var builder = new RenderTreeBuilder();
        RenderFragment<IList<string>> listFragment = (IList<string> items) => innerBuilder =>
        {
            foreach (var item in items)
            {
                innerBuilder.AddContent(0, item);
            }
        };

        // Act - Assign to a variable expecting a more specific type (contravariance)
        RenderFragment<List<string>> specificListFragment = listFragment;
        var list = new List<string> { "Item1", "Item2", "Item3" };
        var result = specificListFragment(list);

        // Assert - Should compile and work without exception
        result(builder);
        Assert.NotNull(result);
    }

    [Fact]
    public void RenderFragment_SupportsContravariance_InMethodParameter()
    {
        // Arrange
        RenderFragment<Animal> animalFragment = (Animal animal) => innerBuilder =>
        {
            innerBuilder.AddContent(0, $"Animal: {animal.Name}");
        };

        var dog = new Dog { Name = "Max", Breed = "Labrador" };
        var builder = new RenderTreeBuilder();

        // Act - Pass base type fragment to method expecting derived type fragment
        ProcessDogFragment(animalFragment, dog, builder);

        // Assert - Should compile and work without exception
        Assert.True(true); // If we got here, contravariance worked
    }

    [Fact]
    public void RenderFragment_SupportsContravariance_WithObject()
    {
        // Arrange
        var builder = new RenderTreeBuilder();
        RenderFragment<object> objectFragment = (object obj) => innerBuilder =>
        {
            innerBuilder.AddContent(0, obj?.ToString() ?? "null");
        };

        // Act - Assign to a variable expecting a more specific type (contravariance)
        RenderFragment<string> stringFragment = objectFragment;
        var result = stringFragment("test string");

        // Assert - Should compile and work without exception
        result(builder);
        Assert.NotNull(result);
    }

    private void ProcessDogFragment(RenderFragment<Dog> fragment, Dog dog, RenderTreeBuilder builder)
    {
        var result = fragment(dog);
        result(builder);
    }

    // Test classes
    private class Animal
    {
        public string Name { get; set; } = string.Empty;
    }

    private class Dog : Animal
    {
        public string Breed { get; set; } = string.Empty;
    }
}
