// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.Forms
{
    public class InputNumberTest
    {
        [Fact]
        public async Task ValidationErrorUsesDisplayAttributeName()
        {
            // Arrange
            var model = new TestModelWithDisplayProperty();
            var rootComponent = new TestInputHostComponent<int, TestInputNumberComponent>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NumberProperty
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.NumberProperty);
            var inputComponent = await Render.RenderAndGetTestInputComponentAsync(rootComponent);

            // Act
            await inputComponent.SetCurrentValueAsStringAsync("InvalidNumberValue");

            // Assert
            var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
            Assert.NotEmpty(validationMessages);
            Assert.Contains("The Height field must be a number.", validationMessages);
        }

        class TestModelWithDisplayProperty
        {
            [Display(Name = "Height")]
            public int NumberProperty { get; set; }
        }

        class TestInputNumberComponent : InputNumber<int>
        {
            // Expose protected members publicly for tests

            public new EditContext EditContext
            {
                get => base.EditContext;
                set { base.EditContext = value; }
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
    }
}
