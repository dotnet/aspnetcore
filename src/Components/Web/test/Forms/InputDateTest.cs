// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.Forms
{
    public class InputDateTest
    {
        [Fact]
        public async Task ValidationErrorUsesDisplayAttributeName()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.DateProperty,
                AdditionalAttributes = new Dictionary<string, object>
                {
                    { "DisplayName", "Date property" }
                }
            };
            var fieldIdentifier = FieldIdentifier.Create(() => model.DateProperty);
            var inputComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

            // Act
            await inputComponent.SetCurrentValueAsStringAsync("invalidDate");

            // Assert
            var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
            Assert.NotEmpty(validationMessages);
            Assert.Contains("The Date property field must be a date.", validationMessages);
        }

        [Fact]
        public async Task InputElementIsAssignedSuccessfully()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<DateTime, TestInputDateComponent>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.DateProperty,
            };

            // Act
            var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

            // Assert
            Assert.NotNull(inputSelectComponent.Element);
        }

        private class TestModel
        {
            public DateTime DateProperty { get; set; }
        }

        private class TestInputDateComponent : InputDate<DateTime>
        {
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
