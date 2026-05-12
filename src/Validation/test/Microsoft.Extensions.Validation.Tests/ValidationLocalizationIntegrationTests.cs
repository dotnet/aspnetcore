#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Tests;

/// <summary>
/// Tests that the validation pipeline (ValidatablePropertyInfo, ValidatableParameterInfo,
/// ValidatableTypeInfo) integrates correctly with <see cref="IValidationLocalizer"/> set on
/// <see cref="ValidationOptions.Localizer"/>. Uses a recording localizer test double to verify
/// the helper invokes the localizer with the right context.
/// </summary>
public class ValidationLocalizationIntegrationTests
{
    // --- No-localizer path ---

    [Fact]
    public async Task Property_NoLocalizer_UsesAttributeDefaults()
    {
        var model = new SimpleModel { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel),
        [
            new TestValidatablePropertyInfo(typeof(SimpleModel), typeof(string), "Name",
                [new RequiredAttribute()])
        ]);
        var context = CreateContext(model, localizer: null);

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].Single());
    }

    [Fact]
    public async Task Property_NoLocalizer_LiteralDisplayNamePassesThrough()
    {
        var model = new SimpleModel { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel),
        [
            new TestValidatablePropertyInfo(typeof(SimpleModel), typeof(string), "Name",
                [new RequiredAttribute()],
                displayName: "Customer Name")
        ]);
        var context = CreateContext(model, localizer: null);

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Customer Name field is required.", context.ValidationErrors["Name"].Single());
    }

    // --- Localizer is invoked ---

    [Fact]
    public async Task Property_WithLocalizer_BothMethodsCalled()
    {
        var localizer = new RecordingValidationLocalizer
        {
            DisplayNameResult = "Localized Display",
            ErrorMessageResult = "Localized error: Localized Display",
        };

        var model = new SimpleModel { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel),
        [
            new TestValidatablePropertyInfo(typeof(SimpleModel), typeof(string), "Name",
                [new RequiredAttribute()],
                displayName: "Customer Name")
        ]);
        var context = CreateContext(model, localizer);

        await typeInfo.ValidateAsync(model, context, default);

        // ResolveDisplayName called with the property's literal display name and declaring type
        var displayCall = Assert.Single(localizer.DisplayNameCalls);
        Assert.Equal("Customer Name", displayCall.DisplayName);
        Assert.Equal("Name", displayCall.MemberName);
        Assert.Equal(typeof(SimpleModel), displayCall.DeclaringType);

        // ResolveErrorMessage called with the resolved display name (passed back into context)
        var errorCall = Assert.Single(localizer.ErrorMessageCalls);
        Assert.Equal("Localized Display", errorCall.DisplayName);
        Assert.Equal("Name", errorCall.MemberName);
        Assert.Equal(typeof(SimpleModel), errorCall.DeclaringType);
        Assert.IsType<RequiredAttribute>(errorCall.Attribute);

        // The localizer's ErrorMessage result is used as the validation error
        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Localized error: Localized Display", context.ValidationErrors["Name"].Single());
    }

    [Fact]
    public async Task Property_LocalizerReturnsNull_FallsBackToLiteral()
    {
        var localizer = new RecordingValidationLocalizer
        {
            DisplayNameResult = null,
            ErrorMessageResult = null,
        };
        var model = new SimpleModel { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel),
        [
            new TestValidatablePropertyInfo(typeof(SimpleModel), typeof(string), "Name",
                [new RequiredAttribute()],
                displayName: "Customer Name")
        ]);
        var context = CreateContext(model, localizer);

        await typeInfo.ValidateAsync(model, context, default);

        // When the localizer can't translate the literal, the LiteralDisplayName strategy
        // returns the literal as the fallback display name (it acts as both lookup key and
        // default value). The error message uses that literal.
        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Customer Name field is required.", context.ValidationErrors["Name"].Single());
    }

    // --- ErrorMessageResourceType bypass ---

    [Fact]
    public async Task Property_ErrorMessageResourceType_BypassesLocalizer()
    {
        var localizer = new RecordingValidationLocalizer
        {
            ErrorMessageResult = "Should not be used",
        };
        var requiredAttr = new RequiredAttribute
        {
            ErrorMessageResourceType = typeof(TestResources),
            ErrorMessageResourceName = nameof(TestResources.RequiredError),
        };

        var model = new SimpleModel { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel),
        [
            new TestValidatablePropertyInfo(typeof(SimpleModel), typeof(string), "Name", [requiredAttr],
                displayName: "Customer Name")
        ]);
        var context = CreateContext(model, localizer);

        await typeInfo.ValidateAsync(model, context, default);

        // ResolveDisplayName is still called (the display name itself isn't part of the bypass)
        Assert.Single(localizer.DisplayNameCalls);
        // But ResolveErrorMessage is NOT called when ErrorMessageResourceType is set
        Assert.Empty(localizer.ErrorMessageCalls);
        // The attribute's resource-resolved message is used
        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(TestResources.RequiredError, context.ValidationErrors["Name"].Single());
    }

    // --- Resource-attribute strategy bypasses the IStringLocalizer path ---

    [Fact]
    public async Task Property_ResourceDisplayName_BypassesLocalizer()
    {
        var localizer = new RecordingValidationLocalizer
        {
            DisplayNameResult = "Should not be used",
        };
        var model = new SimpleModel { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel),
        [
            new TestValidatablePropertyInfo(typeof(SimpleModel), typeof(string), "Name",
                [new RequiredAttribute()],
                displayResourceAccessor: () => "Resource-Resolved Name")
        ]);
        var context = CreateContext(model, localizer);

        await typeInfo.ValidateAsync(model, context, default);

        // The strategy wins; the localizer's ResolveDisplayName is NOT called.
        Assert.Empty(localizer.DisplayNameCalls);
        // ResolveErrorMessage IS called, with the strategy's result as the display name.
        var errorCall = Assert.Single(localizer.ErrorMessageCalls);
        Assert.Equal("Resource-Resolved Name", errorCall.DisplayName);
    }

    [Fact]
    public async Task Property_ResourceDisplayName_ReturnsNull_FallsBackToMemberName()
    {
        var localizer = new RecordingValidationLocalizer();
        var model = new SimpleModel { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel),
        [
            new TestValidatablePropertyInfo(typeof(SimpleModel), typeof(string), "Name",
                [new RequiredAttribute()],
                displayResourceAccessor: () => null)
        ]);
        var context = CreateContext(model, localizer);

        await typeInfo.ValidateAsync(model, context, default);

        Assert.Empty(localizer.DisplayNameCalls);
        var errorCall = Assert.Single(localizer.ErrorMessageCalls);
        Assert.Equal("Name", errorCall.DisplayName);
    }

    [Fact]
    public async Task Property_ResourceDisplayName_Throws_PropagatesException()
    {
        // Pins the failure mode for misconfigured [Display(ResourceType=T, Name=X)] where X is not
        // a public static string property on T. The runtime accessor (DisplayAttribute.GetName)
        // throws InvalidOperationException with a clear BCL message; the validation pipeline does
        // not suppress it. Documented as user-error behaviour: the misconfiguration is surfaced
        // loudly rather than masked by the MemberName fallback.
        var thrown = new InvalidOperationException("Cannot retrieve property 'Name' because localization failed.");
        var model = new SimpleModel { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(SimpleModel),
        [
            new TestValidatablePropertyInfo(typeof(SimpleModel), typeof(string), "Name",
                [new RequiredAttribute()],
                displayResourceAccessor: () => throw thrown)
        ]);
        var context = CreateContext(model, localizer: null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => typeInfo.ValidateAsync(model, context, default));
        Assert.Same(thrown, ex);
    }

    // --- IValidatableObject results are not post-processed ---

    [Fact]
    public async Task IValidatableObject_ResultsNotProcessedThroughLocalizer()
    {
        var localizer = new RecordingValidationLocalizer
        {
            ErrorMessageResult = "Should not be applied to IValidatableObject results",
        };

        var model = new ValidatableObjectModel();
        var typeInfo = new TestValidatableTypeInfo(typeof(ValidatableObjectModel), []);
        var context = CreateContext(model, localizer);

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Custom IValidatableObject error", context.ValidationErrors["Name"].Single());
    }

    // --- Type-level validation attributes use type display name ---

    [Fact]
    public async Task TypeLevelAttribute_UsesTypeAsDeclaringType()
    {
        var localizer = new RecordingValidationLocalizer
        {
            ErrorMessageResult = "Localized type-level error",
        };
        var model = new RangeModel { Start = 10, End = 5 };
        var typeInfo = new TestValidatableTypeInfo(
            typeof(RangeModel),
            [
                new TestValidatablePropertyInfo(typeof(RangeModel), typeof(int), "Start", []),
                new TestValidatablePropertyInfo(typeof(RangeModel), typeof(int), "End", [])
            ],
            attributes: [new StartLessThanEndAttribute { ErrorMessage = "Start must be less than End." }]);
        var context = CreateContext(model, localizer);

        await typeInfo.ValidateAsync(model, context, default);

        // The error message localization for type-level attrs uses the type as DeclaringType
        var errorCall = Assert.Single(localizer.ErrorMessageCalls);
        Assert.Equal(typeof(RangeModel), errorCall.DeclaringType);
        Assert.IsType<StartLessThanEndAttribute>(errorCall.Attribute);
        Assert.NotNull(context.ValidationErrors);
        Assert.Contains("Localized type-level error", context.ValidationErrors.Values.SelectMany(v => v));
    }

    // --- Parameter-level validation passes declaringType: null ---

    [Fact]
    public async Task Parameter_LocalizerCalledWithNullDeclaringType()
    {
        var localizer = new RecordingValidationLocalizer
        {
            DisplayNameResult = "Localized Param",
            ErrorMessageResult = "Param required",
        };
        var paramInfo = new TestValidatableParameterInfo(
            typeof(string), "myParam", "Display Param",
            [new RequiredAttribute()]);
        var context = CreateContext(model: new object(), localizer);

        await paramInfo.ValidateAsync(null, context, default);

        var displayCall = Assert.Single(localizer.DisplayNameCalls);
        Assert.Null(displayCall.DeclaringType);
        Assert.Equal("Display Param", displayCall.DisplayName);
        Assert.Equal("myParam", displayCall.MemberName);

        var errorCall = Assert.Single(localizer.ErrorMessageCalls);
        Assert.Null(errorCall.DeclaringType);
        Assert.Equal("Localized Param", errorCall.DisplayName);
    }

    // --- Helpers and test doubles ---

    private static ValidateContext CreateContext(object model, IValidationLocalizer? localizer)
    {
        var options = new ValidationOptions { Localizer = localizer };
        return new ValidateContext
        {
            ValidationOptions = options,
            ValidationContext = new ValidationContext(model),
        };
    }

    /// <summary>
    /// Records every call into <see cref="IValidationLocalizer"/> so tests can assert what the
    /// pipeline passed through. Returns configurable static results.
    /// </summary>
    private sealed class RecordingValidationLocalizer : IValidationLocalizer
    {
        public List<DisplayNameLocalizationContext> DisplayNameCalls { get; } = [];
        public List<ErrorMessageLocalizationContext> ErrorMessageCalls { get; } = [];
        public string? DisplayNameResult { get; set; }
        public string? ErrorMessageResult { get; set; }

        public string? ResolveDisplayName(in DisplayNameLocalizationContext context)
        {
            DisplayNameCalls.Add(context);
            return DisplayNameResult;
        }

        public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context)
        {
            ErrorMessageCalls.Add(context);
            return ErrorMessageResult;
        }
    }

    private sealed class TestValidatablePropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _validationAttributes;

        public TestValidatablePropertyInfo(
            Type declaringType,
            Type propertyType,
            string name,
            ValidationAttribute[] validationAttributes,
            string? displayName = null,
            Func<string?>? displayResourceAccessor = null)
            : base(declaringType, propertyType, name, BuildDisplayNameInfo(displayName, displayResourceAccessor))
        {
            _validationAttributes = validationAttributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _validationAttributes;

        private static DisplayNameInfo? BuildDisplayNameInfo(string? displayName, Func<string?>? displayResourceAccessor)
        {
            // Resource-attribute path takes precedence (matches FormatPropertyDisplayNameInfo in the SG).
            if (displayResourceAccessor is not null)
            {
                return new TestResourceDisplayName(displayResourceAccessor);
            }

            if (displayName is not null)
            {
                return new TestLiteralDisplayName(displayName);
            }

            return null;
        }
    }

    private sealed class TestValidatableParameterInfo(
        Type parameterType,
        string name,
        string? displayName,
        ValidationAttribute[] validationAttributes)
        : ValidatableParameterInfo(parameterType, name, displayName is null ? null : new TestLiteralDisplayName(displayName))
    {
        protected override ValidationAttribute[] GetValidationAttributes() => validationAttributes;
    }

    private sealed class SimpleModel
    {
        public string? Name { get; set; }
    }

    private sealed class RangeModel
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    private sealed class ValidatableObjectModel : IValidatableObject
    {
        public string? Name { get; set; } = "Test";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield return new ValidationResult("Custom IValidatableObject error", ["Name"]);
        }
    }

    private sealed class StartLessThanEndAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is RangeModel model && model.Start >= model.End)
            {
                return new ValidationResult(ErrorMessage ?? "Start must be less than End.", [nameof(RangeModel.Start)]);
            }
            return ValidationResult.Success;
        }
    }

    internal static class TestResources
    {
        public static string RequiredError => "Resource: This field is required.";
    }
}
