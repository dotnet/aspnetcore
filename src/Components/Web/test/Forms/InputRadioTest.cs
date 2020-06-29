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
    public class InputRadioTest
    {
        [Theory]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task ThrowsOnFirstRenderIfInvalidNameSuppliedWithoutGroup(string name)
        {
            var model = new TestModel();
            var rootComponent = new TestInputRadioHostComponent<TestEnum>
            {
                EditContext = new EditContext(model),
                InnerContent = RadioButtonsWithoutGroup(name, () => model.TestEnum)
            };

            var testRenderer = new TestRenderer();
            var componentId = testRenderer.AssignRootComponentId(rootComponent);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => testRenderer.RenderRootComponentAsync(componentId));
            Assert.Contains($"requires either an explicit 'name' attribute", ex.Message);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GeneratesNameGuidWhenInvalidNameSuppliedWithGroup(string name)
        {
            var model = new TestModel();
            var rootComponent = new TestInputRadioHostComponent<TestEnum>
            {
                EditContext = new EditContext(model),
                InnerContent = RadioButtonsWithGroup(name, () => model.TestEnum)
            };

            var testRenderer = new TestRenderer();
            var componentId = testRenderer.AssignRootComponentId(rootComponent);
            await testRenderer.RenderRootComponentAsync(componentId);
            var inputRadioComponents = FindInputRadioComponents(testRenderer.Batches.Single());

            Assert.All(inputRadioComponents, inputRadio => Assert.True(Guid.TryParseExact(inputRadio.GroupName, "N", out _)));
        }

        [Theory]
        [MemberData(nameof(GetAllInputRadioGenerators))]
        public async Task NameAttributeExistsWhenValidNameSupplied(InputRadioGenerator generator)
        {
            string groupName = "group";

            var model = new TestModel();
            var rootComponent = new TestInputRadioHostComponent<TestEnum>
            {
                EditContext = new EditContext(model),
                InnerContent = generator(groupName, () => model.TestEnum)
            };

            var testRenderer = new TestRenderer();
            var componentId = testRenderer.AssignRootComponentId(rootComponent);
            await testRenderer.RenderRootComponentAsync(componentId);
            var inputRadioComponents = FindInputRadioComponents(testRenderer.Batches.Single());

            Assert.All(inputRadioComponents, inputRadio => Assert.Equal(groupName, inputRadio.GroupName));
        }

        public delegate RenderFragment InputRadioGenerator(string name, Expression<Func<TestEnum>> valueExpression);

        private static readonly InputRadioGenerator RadioButtonsWithoutGroup = (name, valueExpression) => (builder) =>
        {
            int sequence = 0;

            foreach (var selectedValue in (TestEnum[])Enum.GetValues(typeof(TestEnum)))
            {
                builder.OpenComponent<TestInputRadio>(sequence++);
                builder.AddAttribute(sequence++, "name", name);
                builder.AddAttribute(sequence++, "SelectedValue", selectedValue);
                builder.AddAttribute(sequence++, "ValueExpression", valueExpression);
                builder.CloseComponent();
            }
        };

        private static readonly InputRadioGenerator RadioButtonsWithGroup = (name, valueExpression) => (builder) =>
        {
            builder.OpenComponent<InputRadioGroup>(0);
            builder.AddAttribute(1, "Name", name);
            builder.AddAttribute(2, "ChildContent", new RenderFragment((childBuilder) =>
            {
                int sequence = 0;

                foreach (var selectedValue in (TestEnum[])Enum.GetValues(typeof(TestEnum)))
                {
                    childBuilder.OpenComponent<TestInputRadio>(sequence++);
                    childBuilder.AddAttribute(sequence++, "SelectedValue", selectedValue);
                    childBuilder.AddAttribute(sequence++, "ValueExpression", valueExpression);
                    childBuilder.CloseComponent();
                }
            }));

            builder.CloseComponent();
        };

        public static IEnumerable<object[]> GetAllInputRadioGenerators() => new[]
        {
            new[] { RadioButtonsWithoutGroup },
            new[] { RadioButtonsWithGroup }
        };

        private static IEnumerable<TestInputRadio> FindInputRadioComponents(CapturedBatch batch)
            => batch.ReferenceFrames
                    .Where(f => f.FrameType == RenderTreeFrameType.Component)
                    .Select(f => f.Component)
                    .OfType<TestInputRadio>();

        public enum TestEnum
        {
            One,
            Two,
            Three
        }

        private class TestModel
        {
            public TestEnum TestEnum { get; set; }
        }

        private class TestInputRadio : InputRadio<TestEnum>
        {
            public string GroupName => Name;
        }

        private class TestInputRadioHostComponent<TValue> : AutoRenderComponent
        {
            public EditContext EditContext { get; set; }

            public RenderFragment InnerContent { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<CascadingValue<EditContext>>(0);
                builder.AddAttribute(1, "Value", EditContext);
                builder.AddAttribute(2, "ChildContent", InnerContent);
                builder.CloseComponent();
            }
        }
    }
}
