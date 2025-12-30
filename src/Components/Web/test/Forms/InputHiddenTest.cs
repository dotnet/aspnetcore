// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class InputHiddenTest
{
    [Fact]
    public async Task InputElementIsAssignedSuccessfully()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<string, InputHidden>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringProperty,
        };

        // Act
        var inputHiddenComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Assert
        Assert.NotNull(inputHiddenComponent.Element);
    }

    private class TestModel
    {
        public string StringProperty { get; set; }
    }
}
