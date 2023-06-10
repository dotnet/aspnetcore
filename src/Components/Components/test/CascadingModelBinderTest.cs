// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class CascadingModelBinderTest
{
    private readonly TestRenderer _renderer;
    private TestNavigationManager _navigationManager;

    public CascadingModelBinderTest()
    {
        var serviceCollection = new ServiceCollection();
        _navigationManager = new TestNavigationManager();
        serviceCollection.AddSingleton<NavigationManager>(_navigationManager);
        serviceCollection.AddSingleton<IFormValueSupplier, TestFormValueSupplier>();
        var services = serviceCollection.BuildServiceProvider();
        _renderer = new TestRenderer(services);
    }

    [Fact]
    public void CascadingModelBinder_NoBindingContextId_ForDefaultName()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Empty(capturedContext.Name);
        Assert.Empty(capturedContext.BindingContextId);
    }

    [Theory]
    [InlineData("path", "path?handler=named-context")]
    [InlineData("", "?handler=named-context")]
    [InlineData("path/with/multiple/segments", "path/with/multiple/segments?handler=named-context")]
    [InlineData("path/with/multiple/segments?and=query", "path/with/multiple/segments?and=query&handler=named-context")]
    [InlineData("path/with/multiple/segments?and=query#hashtoo", "path/with/multiple/segments?and=query&handler=named-context")]
    [InlineData("path/with/#multiple/segments?and=query#hashtoo", "path/with/?handler=named-context")]
    [InlineData("path/with/multiple/segments#hashtoo?and=query", "path/with/multiple/segments?handler=named-context")]
    public void GeneratesCorrect_BindingContextId_ForNamedBinders(string url, string expectedBindingContextId)
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        _navigationManager.NavigateTo(_navigationManager.ToAbsoluteUri(url).ToString());

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.Name), "named-context");
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal(expectedBindingContextId, capturedContext.BindingContextId);
    }

    [Fact]
    public void CascadingModelBinder_CanProvideName()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.Name), "named-context");
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("named-context", capturedContext.Name);
        Assert.Equal("path?query=value&handler=named-context", capturedContext.BindingContextId);
    }

    [Fact]
    public void CascadingModelBinder_CanNestNamedContexts()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        RenderFragment<ModelBindingContext> nested = (ctx) => b =>
        {
            b.OpenComponent<CascadingModelBinder>(0);
            b.AddAttribute(1, nameof(CascadingModelBinder.Name), "child-context");
            b.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            b.CloseComponent();
        };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.Name), "parent-context");
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), nested);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("parent-context.child-context", capturedContext.Name);
        Assert.Equal("path?query=value&handler=parent-context.child-context", capturedContext.BindingContextId);
    }

    [Fact]
    public void CascadingModelBinder_CanNestWithDefaultContext()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        RenderFragment<ModelBindingContext> nested = (ctx) => b =>
        {
            b.OpenComponent<CascadingModelBinder>(0);
            b.AddAttribute(1, nameof(CascadingModelBinder.Name), "child-context");
            b.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            b.CloseComponent();
        };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), nested);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        _renderer.RenderRootComponent(id);

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("child-context", capturedContext.Name);
        Assert.Equal("path?query=value&handler=child-context", capturedContext.BindingContextId);
    }

    [Fact]
    public void Throws_IfDefaultContextIsNotTheRoot()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        RenderFragment<ModelBindingContext> nested = (ctx) => b =>
        {
            b.OpenComponent<CascadingModelBinder>(0);
            b.AddAttribute(1, nameof(CascadingModelBinder.ChildContent), contents);
            b.CloseComponent();
        };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.Name), "parent-context");
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), nested);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => _renderer.RenderRootComponent(id));
        Assert.Equal("Nested binding contexts must define a Name. (Parent context) = 'parent-context'.", exception.Message);
    }

    [Fact]
    public void Throws_WhenIsFixedAndNameChanges()
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        var contextName = "parent-context";

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.Name), contextName);
            builder.AddAttribute(2, nameof(CascadingModelBinder.IsFixed), true);
            builder.AddAttribute(3, nameof(CascadingModelBinder.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);
        _renderer.RenderRootComponent(id);

        // Act
        contextName = "changed";
        var exception = Assert.Throws<InvalidOperationException>(testComponent.TriggerRender);

        Assert.Equal("'CascadingModelBinder' 'Name' can't change after initialized.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Throws_WhenIsFixed_Changes(bool isFixed)
    {
        ModelBindingContext capturedContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.IsFixed), isFixed);
            builder.AddAttribute(2, nameof(CascadingModelBinder.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);
        _renderer.RenderRootComponent(id);

        // Act
        isFixed = !isFixed;
        var exception = Assert.Throws<InvalidOperationException>(testComponent.TriggerRender);

        Assert.Equal("The value of IsFixed cannot be changed dynamically.", exception.Message);
    }

    [Fact]
    public void CanChange_Name_WhenNotFixed()
    {
        ModelBindingContext capturedContext = null;
        ModelBindingContext originalContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };
        var contextName = "parent-context";

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddAttribute(1, nameof(CascadingModelBinder.Name), contextName);
            builder.AddAttribute(3, nameof(CascadingModelBinder.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);
        _renderer.RenderRootComponent(id);

        originalContext = capturedContext;
        contextName = "changed";

        // Act
        testComponent.TriggerRender();

        Assert.NotSame(capturedContext, originalContext);
        Assert.Equal("changed", capturedContext.Name);
    }

    [Fact]
    public void CanChange_BindingContextId_WhenNotFixed()
    {
        ModelBindingContext capturedContext = null;
        ModelBindingContext originalContext = null;
        RenderFragment<ModelBindingContext> contents = (ctx) => b => { capturedContext = ctx; };

        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<CascadingModelBinder>(0);
            builder.AddComponentParameter(1, nameof(CascadingModelBinder.Name), "context-name");
            builder.AddComponentParameter(2, nameof(CascadingModelBinder.ChildContent), contents);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);
        _renderer.RenderRootComponent(id);

        originalContext = capturedContext;

        // Act
        _navigationManager.NavigateTo(_navigationManager.ToAbsoluteUri("fetch-data/6").ToString());
        testComponent.TriggerRender();

        Assert.NotSame(capturedContext, originalContext);
        Assert.Equal("fetch-data/6?handler=context-name", capturedContext.BindingContextId);
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

    private class TestFormValueSupplier : IFormValueSupplier
    {
        public bool CanBind(string formName, Type valueType)
        {
            return false;
        }

        public bool CanConvertSingleValue(Type type)
        {
            return false;
        }

        public bool TryBind(string formName, Type valueType, [NotNullWhen(true)] out object boundValue)
        {
            boundValue = null;
            return false;
        }
    }
}
