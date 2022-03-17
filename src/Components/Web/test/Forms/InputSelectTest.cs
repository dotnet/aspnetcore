// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class InputSelectTest
{
    [Fact]
    public async Task ParsesCurrentValueWhenUsingNotNullableEnumWithNotEmptyValue()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

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
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

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
        var rootComponent = new TestInputHostComponent<TestEnum?, TestInputSelect<TestEnum?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableEnum
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

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
        var rootComponent = new TestInputHostComponent<TestEnum?, TestInputSelect<TestEnum?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableEnum
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

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
        var rootComponent = new TestInputHostComponent<Guid, TestInputSelect<Guid>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableGuid
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

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
        var rootComponent = new TestInputHostComponent<Guid?, TestInputSelect<Guid?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableGuid
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

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
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

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
        var rootComponent = new TestInputHostComponent<int?, TestInputSelect<int?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableInt
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        inputSelectComponent.CurrentValueAsString = "42";

        // Assert
        Assert.Equal(42, inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task ValidationErrorUsesDisplayAttributeName()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt,
            AdditionalAttributes = new Dictionary<string, object>
                {
                    { "DisplayName", "Some number" }
                }
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableInt);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputSelectComponent.SetCurrentValueAsStringAsync("invalidNumber");

        // Assert
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
        Assert.Contains("The Some number field is not valid.", validationMessages);
    }

    [Fact]
    public async Task InputElementIsAssignedSuccessfully()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt,
        };

        // Act
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        var exception = Record.Exception(() => inputSelectComponent.Element);

        // Assert
        Assert.NotNull(inputSelectComponent.Element);
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
