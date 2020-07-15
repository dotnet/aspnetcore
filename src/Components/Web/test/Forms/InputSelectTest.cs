// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Components.Forms
{
    public class InputSelectTest
    {
        [Fact]
        public async Task ParsesCurrentValueWhenUsingNotNullableEnumWithNotEmptyValue()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputSelectHostComponent<TestEnum>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NotNullableEnum
            };
            var inputSelectComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act
            inputSelectComponent.CurrentValueAsString = "Two";

            // Assert
            Assert.Equal(TestEnum.Two, inputSelectComponent.CurrentValue);
        }

        [Fact]
        public async Task ParsesCurrentValueWhenUsingNotNullableEnumWithEmptyValue()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputSelectHostComponent<TestEnum>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NotNullableEnum
            };
            var inputSelectComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act
            inputSelectComponent.CurrentValueAsString = "";

            // Assert
            Assert.Equal(default, inputSelectComponent.CurrentValue);
        }

        [Fact]
        public async Task ParsesCurrentValueWhenUsingNullableEnumWithNotEmptyValue()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputSelectHostComponent<TestEnum?>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NullableEnum
            };
            var inputSelectComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act
            inputSelectComponent.CurrentValueAsString = "Two";

            // Assert
            Assert.Equal(TestEnum.Two, inputSelectComponent.Value);
        }

        [Fact]
        public async Task ParsesCurrentValueWhenUsingNullableEnumWithEmptyValue()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputSelectHostComponent<TestEnum?>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NullableEnum
            };
            var inputSelectComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act
            inputSelectComponent.CurrentValueAsString = "";

            // Assert
            Assert.Null(inputSelectComponent.CurrentValue);
        }

        // See: https://github.com/dotnet/aspnetcore/issues/9939
        [Fact]
        public async Task ParsesCurrentValueWhenUsingNotNullableGuid()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputSelectHostComponent<Guid>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NotNullableGuid
            };
            var inputSelectComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act
            var guid = Guid.NewGuid();
            inputSelectComponent.CurrentValueAsString = guid.ToString();

            // Assert
            Assert.Equal(guid, inputSelectComponent.CurrentValue);
        }

        // See: https://github.com/dotnet/aspnetcore/issues/9939
        [Fact]
        public async Task ParsesCurrentValueWhenUsingNullableGuid()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputSelectHostComponent<Guid?>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NullableGuid
            };
            var inputSelectComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act
            var guid = Guid.NewGuid();
            inputSelectComponent.CurrentValueAsString = guid.ToString();

            // Assert
            Assert.Equal(guid, inputSelectComponent.CurrentValue);
        }

        // See: https://github.com/dotnet/aspnetcore/pull/19562
        [Fact]
        public async Task ParsesCurrentValueWhenUsingNotNullableInt()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputSelectHostComponent<int>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NotNullableInt
            };
            var inputSelectComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act
            inputSelectComponent.CurrentValueAsString = "42";

            // Assert
            Assert.Equal(42, inputSelectComponent.CurrentValue);
        }

        // See: https://github.com/dotnet/aspnetcore/pull/19562
        [Fact]
        public async Task ParsesCurrentValueWhenUsingNullableInt()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputSelectHostComponent<int?>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NullableInt
            };
            var inputSelectComponent = await RenderAndGetTestInputComponentAsync(rootComponent);

            // Act
            inputSelectComponent.CurrentValueAsString = "42";

            // Assert
            Assert.Equal(42, inputSelectComponent.CurrentValue);
        }

        private static TestInputSelect<TValue> FindInputSelectComponent<TValue>(CapturedBatch batch)
            => batch.ReferenceFrames
                    .Where(f => f.FrameType == RenderTreeFrameType.Component)
                    .Select(f => f.Component)
                    .OfType<TestInputSelect<TValue>>()
                    .Single();

        private static async Task<TestInputSelect<TValue>> RenderAndGetTestInputComponentAsync<TValue>(TestInputSelectHostComponent<TValue> hostComponent)
        {
            var testRenderer = new TestRenderer();
            var componentId = testRenderer.AssignRootComponentId(hostComponent);
            await testRenderer.RenderRootComponentAsync(componentId);
            return FindInputSelectComponent<TValue>(testRenderer.Batches.Single());
        }

        enum TestEnum
        {
            One,
            Two,
            Tree
        }

        class TestModel
        {
            public TestEnum NotNullableEnum { get; set; }

            public TestEnum? NullableEnum { get; set; }

            public Guid NotNullableGuid { get; set; }

            public Guid? NullableGuid { get; set; }

            public int NotNullableInt { get; set; }

            public int? NullableInt { get; set; }
        }

        class TestInputSelect<TValue> : InputSelect<TValue>
        {
            public new TValue CurrentValue => base.CurrentValue;

            public new string CurrentValueAsString
            {
                get => base.CurrentValueAsString;
                set => base.CurrentValueAsString = value;
            }
        }

        class TestInputSelectHostComponent<TValue> : AutoRenderComponent
        {
            public EditContext EditContext { get; set; }

            public Expression<Func<TValue>> ValueExpression { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<EditContext>>(0);
                builder.AddAttribute(1, "Value", EditContext);
                builder.AddAttribute(2, "ChildContent", new RenderFragment(childBuilder =>
                {
                    childBuilder.OpenComponent<TestInputSelect<TValue>>(0);
                    childBuilder.AddAttribute(0, "ValueExpression", ValueExpression);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }
    }
}
