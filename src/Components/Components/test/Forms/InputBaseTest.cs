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
        public async Task ThrowsIfEditContextChanges()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string> { EditContext = new EditContext(model), ValueExpression = () => model.StringProperty };
            await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act/Assert
            rootComponent.EditContext = new EditContext(model);
            var ex = Assert.Throws<InvalidOperationException>(() => rootComponent.TriggerRender());
            Assert.StartsWith($"{typeof(TestInputComponent<string>)} does not support changing the EditContext dynamically", ex.Message);
        }

        [Fact]
        public async Task ThrowsIfNoValueExpressionIsSupplied()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string> { EditContext = new EditContext(model) };

            // Act/Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RenderAndGetTestInputComponentAsync(rootComponent));
            Assert.Contains($"{typeof(TestInputComponent<string>)} requires a value for the 'ValueExpression' parameter. Normally this is provided automatically when using 'bind-Value'.", ex.Message);
        }

        [Fact]
        public async Task GetsCurrentValueFromValueParameter()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string>
            {
                EditContext = new EditContext(model),
                Value = "some value",
                ValueExpression = () => model.StringProperty
            };

            // Act
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Assert
            Assert.Equal("some value", inputComponent.CurrentValue);
        }

        [Fact]
        public async Task ExposesEditContextToSubclass()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string>
            {
                EditContext = new EditContext(model),
                Value = "some value",
                ValueExpression = () => model.StringProperty
            };

            // Act
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Assert
            Assert.Same(rootComponent.EditContext, inputComponent.EditContext);
        }

        [Fact]
        public async Task ExposesFieldIdentifierToSubclass()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string>
            {
                EditContext = new EditContext(model),
                Value = "some value",
                ValueExpression = () => model.StringProperty
            };

            // Act
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Assert
            Assert.Equal(FieldIdentifier.Create(() => model.StringProperty), inputComponent.FieldIdentifier);
        }

        [Fact]
        public async Task CanReadBackChangesToCurrentValue()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string>
            {
                EditContext = new EditContext(model),
                Value = "initial value",
                ValueExpression = () => model.StringProperty
            };
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);
            Assert.Equal("initial value", inputComponent.CurrentValue);

            // Act
            inputComponent.CurrentValue = "new value";

            // Assert
            Assert.Equal("new value", inputComponent.CurrentValue);
        }

        [Fact]
        public async Task WritingToCurrentValueInvokesValueChangedIfDifferent()
        {
            // Arrange
            var model = new TestModel();
            var valueChangedCallLog = new List<string>();
            var rootComponent = new TestInputHostComponent<string>
            {
                EditContext = new EditContext(model),
                Value = "initial value",
                ValueChanged = val => valueChangedCallLog.Add(val),
                ValueExpression = () => model.StringProperty
            };
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);
            Assert.Empty(valueChangedCallLog);

            // Act
            inputComponent.CurrentValue = "new value";

            // Assert
            Assert.Single(valueChangedCallLog, "new value");
        }

        [Fact]
        public async Task WritingToCurrentValueDoesNotInvokeValueChangedIfUnchanged()
        {
            // Arrange
            var model = new TestModel();
            var valueChangedCallLog = new List<string>();
            var rootComponent = new TestInputHostComponent<string>
            {
                EditContext = new EditContext(model),
                Value = "initial value",
                ValueChanged = val => valueChangedCallLog.Add(val),
                ValueExpression = () => model.StringProperty
            };
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);
            Assert.Empty(valueChangedCallLog);

            // Act
            inputComponent.CurrentValue = "initial value";

            // Assert
            Assert.Empty(valueChangedCallLog);
        }

        [Fact]
        public async Task WritingToCurrentValueNotifiesEditContext()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string>
            {
                EditContext = new EditContext(model),
                Value = "initial value",
                ValueExpression = () => model.StringProperty
            };
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);
            Assert.False(rootComponent.EditContext.IsModified(() => model.StringProperty));

            // Act
            inputComponent.CurrentValue = "new value";

            // Assert
            Assert.True(rootComponent.EditContext.IsModified(() => model.StringProperty));
        }

        [Fact]
        public async Task SuppliesCssClassCorrespondingToFieldState()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.StringProperty
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.StringProperty);

            // Act/Assert: Initally, it's valid and unmodified
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);
            Assert.Equal("valid", inputComponent.CssClass);

            // Act/Assert: Modify the field
            rootComponent.EditContext.NotifyFieldChanged(fieldIdentifier);
            Assert.Equal("modified valid", inputComponent.CssClass);

            // Act/Assert: Make it invalid
            var messages = new ValidationMessageStore(rootComponent.EditContext);
            messages.Add(fieldIdentifier, "I do not like this value");
            Assert.Equal("modified invalid", inputComponent.CssClass);

            // Act/Assert: Clear the modification flag
            rootComponent.EditContext.MarkAsUnmodified(fieldIdentifier);
            Assert.Equal("invalid", inputComponent.CssClass);

            // Act/Assert: Make it valid
            messages.Clear();
            Assert.Equal("valid", inputComponent.CssClass);
        }

        private static TestInputComponent<TValue> FindInputComponent<TValue>(CapturedBatch batch)
            => batch.ReferenceFrames
                    .Where(f => f.FrameType == RenderTreeFrameType.Component)
                    .Select(f => f.Component)
                    .OfType<TestInputComponent<TValue>>()
                    .Single();

        private static async Task<TestInputComponent<TValue>> RenderAndGetTestInputComponentAsync<TValue>(TestInputHostComponent<TValue> hostComponent)
        {
            var testRenderer = new TestRenderer();
            var componentId = testRenderer.AssignRootComponentId(hostComponent);
            await testRenderer.RenderRootComponentAsync(componentId);
            return FindInputComponent<TValue>(testRenderer.Batches.Single());
        }

        class TestModel
        {
            public string StringProperty { get; set; }
        }

        class TestInputComponent<T> : InputBase<T>
        {
            // Expose protected members publicly for tests

            public new T CurrentValue
            {
                get => base.CurrentValue;
                set { base.CurrentValue = value; }
            }

            public new EditContext EditContext => base.EditContext;

            public new FieldIdentifier FieldIdentifier => base.FieldIdentifier;

            public new string CssClass => base.CssClass;
        }

        class TestInputHostComponent<T> : AutoRenderComponent
        {
            public EditContext EditContext { get; set; }

            public T Value { get; set; }

            public Action<T> ValueChanged { get; set; }

            public Expression<Func<T>> ValueExpression { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<EditContext>>(0);
                builder.AddAttribute(1, "Value", EditContext);
                builder.AddAttribute(2, RenderTreeBuilder.ChildContent, new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<TestInputComponent<T>>(0);
                    childBuilder.AddAttribute(0, "Value", Value);
                    childBuilder.AddAttribute(1, "ValueChanged", ValueChanged);
                    childBuilder.AddAttribute(2, "ValueExpression", ValueExpression);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }
    }
}
