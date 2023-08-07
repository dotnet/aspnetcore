// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class FormMappingScopeTest
{
    private readonly TestRenderer _renderer;

    public FormMappingScopeTest()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IFormValueMapper, TestFormValueMapper>();
        var services = serviceCollection.BuildServiceProvider();
        _renderer = new TestRenderer(services);
    }

    [Fact]
    public void SuppliesMappingContext()
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
        Assert.Equal("named-context", capturedContext.MappingScopeName);
    }

    [Fact]
    public void CanNestToOverride()
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
        Assert.Equal("child-context", capturedContext.MappingScopeName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ThrowsIfEmptyOrNullNameGiven(string name)
    {
        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<FormMappingScope>(0);
            builder.AddAttribute(1, nameof(FormMappingScope.Name), name);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => _renderer.RenderRootComponent(id));
        Assert.Equal($"The FormMappingScope component requires a nonempty Name parameter value.", exception.Message);
    }

    [Fact]
    public void ThrowsIfNoNameGiven()
    {
        var testComponent = new TestComponent(builder =>
        {
            builder.OpenComponent<FormMappingScope>(0);
            builder.CloseComponent();
        });
        var id = _renderer.AssignRootComponentId(testComponent);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => _renderer.RenderRootComponent(id));
        Assert.Equal($"The FormMappingScope component requires a nonempty Name parameter value.", exception.Message);
    }

    [Fact]
    public void ThrowsIfNameChanges()
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
        public bool CanMap(Type valueType, string mappingScopeName, string formName) => false;
        public void Map(FormValueMappingContext context) { }
    }
}
