// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

public class ApplicationPartManagerTest
{
    [Fact]
    public void PopulateFeature_InvokesAllProvidersSequentially_ForAGivenFeature()
    {
        // Arrange
        var manager = new ApplicationPartManager();
        manager.ApplicationParts.Add(new ControllersPart("ControllersPartA"));
        manager.ApplicationParts.Add(new ViewComponentsPart("ViewComponentsPartB"));
        manager.ApplicationParts.Add(new ControllersPart("ControllersPartC"));
        manager.FeatureProviders.Add(
            new ControllersFeatureProvider((f, v) => f.Values.Add($"ControllersFeatureProvider1{v}")));
        manager.FeatureProviders.Add(
            new ControllersFeatureProvider((f, v) => f.Values.Add($"ControllersFeatureProvider2{v}")));

        var feature = new ControllersFeature();
        var expectedResults = new[]
        {
                "ControllersFeatureProvider1ControllersPartA",
                "ControllersFeatureProvider1ControllersPartC",
                "ControllersFeatureProvider2ControllersPartA",
                "ControllersFeatureProvider2ControllersPartC"
            };

        // Act
        manager.PopulateFeature(feature);

        // Assert
        Assert.Equal(expectedResults, feature.Values.ToArray());
    }

    [Fact]
    public void PopulateFeature_InvokesOnlyProviders_ForAGivenFeature()
    {
        // Arrange
        var manager = new ApplicationPartManager();
        manager.ApplicationParts.Add(new ControllersPart("ControllersPart"));
        manager.FeatureProviders.Add(
            new ControllersFeatureProvider((f, v) => f.Values.Add($"ControllersFeatureProvider{v}")));
        manager.FeatureProviders.Add(
            new NotControllersedFeatureProvider((f, v) => f.Values.Add($"ViewComponentsFeatureProvider{v}")));

        var feature = new ControllersFeature();
        var expectedResults = new[] { "ControllersFeatureProviderControllersPart" };

        // Act
        manager.PopulateFeature(feature);

        // Assert
        Assert.Equal(expectedResults, feature.Values.ToArray());
    }

    [Fact]
    public void PopulateFeature_SkipProviders_ForOtherFeatures()
    {
        // Arrange
        var manager = new ApplicationPartManager();
        manager.ApplicationParts.Add(new ViewComponentsPart("ViewComponentsPart"));
        manager.FeatureProviders.Add(
            new ControllersFeatureProvider((f, v) => f.Values.Add($"ControllersFeatureProvider{v}")));

        var feature = new ControllersFeature();

        // Act
        manager.PopulateFeature(feature);

        // Assert
        Assert.Empty(feature.Values.ToArray());
    }

    private class ControllersPart : ApplicationPart
    {
        public ControllersPart(string value)
        {
            Value = value;
        }

        public override string Name => "Test";

        public string Value { get; }
    }

    private class ViewComponentsPart : ApplicationPart
    {
        public ViewComponentsPart(string value)
        {
            Value = value;
        }

        public override string Name => "Other";

        public string Value { get; }
    }

    private class ControllersFeature
    {
        public IList<string> Values { get; } = new List<string>();
    }

    private class ViewComponentsFeature
    {
        public IList<string> Values { get; } = new List<string>();
    }

    private class NotControllersedFeatureProvider : IApplicationFeatureProvider<ViewComponentsFeature>
    {
        private readonly Action<ViewComponentsFeature, string> _operation;

        public NotControllersedFeatureProvider(Action<ViewComponentsFeature, string> operation)
        {
            _operation = operation;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewComponentsFeature feature)
        {
            foreach (var part in parts.OfType<ViewComponentsPart>())
            {
                _operation(feature, part.Value);
            }
        }
    }

    private class ControllersFeatureProvider : IApplicationFeatureProvider<ControllersFeature>
    {
        private readonly Action<ControllersFeature, string> _operation;

        public ControllersFeatureProvider(Action<ControllersFeature, string> operation)
        {
            _operation = operation;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllersFeature feature)
        {
            foreach (var part in parts.OfType<ControllersPart>())
            {
                _operation(feature, part.Value);
            }
        }
    }
}
