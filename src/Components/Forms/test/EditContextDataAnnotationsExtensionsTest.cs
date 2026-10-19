// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file intentionally tests the obsolete synchronous EditContext.Validate() API.
#pragma warning disable CS0618 // Type or member is obsolete

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

    [Fact]
    public void ValidatesHiddenPropertiesWithoutAmbiguousMatchException()
    {
        var model = new DerivedModelWithHiddenProperty { OrderID = 150 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        Assert.False(editContext.Validate());
        Assert.Equal(new[] { "OrderID:range" }, editContext.GetValidationMessages());

        var orderIdIdentifier = new FieldIdentifier(model, nameof(DerivedModelWithHiddenProperty.OrderID));
        editContext.NotifyFieldChanged(orderIdIdentifier);
        model.OrderID = 50;
        editContext.NotifyFieldChanged(orderIdIdentifier);
        Assert.Empty(editContext.GetValidationMessages());
    }

    [Fact]
    public void ValidatesHiddenPropertiesWithPropertyCaching()
    {
        var model = new DerivedModelWithHiddenProperty { OrderID = 150 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);
        var orderIdIdentifier = new FieldIdentifier(model, nameof(DerivedModelWithHiddenProperty.OrderID));

        var sequence = new[] { 150, 50, 200, 75, 99, 101, 1 };
        var expected = new[] { "OrderID:range" };
        foreach (var value in sequence)
        {
            model.OrderID = value;
            editContext.NotifyFieldChanged(orderIdIdentifier);
            var expectedMessages = (value < 1 || value > 100) ? expected : Array.Empty<string>();
            Assert.Equal(expectedMessages, editContext.GetValidationMessages());
        }
    }

    [Fact]
    public void MatchesPropertyByExactName()
    {
        var model = new DerivedModelWithHiddenProperty { OrderID = 150 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        var field = new FieldIdentifier(model, "OrderID");
        editContext.NotifyFieldChanged(field);
        Assert.Equal(new[] { "OrderID:range" }, editContext.GetValidationMessages());
    }

    [Fact]
    public void ValidatesInheritedPropertyFromBaseClass()
    {
        var model = new DerivedModelWithInheritedOnly { Description = "x" };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        var field = new FieldIdentifier(model, nameof(DerivedModelWithInheritedOnly.BaseName));
        editContext.NotifyFieldChanged(field);
        Assert.Equal(new[] { "BaseName:required" }, editContext.GetValidationMessages());

        model.BaseName = "ok";
        editContext.NotifyFieldChanged(field);
        Assert.Empty(editContext.GetValidationMessages());
    }

    [Fact]
    public void ValidatesPropertyHiddenAtMultipleInheritanceLevels()
    {
        var model = new DeepDerivedModel { Tag = 150 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        var field = new FieldIdentifier(model, nameof(DeepDerivedModel.Tag));
        editContext.NotifyFieldChanged(field);
        Assert.Equal(new[] { "Tag:range" }, editContext.GetValidationMessages());

        model.Tag = 5;
        editContext.NotifyFieldChanged(field);
        Assert.Empty(editContext.GetValidationMessages());
    }

    [Fact]
    public void SkipsValidationWhenDerivedShadowHasNoAttributes()
    {
        var model = new DerivedModelWithUnattributedHiddenProperty { Name = null };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        var field = new FieldIdentifier(model, nameof(DerivedModelWithUnattributedHiddenProperty.Name));
        editContext.NotifyFieldChanged(field);
        Assert.Empty(editContext.GetValidationMessages());
    }

    [Fact]
    public void IgnoresStaticProperty()
    {
        var model = new ModelWithStaticProperty { Value = 0 };
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        var field = new FieldIdentifier(model, nameof(ModelWithStaticProperty.StaticValue));
        editContext.NotifyFieldChanged(field);
        Assert.Empty(editContext.GetValidationMessages());
    }

    [Fact]
    public Task FormLevelAsyncValidationProducesMessages() => RunOnDispatcher(async () =>
    {
        var model = new AsyncTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        var isValid = await editContext.ValidateAsync();

        Assert.False(isValid);
        Assert.Equal(new[] { "AsyncString:asyncnonempty" }, editContext.GetValidationMessages());
    });

    [Fact]
    public void FormLevelAsyncOnlyValidation_ValidateInvokesSyncFallbackAndThrows()
    {
        // Synchronous Validate() runs DataAnnotations through the synchronous Validator.TryValidateObject,
        // which invokes the synchronous IsValid fallback of an async-only attribute. AsyncNonEmptyAttribute's
        // fallback is not supported, so the exception it throws propagates out of Validate().
        var model = new AsyncTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);

        var ex = Assert.Throws<NotSupportedException>(() => editContext.Validate());
        Assert.Contains("only supports asynchronous validation", ex.Message);
    }

    [Fact]
    public Task FieldLevelAsyncValidationBecomesPendingThenSettles() => RunOnDispatcher(async () =>
    {
        var model = new AsyncTestModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);
        var field = editContext.Field(nameof(AsyncTestModel.AsyncString));

        editContext.NotifyFieldChanged(field);

        Assert.True(editContext.IsValidationPending(field));
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.False(editContext.IsValidationFaulted(field));
        Assert.Equal(new[] { "AsyncString:asyncnonempty" }, editContext.GetValidationMessages(field));
    });

    [Fact]
    public Task FieldLevelAsyncValidationThrowing_MarksFieldFaulted() => RunOnDispatcher(async () =>
    {
        var model = new AsyncThrowingModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(_serviceProvider);
        var field = editContext.Field(nameof(AsyncThrowingModel.ThrowingString));

        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.True(editContext.IsValidationFaulted(field));
    });

    private static Task RunOnDispatcher(Func<Task> body)
        => Dispatcher.CreateDefault().InvokeAsync(body);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start > timeout)
            {
                throw new TimeoutException("The expected condition was not reached before the timeout.");
            }

            await Task.Yield();
        }
    }

    private sealed class AsyncNonEmptyAttribute : AsyncValidationAttribute
    {
        protected override async Task<ValidationResult> IsValidAsync(object value, ValidationContext validationContext, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return value is string s && s.Length > 0
                ? ValidationResult.Success
                : new ValidationResult($"{validationContext.MemberName}:asyncnonempty", new[] { validationContext.MemberName });
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            => throw new NotSupportedException("This attribute only supports asynchronous validation.");
    }

    private sealed class AsyncThrowingAttribute : AsyncValidationAttribute
    {
        protected override async Task<ValidationResult> IsValidAsync(object value, ValidationContext validationContext, CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("Async validation failed");
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            => throw new NotSupportedException("This attribute only supports asynchronous validation.");
    }

    private sealed class AsyncTestModel
    {
        [AsyncNonEmpty] public string AsyncString { get; set; }
    }

    private sealed class AsyncThrowingModel
    {
        [AsyncThrowing] public string ThrowingString { get; set; }
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

    class DerivedModelWithHiddenProperty : ModelWithHiddenBaseProperty
    {
        [Range(1, 100, ErrorMessage = "OrderID:range")]
        public new int OrderID { get; set; }
    }

    class ModelWithHiddenBaseProperty
    {
        public object OrderID { get; set; }

        public object Tag { get; set; }
    }

    class MidLevelModelWithShadow : ModelWithHiddenBaseProperty
    {
        public new string Tag { get; set; }
    }

    class DeepDerivedModel : MidLevelModelWithShadow
    {
        [Range(1, 100, ErrorMessage = "Tag:range")]
        public new int Tag { get; set; }
    }

    class DerivedModelWithUnattributedHiddenProperty : ModelWithNamedBase
    {
        public new string Name { get; set; }
    }

    class ModelWithNamedBase
    {
        [Required(ErrorMessage = "Name:required")]
        public object Name { get; set; }
    }

    class ModelWithStaticProperty
    {
        [Range(1, 100, ErrorMessage = "StaticValue:range")]
        public static int StaticValue { get; set; }

        public int Value { get; set; }
    }

    class DerivedModelWithInheritedOnly : ModelWithBaseName
    {
        public string Description { get; set; }
    }

    class ModelWithBaseName
    {
        [Required(ErrorMessage = "BaseName:required")]
        public string BaseName { get; set; }
    }
}
