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
		var exception = Record.Exception(() => _ = inputSelectComponent.Element);
		
        // Assert
        Assert.Null(exception);
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

    public static IEnumerable<object[]> AdditionalAttributesTestData =>
        new List<object[]>
        {
            new object[] { new Dictionary<string, object> { { "class", "custom-select-class another-class" } } },
            new object[] { new Dictionary<string, object> { { "data-testid", "my-select" }, { "data-required", "true" }, { "data-custom-value", "some-value" } } },
            new object[] { new Dictionary<string, object> { { "aria-label", "Select an option" }, { "aria-describedby", "help-text" }, { "aria-required", "true" } } }
        };

    [Theory]
    [MemberData(nameof(AdditionalAttributesTestData))]
    public async Task AdditionalAttributes_AreRenderedOnElement(Dictionary<string, object> attributes)
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum,
            AdditionalAttributes = attributes
        };

        var componentId = await RenderAndGetInputSelectComponentIdAsync(rootComponent);
        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        foreach (var kvp in attributes)
        {
            var attributeFrame = frames.Array.FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == kvp.Key);
            Assert.NotEqual(default, attributeFrame);

            if (kvp.Key == "class")
            {
                Assert.Contains(kvp.Value.ToString(), attributeFrame.AttributeValue?.ToString());
            }
            else
            {
                Assert.Equal(kvp.Value, attributeFrame.AttributeValue);
            }
        }
    }

    [Fact]
    public async Task InvalidEnumValueDoesNotChangeCurrentValueAndAddsValidationError()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableEnum);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputSelectComponent.SetCurrentValueAsStringAsync("NotARealEnumValue");

        Assert.Equal(default, inputSelectComponent.CurrentValue);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
    }

    [Fact]
    public async Task InvalidIntValueDoesNotChangeCurrentValueAndAddsValidationError()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableInt);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputSelectComponent.SetCurrentValueAsStringAsync("abc");

        Assert.Equal(default, inputSelectComponent.CurrentValue);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
    }

    [Fact]
    public async Task NullableIntWithEmptyValueBecomesNull()
    {
        var model = new TestModel { NullableInt = 10 };
        var rootComponent = new TestInputHostComponent<int?, TestInputSelect<int?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableInt
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        inputSelectComponent.CurrentValueAsString = "";

        Assert.Null(inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task NotNullableIntWithEmptyValueKeepsLastValidValueAndReportsValidationError()
    {
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

        await inputSelectComponent.SetCurrentValueAsStringAsync("");

        Assert.Equal(99, inputSelectComponent.CurrentValue);
        var validationMessages = rootComponent.EditContext.GetValidationMessages(fieldIdentifier);
        Assert.NotEmpty(validationMessages);
    }

    [Fact]
    public async Task NullableGuidWithEmptyValueBecomesNull()
    {
        var model = new TestModel { NullableGuid = Guid.NewGuid() };
        var rootComponent = new TestInputHostComponent<Guid?, TestInputSelect<Guid?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableGuid
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        inputSelectComponent.CurrentValueAsString = "";

        Assert.Null(inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task NullableBoolWithEmptyStringBecomesNull()
    {
        var model = new TestModelWithBool { NullableBool = true };
        var rootComponent = new TestInputHostComponent<bool?, TestInputSelect<bool?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableBool
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        inputSelectComponent.CurrentValueAsString = "";

        Assert.Null(inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task NullableBoolParseTrueAndFalseStrings()
    {
        var model = new TestModelWithBool();
        var rootComponent = new TestInputHostComponent<bool?, TestInputSelect<bool?>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NullableBool
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        inputSelectComponent.CurrentValueAsString = "true";
        Assert.Equal(true, inputSelectComponent.CurrentValue);

        inputSelectComponent.CurrentValueAsString = "false";
        Assert.Equal(false, inputSelectComponent.CurrentValue);

        inputSelectComponent.CurrentValueAsString = "";
        Assert.Null(inputSelectComponent.CurrentValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MultipleAttribute_RendersBasedOnSelectType(bool isMultiSelect)
    {
        int componentId;
        if (isMultiSelect)
        {
            var model = new TestModelWithArray();
            var rootComponent = new TestInputHostComponent<string[], TestInputSelect<string[]>>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.StringArray,
                Value = model.StringArray
            };
            componentId = await RenderAndGetInputSelectComponentIdAsync(rootComponent);
        }
        else
        {
            var model = new TestModel();
            var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NotNullableEnum
            };
            componentId = await RenderAndGetInputSelectComponentIdAsync(rootComponent);
        }

        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);

        if (isMultiSelect)
        {
            var multipleAttribute = frames.Array
                .FirstOrDefault(f => f.FrameType == RenderTreeFrameType.Attribute && f.AttributeName == "multiple");
            Assert.NotEqual(default, multipleAttribute);
            Assert.True((bool)multipleAttribute.AttributeValue);
        }
        else
        {
            var hasMultipleTrue = frames.Array
                .Any(f => f.FrameType == RenderTreeFrameType.Attribute
                       && f.AttributeName == "multiple"
                       && f.AttributeValue is true);
            Assert.False(hasMultipleTrue);
        }
    }

    [Fact]
    public async Task ChangingValueMultipleTimesAlwaysReflectsLatestValue()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

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
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt
        };
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        inputSelectComponent.CurrentValueAsString = "7";
        inputSelectComponent.CurrentValueAsString = "7";

        Assert.Equal(7, inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task ValidValueAfterInvalidClearsValidationErrors()
    {
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

        await inputSelectComponent.SetCurrentValueAsStringAsync("5");

        Assert.Empty(rootComponent.EditContext.GetValidationMessages(fieldIdentifier));
        Assert.Equal(5, inputSelectComponent.CurrentValue);
    }

    [Fact]
    public async Task InvalidInputMarksFieldAsModified()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<int, TestInputSelect<int>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableInt
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableInt);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputSelectComponent.SetCurrentValueAsStringAsync("notANumber");

        Assert.True(rootComponent.EditContext.IsModified(fieldIdentifier));
    }

    [Fact]
    public async Task ValidInputMarksFieldAsModified()
    {
        var model = new TestModel();
        var rootComponent = new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
        {
            EditContext = new EditContext(model),
            ValueExpression = () => model.NotNullableEnum
        };
        var fieldIdentifier = FieldIdentifier.Create(() => model.NotNullableEnum);
        var inputSelectComponent = await InputRenderer.RenderAndGetComponent(rootComponent);

        await inputSelectComponent.SetCurrentValueAsStringAsync("Two");

        Assert.True(rootComponent.EditContext.IsModified(fieldIdentifier));
    }

    [Fact]
    public async Task RendersOnchangeEventHandlerForBinding()
    {
        var model = new TestModel { NotNullableEnum = TestEnum.One };
        var componentId = await RenderAndGetInputSelectComponentIdAsync(
            new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NotNullableEnum
            });

        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);
        var onchangeAttribute = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Attribute &&
            f.AttributeName == "onchange");

        Assert.True(onchangeAttribute.FrameType == RenderTreeFrameType.Attribute);
        Assert.True(onchangeAttribute.AttributeEventHandlerId > 0, "Event handler ID should be valid for event dispatch");
    }

    [Fact]
    public async Task SelectElementIsProperlyRendered()
    {
        var model = new TestModel { NotNullableEnum = TestEnum.Two };
        var componentId = await RenderAndGetInputSelectComponentIdAsync(
            new TestInputHostComponent<TestEnum, TestInputSelect<TestEnum>>
            {
                EditContext = new EditContext(model),
                ValueExpression = () => model.NotNullableEnum
            });

        var frames = _testRenderer.GetCurrentRenderTreeFrames(componentId);
        var selectElement = frames.Array.FirstOrDefault(f =>
            f.FrameType == RenderTreeFrameType.Element &&
            f.ElementName == "select");

        Assert.True(selectElement.FrameType == RenderTreeFrameType.Element,
            "InputSelect must render a <select> element");
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
