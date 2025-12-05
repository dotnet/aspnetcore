// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Microsoft.AspNetCore.Components;

public class EnvironmentBoundaryTest
{
    [Theory]
    [InlineData("Development", "Development")]
    [InlineData("development", "Development")]
    [InlineData("DEVELOPMENT", "Development")]
    [InlineData(" development", "Development")]
    [InlineData("development ", "Development")]
    [InlineData(" development ", "Development")]
    [InlineData("Development,Production", "Development")]
    [InlineData("Production,Development", "Development")]
    [InlineData("Development , Production", "Development")]
    [InlineData("   Development,Production   ", "Development")]
    [InlineData("Development ,  Production", "Development")]
    [InlineData("Development\t,Production", "Development")]
    [InlineData("Development,\tProduction", "Development")]
    [InlineData(" Development,Production ", "Development")]
    [InlineData("Development,Staging,Production", "Development")]
    [InlineData("Staging,Development,Production", "Development")]
    [InlineData("Staging,Production,Development", "Development")]
    [InlineData("Test", "Test")]
    [InlineData("Test,Staging", "Test")]
    public void ShowsContentWhenCurrentEnvironmentIsInIncludeList(string includeAttribute, string environmentName)
    {
        ShouldShowContentWithInclude(includeAttribute, environmentName);
    }

    [Theory]
    [InlineData("NotDevelopment", "Development")]
    [InlineData("NOTDEVELOPMENT", "Development")]
    [InlineData("NotDevelopment,AlsoNotDevelopment", "Development")]
    [InlineData("Doesn'tMatchAtAll", "Development")]
    [InlineData("Development and a space", "Development")]
    [InlineData("Development and a space,SomethingElse", "Development")]
    public void HidesContentWhenCurrentEnvironmentIsNotInIncludeList(string includeAttribute, string environmentName)
    {
        ShouldHideContentWithInclude(includeAttribute, environmentName);
    }

