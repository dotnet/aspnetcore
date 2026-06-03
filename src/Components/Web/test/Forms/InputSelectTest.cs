// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputSelectTest
{
    private readonly TestRenderer _testRenderer = new TestRenderer();

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

    [Fact]
    public async Task RendersIdAttribute()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum,
        };

        var componentId = await RenderAndGetInputSelectComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.Single(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("model_NotNullableEnum", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task ExplicitIdOverridesGenerated()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum,
            AdditionalAttributes = new Dictionary<string, object> { { "id", "custom-select-id" } }
        };

        var componentId = await RenderAndGetInputSelectComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        var idAttribute = frames.Array.First(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "id");
        Assert.Equal("custom-select-id", idAttribute.AttributeValue);
    }

    [Fact]
    public async Task InvalidEnumValueDoesNotChangeCurrentValueAndAddsValidationError()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableEnum);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputSelectComponent.SetCurrentValueAsStringAsync("NotARealEnumValue");

        // Assert
        Assert.Equal(default, inputSelectComponent.CurrentValue);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
    }

    [Fact]
    public async Task InvalidGuidValueDoesNotChangeCurrentValueAndAddsValidationError()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<Guid, TestInputSelect<Guid>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableGuid
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableGuid);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputSelectComponent.SetCurrentValueAsStringAsync("not-a-valid-guid");

        // Assert
        Assert.Equal(default, inputSelectComponent.CurrentValue);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
    }

    [Fact]
    public async Task InvalidIntValueDoesNotChangeCurrentValueAndAddsValidationError()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableInt);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputSelectComponent.SetCurrentValueAsStringAsync("abc");

        // Assert
        Assert.Equal(default, inputSelectComponent.CurrentValue);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
    }

    [Fact]
    public async Task NullableEnumWithWhitespaceValueProducesValidationError()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum?, TestInputSelect<TestEnum?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableEnum
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NullableEnum);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputSelectComponent.SetCurrentValueAsStringAsync("   ");

        // Assert
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
    }

    [Fact]
    public async Task NullableIntWithEmptyValueBecomesNull()
    {
        // Arrange
        var model = new TestModel { NullableInt = 10 };
        var rootComponent = new TestInputHostComponent<int?, TestInputSelect<int?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableInt
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        inputSelectComponent.CurrentValueAsString = "";

        // Assert
        Assert.Null(inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task NotNullableIntWithEmptyValueKeepsLastValidValue()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableInt);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);
        await inputSelectComponent.SetCurrentValueAsStringAsync("99");
        Assert.Equal(99, inputSelectComponent.CurrentValue);

        // Act
        await inputSelectComponent.SetCurrentValueAsStringAsync("");

        // Assert
        Assert.Equal(99, inputSelectComponent.CurrentValue);
        Assert.NotEmpty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
    }

    [Fact]
    public async Task NullableGuidWithEmptyValueBecomesNull()
    {
        // Arrange
        var model = new TestModel { NullableGuid = Guid.NewGuid() };
        var rootComponent = new TestInputHostComponent<Guid?, TestInputSelect<Guid?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableGuid
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        inputSelectComponent.CurrentValueAsString = "";

        // Assert
        Assert.Null(inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task NullableBoolWithEmptyStringBecomesNull()
    {
        // Arrange
        var model = new TestModelWithBool { NullableBool = true };
        var rootComponent = new TestInputHostComponent<bool?, TestInputSelect<bool?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableBool
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        inputSelectComponent.CurrentValueAsString = "";

        // Assert
        Assert.Null(inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task NullableBoolParseTrueAndFalseStrings()
    {
        // Arrange
        var model = new TestModelWithBool();
        var rootComponent = new TestInputHostComponent<bool?, TestInputSelect<bool?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableBool
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act & Assert
        inputSelectComponent.CurrentValueAsString = "true";
        Assert.Equal(true, inputSelectComponent.CurrentValue);

        // Act & Assert
        inputSelectComponent.CurrentValueAsString = "false";
        Assert.Equal(false, inputSelectComponent.CurrentValue);

        // Act & Assert
        inputSelectComponent.CurrentValueAsString = "";
        Assert.Null(inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task MultiSelectWithStringArrayRendersMultipleAttribute()
    {
        // Arrange
        var model = new TestModelWithArray();
        var rootComponent = new TestInputHostComponent<string[], TestInputSelect<string[]>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.StringArray,
            Value = model.StringArray   // must be non-null so BindConverter doesn't throw
        };

        var componentId = await RenderAndGetInputSelectComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        var multipleAttribute = frames.Array
            .FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "multiple");
        Assert.NotEqual(default, multipleAttribute);
        Assert.True((bool)multipleAttribute.AttributeValue);
    }

    [Fact]
    public async Task SingleSelectDoesNotRenderMultipleAttribute()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum
        };

        var componentId = await RenderAndGetInputSelectComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        // Assert
        var hasMultiple = frames.Array
            .Any(f => f.FrameType == RenderTreeFrameType.Attribute
                   && f.AttributeName == "multiple"
                   && f.AttributeValue is true);
        Assert.False(hasMultiple);
    }

    [Fact]
    public async Task ChangingValueMultipleTimesAlwaysReflectsLatestValue()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act & Assert
        inputSelectComponent.CurrentValueAsString = "One";
        Assert.Equal(TestEnum.One, inputSelectComponent.CurrentValue);

        inputSelectComponent.CurrentValueAsString = "Two";
        Assert.Equal(TestEnum.Two, inputSelectComponent.CurrentValue);

        inputSelectComponent.CurrentValueAsString = "Tree";
        Assert.Equal(TestEnum.Tree, inputSelectComponent.CurrentValue);

        inputSelectComponent.CurrentValueAsString = "One";
        Assert.Equal(TestEnum.One, inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task ReSelectingSameValueDoesNotCorruptState()
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
        inputSelectComponent.CurrentValueAsString = "7";
        inputSelectComponent.CurrentValueAsString = "7";

        // Assert
        Assert.Equal(7, inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task ValidValueAfterInvalidClearsValidationErrors()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableInt);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputSelectComponent.SetCurrentValueAsStringAsync("bad");
        Assert.NotEmpty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));

        // Act
        await inputSelectComponent.SetCurrentValueAsStringAsync("5");

        // Assert – validation errors are cleared
        Assert.Empty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
        Assert.Equal(5, inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task InvalidInputMarksFieldAsModified()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableInt);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputSelectComponent.SetCurrentValueAsStringAsync("notANumber");

        // Assert
        Assert.True(rootComponent.EditContext.IsModified(fieldIdentifier));
    }

    [Fact]
    public async Task ValidInputMarksFieldAsModified()
    {
        // Arrange
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableEnum);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        // Act
        await inputSelectComponent.SetCurrentValueAsStringAsync("Two");

        // Assert
        Assert.True(rootComponent.EditContext.IsModified(fieldIdentifier));
    }

    private async Task<int> RenderAndGetInputSelectComponentIdAsync<TValue>(TestInputHostComponent<TValue, TestInputSelect<TValue>> hostComponent)
    {
        var hostComponentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(hostComponentId);
        var batch = _testRenderer.Batches.Single();
        return batch.GetComponentFrames<TestInputSelect<TValue>>().Single().ComponentId;
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

    class TestModelWithBool
    {
        public bool NotNullableBool { get; set; }

        public bool? NullableBool { get; set; }
    }

    class TestModelWithArray
    {
        public string[] StringArray { get; set; } = Array.Empty<string>();
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
