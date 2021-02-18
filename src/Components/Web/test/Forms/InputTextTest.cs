// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.Forms
{
    public class InputTextTest
    {
        [Fact]
        public async Task InputElementIsAssignedSuccessfully()
        {
            // Arrange
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<string, InputText>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.StringProperty,
            };

            // Act
            var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

            // Assert
            Assert.NotNull(inputSelectComponent.Element);
        }

        private class TestModel
        {
            public string StringProperty { get; set; }
        }
    }
}