    [Theory]
    [InlineData(null, "Development")]
    [InlineData("", "Development")]
    [InlineData("  ", "Development")]
    [InlineData(", ", "Development")]
    [InlineData(",", "Development")]
    public void ShowsContentWhenNoIncludeOrExcludeIsSpecified(string includeAttribute, string environmentName)
    {
        // When no Include is specified and Exclude is not specified or empty,
        // the component should render its content
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent(environmentName);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.Include), includeAttribute },
            { nameof(EnvironmentBoundary.ChildContent), (RenderFragment)(builder => builder.AddContent(0, "Test Content")) },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.Contains(batch.ReferenceFrames, frame =>
            frame.FrameType == RenderTree.RenderTreeFrameType.Text &&
            frame.TextContent == "Test Content");
    }

    [Theory]
    [InlineData("Production", "Development")]
    [InlineData("production", "Development")]
    [InlineData("PRODUCTION", "Development")]
    [InlineData("Production,Staging", "Development")]
    public void ShowsContentWhenCurrentEnvironmentIsNotInExcludeList(string excludeAttribute, string environmentName)
    {
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent(environmentName);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.Exclude), excludeAttribute },
            { nameof(EnvironmentBoundary.ChildContent), (RenderFragment)(builder => builder.AddContent(0, "Test Content")) },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.Contains(batch.ReferenceFrames, frame =>
            frame.FrameType == RenderTree.RenderTreeFrameType.Text &&
            frame.TextContent == "Test Content");
    }

    [Theory]
    [InlineData("Development", "Development")]
    [InlineData("development", "Development")]
    [InlineData("DEVELOPMENT", "Development")]
    [InlineData("Development,Staging", "Development")]
    [InlineData("Production,Development", "Development")]
    public void HidesContentWhenCurrentEnvironmentIsInExcludeList(string excludeAttribute, string environmentName)
    {
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent(environmentName);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.Exclude), excludeAttribute },
            { nameof(EnvironmentBoundary.ChildContent), (RenderFragment)(builder => builder.AddContent(0, "Test Content")) },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.DoesNotContain(batch.ReferenceFrames, frame =>
            frame.FrameType == RenderTree.RenderTreeFrameType.Text &&
            frame.TextContent == "Test Content");
    }

    [Theory]
    [InlineData("Development", "Development", "Development")] // In include, also in exclude -> hide
    [InlineData("Development,Staging", "Staging", "Staging")] // In include, also in exclude -> hide
    public void ExcludeTakesPrecedenceOverInclude(string includeAttribute, string excludeAttribute, string environmentName)
    {
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent(environmentName);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.Include), includeAttribute },
            { nameof(EnvironmentBoundary.Exclude), excludeAttribute },
            { nameof(EnvironmentBoundary.ChildContent), (RenderFragment)(builder => builder.AddContent(0, "Test Content")) },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.DoesNotContain(batch.ReferenceFrames, frame =>
            frame.FrameType == RenderTree.RenderTreeFrameType.Text &&
            frame.TextContent == "Test Content");
    }

    [Theory]
    [InlineData("Development", "Production", "Development")] // In include, not in exclude -> show
    [InlineData("Development,Staging", "Production", "Staging")] // In include, not in exclude -> show
    public void ShowsContentWhenInIncludeButNotInExclude(string includeAttribute, string excludeAttribute, string environmentName)
    {
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent(environmentName);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.Include), includeAttribute },
            { nameof(EnvironmentBoundary.Exclude), excludeAttribute },
            { nameof(EnvironmentBoundary.ChildContent), (RenderFragment)(builder => builder.AddContent(0, "Test Content")) },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.Contains(batch.ReferenceFrames, frame =>
            frame.FrameType == RenderTree.RenderTreeFrameType.Text &&
            frame.TextContent == "Test Content");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    public void HidesContentWhenEnvironmentNameIsNullOrEmpty(string environmentName)
    {
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent(environmentName);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.Include), "Development" },
            { nameof(EnvironmentBoundary.ChildContent), (RenderFragment)(builder => builder.AddContent(0, "Test Content")) },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.DoesNotContain(batch.ReferenceFrames, frame =>
            frame.FrameType == RenderTree.RenderTreeFrameType.Text &&
            frame.TextContent == "Test Content");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ShowsContentWhenEnvironmentNameIsNullOrEmptyAndNoIncludeExcludeSpecified(string environmentName)
    {
        // For consistency with MVC EnvironmentTagHelper, render content when environment name is not set
        // and no Include/Exclude are specified.
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent(environmentName);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.ChildContent), (RenderFragment)(builder => builder.AddContent(0, "Test Content")) },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.Contains(batch.ReferenceFrames, frame =>
            frame.FrameType == RenderTree.RenderTreeFrameType.Text &&
            frame.TextContent == "Test Content");
    }

    [Fact]
    public void RendersNothingWhenChildContentIsNull()
    {
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent("Development");

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.Include), "Development" },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.DoesNotContain(batch.ReferenceFrames, f => f.FrameType == RenderTree.RenderTreeFrameType.Text);
    }

    private void ShouldShowContentWithInclude(string includeAttribute, string environmentName)
    {
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent(environmentName);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.Include), includeAttribute },
            { nameof(EnvironmentBoundary.ChildContent), (RenderFragment)(builder => builder.AddContent(0, "Test Content")) },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.Contains(batch.ReferenceFrames, frame =>
            frame.FrameType == RenderTree.RenderTreeFrameType.Text &&
            frame.TextContent == "Test Content");
    }

    private void ShouldHideContentWithInclude(string includeAttribute, string environmentName)
    {
        var (renderer, componentId) = CreateEnvironmentBoundaryComponent(environmentName);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            { nameof(EnvironmentBoundary.Include), includeAttribute },
            { nameof(EnvironmentBoundary.ChildContent), (RenderFragment)(builder => builder.AddContent(0, "Test Content")) },
        });

        renderer.RenderRootComponent(componentId, parameters);

        var batch = renderer.Batches.Single();
        Assert.DoesNotContain(batch.ReferenceFrames, frame =>
            frame.FrameType == RenderTree.RenderTreeFrameType.Text &&
            frame.TextContent == "Test Content");
    }

    private static (TestRenderer Renderer, int ComponentId) CreateEnvironmentBoundaryComponent(string environmentName)
    {
        var serviceProvider = new TestServiceProvider();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupProperty(h => h.EnvironmentName, environmentName);
        serviceProvider.AddService<IHostEnvironment>(hostEnvironment.Object);

        var renderer = new TestRenderer(serviceProvider);
        var component = (EnvironmentBoundary)renderer.InstantiateComponent<EnvironmentBoundary>();
        var componentId = renderer.AssignRootComponentId(component);

        return (renderer, componentId);
    }
}
