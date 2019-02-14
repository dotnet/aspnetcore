// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.Tests.Forms
{
    public class InputBaseTest
    {
        [Fact]
        public async Task ThrowsOnFirstRenderIfNoEditContextIsSupplied()
        {
            // Arrange
            var inputComponent = new TestInputComponent<string>();
            var testRenderer = new TestRenderer();
            var componentId = testRenderer.AssignRootComponentId(inputComponent);
            
            // Act/Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => testRenderer.RenderRootComponentAsync(componentId));
            Assert.StartsWith($"{typeof(TestInputComponent<string>)} requires a cascading parameter of type {nameof(EditContext)}", ex.Message);
        }

        [Fact]
        public void ThrowsIfEditContextChanges()
        {
            // Arrange
            var context = new TestRenderingContext<TestInputComponent<string>>();
            var model = new TestModel();
            context.SupplyParameters(new EditContext(model), valueExpression: () => model.StringProperty);

            // Act/Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                context.SupplyParameters(new EditContext(model), valueExpression: () => model.StringProperty);
            });
            Assert.StartsWith($"{typeof(TestInputComponent<string>)} does not support changing the EditContext dynamically", ex.Message);
        }

        [Fact]
        public void ThrowsIfNoValueExpressionIsSupplied()
        {
            // Arrange
            var context = new TestRenderingContext<TestInputComponent<string>>();
            var model = new TestModel();

            // Act/Assert
            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                context.SupplyParameters<object>(new EditContext(model), valueExpression: null);
            });
            Assert.Contains($"{typeof(TestInputComponent<string>)} requires a value for the 'ValueExpression' parameter. Normally this is provided automatically when using 'bind-Value'.", ex.Message);
        }

        [Fact]
        public void GetsCurrentValueFromValueParameter()
        {
            // Arrange
            var context = new TestRenderingContext<TestInputComponent<string>>();
            var model = new TestModel();

            // Act
            context.SupplyParameters(new EditContext(model), value: "some value", valueExpression: () => model.StringProperty);

            // Assert
            Assert.Equal("some value", context.Component.RenderedStates.Single().CurrentValue);
        }

        [Fact]
        public void ExposesEditContextToSubclass()
        {
            // Arrange
            var context = new TestRenderingContext<TestInputComponent<string>>();
            var model = new TestModel();
            var editContext = new EditContext(model);

            // Act
            context.SupplyParameters(editContext, valueExpression: () => model.StringProperty);

            // Assert
            Assert.Same(editContext, context.Component.EditContext);
        }

        [Fact]
        public void ExposesFieldIdentifierToSubclass()
        {
            // Arrange
            var context = new TestRenderingContext<TestInputComponent<string>>();
            var model = new TestModel();
            var editContext = new EditContext(model);

            // Act
            context.SupplyParameters(editContext, valueExpression: () => model.StringProperty);

            // Assert
            Assert.Equal(FieldIdentifier.Create(() => model.StringProperty), context.Component.FieldIdentifier);
        }

        [Fact]
        public void CanReadBackChangesToCurrentValue()
        {
            // Arrange
            var context = new TestRenderingContext<TestInputComponent<string>>();
            var model = new TestModel();
            context.SupplyParameters(new EditContext(model), value: "some value", valueExpression: () => model.StringProperty);
            Assert.Single(context.Component.RenderedStates);

            // Act
            context.Component.CurrentValue = "new value";

            // Assert
            Assert.Equal("new value", context.Component.CurrentValue);
            Assert.Single(context.Component.RenderedStates); // Writing to CurrentValue doesn't inherently trigger a render (though the fact that it invokes ValueChanged might)
        }

        [Fact]
        public void WritingToCurrentValueInvokesValueChangedIfDifferent()
        {
            // Arrange
            var context = new TestRenderingContext<TestInputComponent<string>>();
            var model = new TestModel();
            var valueChangedCallLog = new List<string>();
            Action<string> valueChanged = val => valueChangedCallLog.Add(val);
            context.SupplyParameters(new EditContext(model), valueChanged: valueChanged, valueExpression: () => model.StringProperty);
            Assert.Single(context.Component.RenderedStates);

            // Act
            context.Component.CurrentValue = "new value";

            // Assert
            Assert.Single(valueChangedCallLog, "new value");
        }

        [Fact]
        public void WritingToCurrentValueDoesNotInvokeValueChangedIfUnchanged()
        {
            // Arrange
            var context = new TestRenderingContext<TestInputComponent<string>>();
            var model = new TestModel();
            var valueChangedCallLog = new List<string>();
            Action<string> valueChanged = val => valueChangedCallLog.Add(val);
            context.SupplyParameters(new EditContext(model), value: "initial value", valueChanged: valueChanged, valueExpression: () => model.StringProperty);
            Assert.Single(context.Component.RenderedStates);

            // Act
            context.Component.CurrentValue = "initial value";

            // Assert
            Assert.Empty(valueChangedCallLog);
        }

        [Fact]
        public void WritingToCurrentValueNotifiesEditContext()
        {
            // Arrange
            var context = new TestRenderingContext<TestInputComponent<string>>();
            var model = new TestModel();
            var editContext = new EditContext(model);
            context.SupplyParameters(editContext, valueExpression: () => model.StringProperty);
            Assert.False(editContext.IsModified(() => model.StringProperty));

            // Act
            context.Component.CurrentValue = "new value";

            // Assert
            Assert.True(editContext.IsModified(() => model.StringProperty));
        }

        class TestModel
        {
            public string StringProperty { get; set; }

            public string AnotherStringProperty { get; set; }
        }

        class TestInputComponent<T> : InputBase<T> where T: IEquatable<T>
        {
            public List<StateWhenRendering> RenderedStates { get; } = new List<StateWhenRendering>();

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                // No need to actually render anything. We just want to assert about what data is given to derived classes.
                RenderedStates.Add(new StateWhenRendering { CurrentValue = CurrentValue });
            }

            public class StateWhenRendering
            {
                public T CurrentValue { get; set; }
            }

            // Expose publicly for tests
            public new T CurrentValue
            {
                get => base.CurrentValue;
                set { base.CurrentValue = value; }
            }

            public new EditContext EditContext => base.EditContext;

            public new FieldIdentifier FieldIdentifier => base.FieldIdentifier;
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

        class TestRenderingContext<TComponent> where TComponent: IComponent
        {
            private readonly TestRenderer _renderer = new TestRenderer();
            private readonly TestComponent _rootComponent;
            private RenderFragment _renderFragment;

            public TestRenderingContext()
            {
                _rootComponent = new TestComponent(builder => builder.AddContent(0, _renderFragment));
            }

            public TComponent Component { get; private set; }

            public void SupplyParameters<T>(EditContext editContext, T value = default, Action<T> valueChanged = default, Expression<Func<T>> valueExpression = default)
            {
                _renderFragment = builder =>
                {
                    builder.OpenComponent<CascadingValue<EditContext>>(0);
                    builder.AddAttribute(1, "Value", editContext);
                    builder.AddAttribute(2, RenderTreeBuilder.ChildContent, new RenderFragment(childBuilder =>
                    {
                        childBuilder.OpenComponent<TComponent>(0);
                        childBuilder.AddAttribute(0, "Value", value);
                        childBuilder.AddAttribute(1, "ValueChanged", valueChanged);
                        childBuilder.AddAttribute(2, "ValueExpression", valueExpression);
                        childBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                };

                if (Component == null)
                {
                    var rootComponentId = _renderer.AssignRootComponentId(_rootComponent);
                    var renderTask = _renderer.RenderRootComponentAsync(rootComponentId);
                    if (renderTask.IsFaulted)
                    {
                        throw renderTask.Exception;
                    }
                    Assert.True(renderTask.IsCompletedSuccessfully); // Everything's synchronous here

                    var batch = _renderer.Batches.Single();
                    Component = batch.ReferenceFrames
                        .Where(f => f.FrameType == RenderTreeFrameType.Component)
                        .Select(f => f.Component)
                        .OfType<TComponent>()
                        .Single();
                }
                else
                {
                    _rootComponent.TriggerRender();
                }
            }
        }
    }
}
