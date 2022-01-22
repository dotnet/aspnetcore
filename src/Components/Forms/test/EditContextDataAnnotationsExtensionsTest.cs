// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

public class EditContextDataAnnotationsExtensionsTest
{
    private static readonly IServiceProvider _serviceProvider = new TestServiceProvider();

    [Fact]
    public void CannotUseNullEditContext()
    {
        var editContext = (EditContext)null;
        var ex = Assert.Throws<ArgumentNullException>(() => editContext.EnableDataAnnotationsValidation(_serviceProvider));
        Assert.Equal("editContext", ex.ParamName);
    }

    [Fact]
    public void ObsoleteApiReturnsEditContextForChaining()
    {
        var editContext = new EditContext(new object());
#pragma warning disable 0618
        var returnValue = editContext.AddDataAnnotationsValidation();
#pragma warning restore 0618
        Assert.Same(editContext, returnValue);
    }

    [Fact]
    public void GetsValidationMessagesFromDataAnnotations()
    {
        // Arrange
        var model = new TestModel { IntFrom1To100 = 101 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        // Act
        var isValid = editContext.Validate();

        // Assert
        Assert.False(isValid);

        Assert.Equal(new string[]
            {
                    "RequiredString:required",
                    "IntFrom1To100:range"
            },
            editContext.GetValidationMessages());

        Assert.Equal(new string[] { "RequiredString:required" },
            editContext.GetValidationMessages(editContext.Field(nameof(TestModel.RequiredString))));

        // This shows we're including non-[Required] properties in the validation results, i.e,
        // that we're correctly passing "validateAllProperties: true" to DataAnnotations
        Assert.Equal(new string[] { "IntFrom1To100:range" },
            editContext.GetValidationMessages(editContext.Field(nameof(TestModel.IntFrom1To100))));
    }

    [Fact]
    public void ClearsExistingValidationMessagesOnFurtherRuns()
    {
        // Arrange
        var model = new TestModel { IntFrom1To100 = 101 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        // Act/Assert 1: Initially invalid
        Assert.False(editContext.Validate());

        // Act/Assert 2: Can become valid
        model.RequiredString = "Hello";
        model.IntFrom1To100 = 100;
        Assert.True(editContext.Validate());
    }

    [Fact]
    public void NotifiesValidationStateChangedAfterObjectValidation()
    {
        // Arrange
        var model = new TestModel { IntFrom1To100 = 101 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);
        var onValidationStateChangedCount = 0;
        editContext.OnValidationStateChanged += (sender, eventArgs) => onValidationStateChangedCount++;

        // Act/Assert 1: Notifies after invalid results
        Assert.False(editContext.Validate());
        Assert.Equal(1, onValidationStateChangedCount);

        // Act/Assert 2: Notifies after valid results
        model.RequiredString = "Hello";
        model.IntFrom1To100 = 100;
        Assert.True(editContext.Validate());
        Assert.Equal(2, onValidationStateChangedCount);

        // Act/Assert 3: Notifies even if results haven't changed. Later we might change the
        // logic to track the previous results and compare with the new ones, but that's just
        // an optimization. It's legal to notify regardless.
        Assert.True(editContext.Validate());
        Assert.Equal(3, onValidationStateChangedCount);
    }

    [Fact]
    public void PerformsPerPropertyValidationOnFieldChange()
    {
        // Arrange
        var model = new TestModel { IntFrom1To100 = 101 };
        var independentTopLevelModel = new object(); // To show we can validate things on any model, not just the top-level one
        var editContext = new EditContext(independentTopLevelModel);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);
        var onValidationStateChangedCount = 0;
        var requiredStringIdentifier = new FieldIdentifier(model, nameof(TestModel.RequiredString));
        var intFrom1To100Identifier = new FieldIdentifier(model, nameof(TestModel.IntFrom1To100));
        editContext.OnValidationStateChanged += (sender, eventArgs) => onValidationStateChangedCount++;

        // Act/Assert 1: Notify about RequiredString
        // Only RequiredString gets validated, even though IntFrom1To100 also holds an invalid value
        editContext.NotifyFieldChanged(requiredStringIdentifier);
        Assert.Equal(1, onValidationStateChangedCount);
        Assert.Equal(new[] { "RequiredString:required" }, editContext.GetValidationMessages());

        // Act/Assert 2: Fix RequiredString, but only notify about IntFrom1To100
        // Only IntFrom1To100 gets validated; messages for RequiredString are left unchanged
        model.RequiredString = "This string is very cool and very legal";
        editContext.NotifyFieldChanged(intFrom1To100Identifier);
        Assert.Equal(2, onValidationStateChangedCount);
        Assert.Equal(new string[]
            {
                    "RequiredString:required",
                    "IntFrom1To100:range"
            },
            editContext.GetValidationMessages());

        // Act/Assert 3: Notify about RequiredString
        editContext.NotifyFieldChanged(requiredStringIdentifier);
        Assert.Equal(3, onValidationStateChangedCount);
        Assert.Equal(new[] { "IntFrom1To100:range" }, editContext.GetValidationMessages());
    }

    [Theory]
    [InlineData(nameof(TestModel.ThisWillNotBeValidatedBecauseItIsAField))]
    [InlineData(nameof(TestModel.ThisWillNotBeValidatedBecauseItIsInternal))]
    [InlineData("ThisWillNotBeValidatedBecauseItIsPrivate")]
    [InlineData("This does not correspond to anything")]
    [InlineData("")]
    public void IgnoresFieldChangesThatDoNotCorrespondToAValidatableProperty(string fieldName)
    {
        // Arrange
        var editContext = new EditContext(new TestModel());
        editContext.EnableDataAnnotationsValidation(_serviceProvider);
        var onValidationStateChangedCount = 0;
        editContext.OnValidationStateChanged += (sender, eventArgs) => onValidationStateChangedCount++;

        // Act/Assert: Ignores field changes that don't correspond to a validatable property
        editContext.NotifyFieldChanged(editContext.Field(fieldName));
        Assert.Equal(0, onValidationStateChangedCount);

        // Act/Assert: For sanity, observe that we would have validated if it was a validatable property
        editContext.NotifyFieldChanged(editContext.Field(nameof(TestModel.RequiredString)));
        Assert.Equal(1, onValidationStateChangedCount);
    }

    [Fact]
    public void CanDetachFromEditContext()
    {
        // Arrange
        var model = new TestModel { IntFrom1To100 = 101 };
        var editContext = new EditContext(model);
        var subscription = editContext.EnableDataAnnotationsValidation(_serviceProvider);

        // Act/Assert 1: when we're attached
        Assert.False(editContext.Validate());
        Assert.NotEmpty(editContext.GetValidationMessages());

        // Act/Assert 2: when we're detached
        subscription.Dispose();
        Assert.True(editContext.Validate());
        Assert.Empty(editContext.GetValidationMessages());
    }

    class TestModel
    {
        [Required(ErrorMessage = "RequiredString:required")] public string RequiredString { get; set; }

        [Range(1, 100, ErrorMessage = "IntFrom1To100:range")] public int IntFrom1To100 { get; set; }

#pragma warning disable 649
        [Required] public string ThisWillNotBeValidatedBecauseItIsAField;
        [Required] string ThisWillNotBeValidatedBecauseItIsPrivate { get; set; }
        [Required] internal string ThisWillNotBeValidatedBecauseItIsInternal { get; set; }
#pragma warning restore 649
    }
}
