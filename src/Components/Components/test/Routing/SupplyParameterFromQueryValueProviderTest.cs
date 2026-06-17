// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Routing;

public class SupplyParameterFromQueryValueProviderTest
{
    [Fact]
    public void NavigationWithNestedComponentsDoesNotThrowCollectionModifiedException()
    {
        // This test reproduces the bug where navigating causes a child component with
        // [SupplyParameterFromQuery] to be rendered, which modifies the subscribers collection
        // while it's being enumerated during OnLocationChanged

        // Arrange
        var serviceCollection = new ServiceCollection();
        var navigationManager = new FakeNavigationManager();
        serviceCollection.AddSingleton<NavigationManager>(navigationManager);
        serviceCollection.AddSupplyValueFromQueryProvider();
        var services = serviceCollection.BuildServiceProvider();

        var renderer = new TestRenderer(services);

        // Create a parent component that conditionally renders a child based on query parameter
        var parentComponent = new ConditionalParentComponent();
        var parentComponentId = renderer.AssignRootComponentId(parentComponent);

        // Initial render - parent without child
        navigationManager.NotifyLocationChanged("http://localhost/test", false);
        parentComponent.TriggerRender();

        // Assert initial state
        Assert.Single(renderer.Batches);

        // Act - Navigate to URL that causes child to be rendered
        // This should trigger OnLocationChanged which enumerates subscribers,
        // and the child component subscribes during that enumeration
        var exception = Record.Exception(() =>
        {
            navigationManager.NotifyLocationChanged("http://localhost/test?parentParam=showChild", false);
        });

        // Assert - No exception should be thrown
        Assert.Null(exception);

        // Verify the components rendered correctly
        Assert.True(renderer.Batches.Count >= 2, "Should have rendered at least twice");
    }

    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/test");
        }

        public void NotifyLocationChanged(string uri, bool isInterceptedLink)
        {
            Uri = uri;
            NotifyLocationChanged(isInterceptedLink);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private class ConditionalParentComponent : AutoRenderComponent
    {
        [SupplyParameterFromQuery]
        public string ParentParam { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Parent: {ParentParam}");

            if (ParentParam == "showChild")
            {
                builder.OpenComponent<ChildWithQueryParamComponent>(2);
                builder.CloseComponent();
            }

            builder.CloseElement();
        }
    }

    private class ChildWithQueryParamComponent : AutoRenderComponent
    {
        [SupplyParameterFromQuery]
        public string ChildParam { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, $"Child: {ChildParam}");
            builder.CloseElement();
        }
    }
}
