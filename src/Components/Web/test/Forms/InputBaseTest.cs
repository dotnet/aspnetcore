// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Components.Forms
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
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>> { EditContext = new EditContext(model), ValueExpression = () => model.StringProperty };
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
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>> { EditContext = new EditContext(model) };

            // Act/Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RenderAndGetTestInputComponentAsync(rootComponent));
            Assert.Contains($"{typeof(TestInputComponent<string>)} requires a value for the 'ValueExpression' parameter. Normally this is provided automatically when using 'bind-Value'.", ex.Message);
        }

        [Fact]
        public async Task GetsCurrentValueFromValueParameter()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
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
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
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
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
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
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
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
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
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
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
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
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
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
        public async Task SuppliesFieldClassCorrespondingToFieldState()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.StringProperty
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.StringProperty);

            // Act/Assert: Initially, it's valid and unmodified
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);
            Assert.Equal("valid", inputComponent.CssClass); //  no Class was specified

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

        [Fact]
        public async Task CssClassCombinesClassWithFieldClass()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
            {
                AdditionalAttributes = new Dictionary<string, object>()
                {
                    { "class", "my-class other-class" },
                },
                EditContext = new EditContext(model),
                ValueExpression = () => model.StringProperty
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.StringProperty);

            // Act/Assert
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);
            Assert.Equal("my-class other-class valid", inputComponent.CssClass);

            // Act/Assert: Retains custom class when changing field class
            rootComponent.EditContext.NotifyFieldChanged(fieldIdentifier);
            Assert.Equal("my-class other-class modified valid", inputComponent.CssClass);
        }

        [Fact]
        public async Task SuppliesCurrentValueAsStringWithFormatting()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<DateTime, TestDateInputComponent>
            {
                EditContext = new EditContext(model),
                Value = new DateTime(1915, 3, 2),
                ValueExpression = () => model.DateProperty
            };
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act/Assert
            Assert.Equal("1915/03/02", inputComponent.CurrentValueAsString);
        }

        [Fact]
        public async Task ParsesCurrentValueAsStringWhenChanged_Valid()
        {
            // Arrange
            var model = new TestModel();
            var valueChangedArgs = new List<DateTime>();
            var rootComponent = new TestInputHostComponent<DateTime, TestDateInputComponent>
            {
                EditContext = new EditContext(model),
                ValueChanged = valueChangedArgs.Add,
                ValueExpression = () => model.DateProperty
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);
            var numValidationStateChanges = 0;
            rootComponent.EditContext.OnValidationStateChanged += (sender, eventArgs) => { numValidationStateChanges++; };

            // Act
            await inputComponent.SetCurrentValueAsStringAsync("1991/11/20");

            // Assert
            var receivedParsedValue = valueChangedArgs.Single();
            Assert.Equal(1991, receivedParsedValue.Year);
            Assert.Equal(11, receivedParsedValue.Month);
            Assert.Equal(20, receivedParsedValue.Day);
            Assert.True(rootComponent.EditContext.IsModified(fieldIdentifier));
            Assert.Empty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
            Assert.Equal(0, numValidationStateChanges);
        }

        [Fact]
        public async Task ParsesCurrentValueAsStringWhenChanged_Invalid()
        {
            // Arrange
            var model = new TestModel();
            var valueChangedArgs = new List<DateTime>();
            var rootComponent = new TestInputHostComponent<DateTime, TestDateInputComponent>
            {
                EditContext = new EditContext(model),
                ValueChanged = valueChangedArgs.Add,
                ValueExpression = () => model.DateProperty
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
            var inputComponent = await RenderAndGetTestInputComponentAsync(rootComponent);
            var numValidationStateChanges = 0;
            rootComponent.EditContext.OnValidationStateChanged += (sender, eventArgs) => { numValidationStateChanges++; };

            // Act/Assert 1: Transition to invalid
            await inputComponent.SetCurrentValueAsStringAsync("1991/11/40");
            Assert.Empty(valueChangedArgs);
            Assert.True(rootComponent.EditContext.IsModified(fieldIdentifier));
            Assert.Equal(new[] { "Bad date value" }, rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
            Assert.Equal(1, numValidationStateChanges);

            // Act/Assert 2: Transition to valid
            await inputComponent.SetCurrentValueAsStringAsync("1991/11/20");
            var receivedParsedValue = valueChangedArgs.Single();
            Assert.Equal(1991, receivedParsedValue.Year);
            Assert.Equal(11, receivedParsedValue.Month);
            Assert.Equal(20, receivedParsedValue.Day);
            Assert.True(rootComponent.EditContext.IsModified(fieldIdentifier));
            Assert.Empty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
            Assert.Equal(2, numValidationStateChanges);
        }

        [Fact]
        public async Task RespondsToValidationStateChangeNotifications()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.StringProperty
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.StringProperty);
            var renderer = new TestRenderer();
            var rootComponentId = renderer.AssignRootComponentId(rootComponent);
            await renderer.RenderRootComponentAsync(rootComponentId);

            // Initally, it rendered one batch and is valid
            var batch1 = renderer.Batches.Single();
            var componentFrame1 = batch1.GetComponentFrames<TestInputComponent<string>>().Single();
            var inputComponentId = componentFrame1.ComponentId;
            var component = (TestInputComponent<string>)componentFrame1.Component;
            Assert.Equal("valid", component.CssClass);

            // Act: update the field state in the EditContext and notify
            var messageStore = new ValidationMessageStore(rootComponent.EditContext);
            messageStore.Add(fieldIdentifier, "Some message");
            await renderer.Dispatcher.InvokeAsync(rootComponent.EditContext.NotifyValidationStateChanged);

            // Assert: The input component rendered itself again and now has the new class
            var batch2 = renderer.Batches.Skip(1).Single();
            Assert.Equal(inputComponentId, batch2.DiffsByComponentId.Keys.Single());
            Assert.Equal("invalid", component.CssClass);
        }

        [Fact]
        public async Task UnsubscribesFromValidationStateChangeNotifications()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string, TestInputComponent<string>>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.StringProperty
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.StringProperty);
            var renderer = new TestRenderer();
            var rootComponentId = renderer.AssignRootComponentId(rootComponent);
            await renderer.RenderRootComponentAsync(rootComponentId);
            var component = renderer.Batches.Single().GetComponentFrames<TestInputComponent<string>>().Single().Component;

            // Act: dispose, then update the field state in the EditContext and notify
            ((IDisposable)component).Dispose();
            var messageStore = new ValidationMessageStore(rootComponent.EditContext);
            messageStore.Add(fieldIdentifier, "Some message");
            await renderer.Dispatcher.InvokeAsync(rootComponent.EditContext.NotifyValidationStateChanged);

            // Assert: No additional render
            Assert.Empty(renderer.Batches.Skip(1));
        }

        private static TComponent FindComponent<TComponent>(CapturedBatch batch)
            => batch.ReferenceFrames
                    .Where(f => f.FrameType == RenderTreeFrameType.Component)
                    .Select(f => f.Component)
                    .OfType<TComponent>()
                    .Single();

        private static async Task<TComponent> RenderAndGetTestInputComponentAsync<TValue, TComponent>(TestInputHostComponent<TValue, TComponent> hostComponent) where TComponent : TestInputComponent<TValue>
        {
            var testRenderer = new TestRenderer();
            var componentId = testRenderer.AssignRootComponentId(hostComponent);
            await testRenderer.RenderRootComponentAsync(componentId);
            return FindComponent<TComponent>(testRenderer.Batches.Single());
        }

        class TestModel
        {
            public string StringProperty { get; set; }

            public DateTime DateProperty { get; set; }
        }

        class TestInputComponent<T> : InputBase<T>
        {
            // Expose protected members publicly for tests

            public new T CurrentValue
            {
                get => base.CurrentValue;
                set { base.CurrentValue = value; }
            }

            public new string CurrentValueAsString
            {
                get => base.CurrentValueAsString;
            }

            public new IReadOnlyDictionary<string, object> AdditionalAttributes => base.AdditionalAttributes;

            public new string CssClass => base.CssClass;

            public new EditContext EditContext => base.EditContext;

            public new FieldIdentifier FieldIdentifier => base.FieldIdentifier;

            protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage)
            {
                throw new NotImplementedException();
            }

            public async Task SetCurrentValueAsStringAsync(string value)
            {
                // This is equivalent to the subclass writing to CurrentValueAsString
                // (e.g., from @bind), except to simplify the test code there's an InvokeAsync
                // here. In production code it wouldn't normally be required because @bind
                // calls run on the sync context anyway.
                await InvokeAsync(() => { base.CurrentValueAsString = value; });
            }
        }

        class TestDateInputComponent : TestInputComponent<DateTime>
        {
            protected override string FormatValueAsString(DateTime value)
                => value.ToString("yyyy/MM/dd");

            protected override bool TryParseValueFromString(string value, out DateTime result, out string validationErrorMessage)
            {
                if (DateTime.TryParse(value, out result))
                {
                    validationErrorMessage = null;
                    return true;
                }
                else
                {
                    validationErrorMessage = "Bad date value";
                    return false;
                }
            }
        }

        class TestInputHostComponent<TValue, TComponent> : AutoRenderComponent where TComponent : TestInputComponent<TValue>
        {
            public Dictionary<string, object> AdditionalAttributes { get; set; }

            public EditContext EditContext { get; set; }

            public TValue Value { get; set; }

            public Action<TValue> ValueChanged { get; set; }

            public Expression<Func<TValue>> ValueExpression { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<EditContext>>(0);
                builder.AddAttribute(1, "Value", EditContext);
                builder.AddAttribute(2, "ChildContent", new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<TComponent>(0);
                    childBuilder.AddAttribute(0, "Value", Value);
                    childBuilder.AddAttribute(1, "ValueChanged",
                        EventCallback.Factory.Create(this, ValueChanged));
                    childBuilder.AddAttribute(2, "ValueExpression", ValueExpression);
                    childBuilder.AddMultipleAttributes(3, AdditionalAttributes);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }
    }
}
