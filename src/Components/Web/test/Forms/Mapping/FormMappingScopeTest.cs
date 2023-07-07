// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class FormMappingScopeTest
{
    private readonly TestRenderer _renderer;
    private TestNavigationManager _navigationManager;

    public FormMappingScopeTest()
    {
        var serviceCollection = new ServiceCollection();
        _navigationManager = new TestNavigationManager();
        serviceCollection.AddSingleton<NavigationManager>(_navigationManager);
        serviceCollection.AddSingleton<IFormValueMapper, TestFormValueMapper>();
        var services = serviceCollection.BuildServiceProvider();
        _renderer = new TestRenderer(services);
    }

    [Fact]
    public void FormMappingScope_NoMappingContextId_ForDefaultName()
    {
        FormMappingContext capturedContext = null;
        RenderFragment<FormMappingContext> contents = (ctx) => b => { capturedContext = ctx; };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<FormMappingScope>(0);
            builder.AddAttribute(1, nameof(FormMappingScope.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Empty(capturedContext.Name);
        Assert.Empty(capturedContext.MappingContextId);
    }

    [Theory]
    [InlineData("path", "path?handler=named-context")]
    [InlineData("", "?handler=named-context")]
    [InlineData("path/with/multiple/segments", "path/with/multiple/segments?handler=named-context")]
    [InlineData("path/with/multiple/segments?and=query", "path/with/multiple/segments?and=query&handler=named-context")]
    [InlineData("path/with/multiple/segments?and=query#hashtoo", "path/with/multiple/segments?and=query&handler=named-context")]
    [InlineData("path/with/#multiple/segments?and=query#hashtoo", "path/with/?handler=named-context")]
    [InlineData("path/with/multiple/segments#hashtoo?and=query", "path/with/multiple/segments?handler=named-context")]
    public void GeneratesCorrect_MappingContextId_ForNamedMappers(string url, string expectedMappingContextId)
    {
        FormMappingContext capturedContext = null;
        RenderFragment<FormMappingContext> contents = (ctx) => b => { capturedContext = ctx; };
        _navigationManager.NavigateTo(_navigationManager.ToAbsoluteUri(url).ToString());

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<FormMappingScope>(0);
            builder.AddAttribute(1, nameof(FormMappingScope.Name), "named-context");
            builder.AddAttribute(2, nameof(FormMappingScope.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal(expectedMappingContextId, capturedContext.MappingContextId);
    }

    [Fact]
    public void FormMappingScope_CanProvideName()
    {
        FormMappingContext capturedContext = null;
        RenderFragment<FormMappingContext> contents = (ctx) => b => { capturedContext = ctx; };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<FormMappingScope>(0);
            builder.AddAttribute(1, nameof(FormMappingScope.Name), "named-context");
            builder.AddAttribute(2, nameof(FormMappingScope.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("named-context", capturedContext.Name);
        Assert.Equal("path?query=value&handler=named-context", capturedContext.MappingContextId);
    }

    [Fact]
    public void FormMappingScope_CanNestNamedContexts()
    {
        FormMappingContext capturedContext = null;
        RenderFragment<FormMappingContext> contents = (ctx) => b => { capturedContext = ctx; };
        RenderFragment<FormMappingContext> nested = (ctx) => b =>
        {
            b.OpenComponent<FormMappingScope>(0);
            b.AddAttribute(1, nameof(FormMappingScope.Name), "child-context");
            b.AddAttribute(2, nameof(FormMappingScope.ChildContent), contents);
            b.CloseComponent();
        };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<FormMappingScope>(0);
            builder.AddAttribute(1, nameof(FormMappingScope.Name), "parent-context");
            builder.AddAttribute(2, nameof(FormMappingScope.ChildContent), nested);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("parent-context.child-context", capturedContext.Name);
        Assert.Equal("path?query=value&handler=parent-context.child-context", capturedContext.MappingContextId);
    }

    [Fact]
    public void FormMappingScope_CanNestWithDefaultContext()
    {
        FormMappingContext capturedContext = null;
        RenderFragment<FormMappingContext> contents = (ctx) => b => { capturedContext = ctx; };
        RenderFragment<FormMappingContext> nested = (ctx) => b =>
        {
            b.OpenComponent<FormMappingScope>(0);
            b.AddAttribute(1, nameof(FormMappingScope.Name), "child-context");
            b.AddAttribute(2, nameof(FormMappingScope.ChildContent), contents);
            b.CloseComponent();
        };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<FormMappingScope>(0);
            builder.AddAttribute(2, nameof(FormMappingScope.ChildContent), nested);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("child-context", capturedContext.Name);
        Assert.Equal("path?query=value&handler=child-context", capturedContext.MappingContextId);
    }

    [Fact]
    public void Throws_IfDefaultContextIsNotTheRoot()
    {
        FormMappingContext capturedContext = null;
        RenderFragment<FormMappingContext> contents = (ctx) => b => { capturedContext = ctx; };
        RenderFragment<FormMappingContext> nested = (ctx) => b =>
        {
            b.OpenComponent<FormMappingScope>(0);
            b.AddAttribute(1, nameof(FormMappingScope.ChildContent), contents);
            b.CloseComponent();
        };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<FormMappingScope>(0);
            builder.AddAttribute(1, nameof(FormMappingScope.Name), "parent-context");
            builder.AddAttribute(2, nameof(FormMappingScope.ChildContent), nested);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => _renderer.RenderRootComponent(id));
        Assert.Equal("Nested form mapping contexts must define a Name. (Parent context) = 'parent-context'.", exception.Message);
    }

    [Fact]
    public void Throws_IfNameChanges()
    {
        FormMappingContext capturedContext = null;
        RenderFragment<FormMappingContext> contents = (ctx) => b => { capturedContext = ctx; };
        var contextName = "parent-context";

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<FormMappingScope>(0);
            builder.AddAttribute(1, nameof(FormMappingScope.Name), contextName);
            builder.AddAttribute(2, nameof(FormMappingScope.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);
        _renderer.RenderRootComponent(id);

        // Act
        contextName = "changed";
        var exception = Assert.Throws<InvalidOperationException>(testComponent.TriggerRender);

        Assert.Equal("FormMappingScope 'Name' can't change after initialization.", exception.Message);
    }

    private class RouteViewTestNavigationManager : NavigationManager
    {
        public RouteViewTestNavigationManager() =>
            Initialize("https://www.example.com/subdir/", "https://www.example.com/subdir/");

        public void NotifyLocationChanged(string uri)
        {
            Uri = uri;
            NotifyLocationChanged(false);
        }
    }

    class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://localhost:85/subdir/", "https://localhost:85/subdir/path?query=value#hash");
        }

        protected override void NavigateToCore([StringSyntax("Uri")] string uri, NavigationOptions options)
        {
            Uri = uri;
        }
    }

    class TestComponent : AutoRenderComponent
    {
        private readonly RenderFragment _renderFragment;

        public TestComponent(RenderFragment renderFragment)
        {
            _renderFragment = renderFragment;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => _renderFragment(builder);
    }

    private class TestFormValueMapper : IFormValueMapper
    {
        public bool CanMap(Type valueType, string formName = null) => false;
        public void Map(FormValueMappingContext context) { }
    }
}
