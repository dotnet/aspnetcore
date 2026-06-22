// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Endpoints.Forms;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.ClientValidation;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.FormValidation;

// Integration tests for the SSR client-validation rule pipeline:
// EndpointClientValidationProvider + ClientValidationCache. These exercise the real reflection,
// rule mapping, server-validation gating, and localization, driven by the set of fields an input
// rendered for (the renderedFields map that ClientValidationData passes in).
public class ClientValidationProviderTests
{
    [Fact]
    public void AllBuiltInValidators_ProduceExpectedRulesAndParameters()
    {
        var descriptor = GetDescriptor<AllAttributesModel>(
            nameof(AllAttributesModel.Required),
            nameof(AllAttributesModel.Length),
            nameof(AllAttributesModel.MaxLen),
            nameof(AllAttributesModel.MinLen),
            nameof(AllAttributesModel.NumericRange),
            nameof(AllAttributesModel.Pattern),
            nameof(AllAttributesModel.Compared),
            nameof(AllAttributesModel.Email),
            nameof(AllAttributesModel.Website),
            nameof(AllAttributesModel.PhoneNumber),
            nameof(AllAttributesModel.Card),
            nameof(AllAttributesModel.Upload));

        Assert.NotNull(descriptor);

        Assert.Equal("required", SingleRule(descriptor!, nameof(AllAttributesModel.Required)).Name);

        var length = SingleRule(descriptor!, nameof(AllAttributesModel.Length));
        Assert.Equal("length", length.Name);
        Assert.Equal("8", length.Parameters!["min"]);
        Assert.Equal("100", length.Parameters!["max"]);

        var maxlen = SingleRule(descriptor!, nameof(AllAttributesModel.MaxLen));
        Assert.Equal("maxlength", maxlen.Name);
        Assert.Equal("50", maxlen.Parameters!["max"]);

        var minlen = SingleRule(descriptor!, nameof(AllAttributesModel.MinLen));
        Assert.Equal("minlength", minlen.Name);
        Assert.Equal("5", minlen.Parameters!["min"]);

        var range = SingleRule(descriptor!, nameof(AllAttributesModel.NumericRange));
        Assert.Equal("range", range.Name);
        Assert.Equal("1", range.Parameters!["min"]);
        Assert.Equal("10", range.Parameters!["max"]);

        var regex = SingleRule(descriptor!, nameof(AllAttributesModel.Pattern));
        Assert.Equal("regex", regex.Name);
        Assert.Equal("[a-z]+", regex.Parameters!["pattern"]);

        var equalto = SingleRule(descriptor!, nameof(AllAttributesModel.Compared));
        Assert.Equal("equalto", equalto.Name);
        Assert.Equal("*." + nameof(AllAttributesModel.Required), equalto.Parameters!["other"]);

        Assert.Equal("email", SingleRule(descriptor!, nameof(AllAttributesModel.Email)).Name);
        Assert.Equal("url", SingleRule(descriptor!, nameof(AllAttributesModel.Website)).Name);
        Assert.Equal("phone", SingleRule(descriptor!, nameof(AllAttributesModel.PhoneNumber)).Name);
        Assert.Equal("creditcard", SingleRule(descriptor!, nameof(AllAttributesModel.Card)).Name);

        var file = SingleRule(descriptor!, nameof(AllAttributesModel.Upload));
        Assert.Equal("fileextensions", file.Name);
        Assert.Equal(".png,.jpg", file.Parameters!["extensions"]);

        // Every rule carries a non-empty formatted error message.
        Assert.All(descriptor!.Fields, f => Assert.All(f.Rules, r => Assert.False(string.IsNullOrEmpty(r.ErrorMessage))));
    }

    [Fact]
    public void MultipleAttributesOnOneProperty_ProduceRulesInDeclarationOrder()
    {
        var descriptor = GetDescriptor<MultiAttributeModel>(nameof(MultiAttributeModel.Email));

        var field = Assert.Single(descriptor!.Fields);
        Assert.Collection(field.Rules,
            r => Assert.Equal("required", r.Name),
            r => Assert.Equal("email", r.Name));
    }

    [Fact]
    public void OnlyRenderedFields_AreEmitted()
    {
        // The model has two validated properties; only one input rendered, so only that field
        // appears. This is the core behavior of render-driven generation.
        var descriptor = GetDescriptor<TwoFieldModel>(nameof(TwoFieldModel.First));

        var field = Assert.Single(descriptor!.Fields);
        Assert.Equal(nameof(TwoFieldModel.First), field.Name);
    }

    [Fact]
    public void PropertyWithoutValidationAttributes_IsOmitted()
    {
        var descriptor = GetDescriptor<TwoFieldModel>(nameof(TwoFieldModel.Unvalidated));

        Assert.Null(descriptor);
    }

    [Fact]
    public void EmptyRenderedFields_ReturnsNull()
    {
        var provider = CreateProvider();
        var model = new TwoFieldModel();

        var descriptor = provider.GetFormDescriptor(new EditContext(model), new Dictionary<FieldIdentifier, string>());

        Assert.Null(descriptor);
    }

    [Fact]
    public void StringLength_OmitsSentinelBounds()
    {
        var maxOnly = SingleRule(GetDescriptor<StringLengthModel>(nameof(StringLengthModel.MaxOnly))!, nameof(StringLengthModel.MaxOnly));
        Assert.Equal("length", maxOnly.Name);
        Assert.True(maxOnly.Parameters!.ContainsKey("max"));
        Assert.False(maxOnly.Parameters!.ContainsKey("min"));

        var minOnly = SingleRule(GetDescriptor<StringLengthModel>(nameof(StringLengthModel.MinOnly))!, nameof(StringLengthModel.MinOnly));
        Assert.Equal("length", minOnly.Name);
        Assert.True(minOnly.Parameters!.ContainsKey("min"));
        Assert.False(minOnly.Parameters!.ContainsKey("max"));
    }

    [Fact]
    public void Range_WithNonNumericOperand_ProducesNoRule()
    {
        // The JS range validator is numeric-only; a DateTime range must not emit a rule, and
        // because it is the only attribute, the field is omitted entirely.
        var descriptor = GetDescriptor<DateRangeModel>(nameof(DateRangeModel.Date));

        Assert.Null(descriptor);
    }

    [Fact]
    public void CustomAdapterAttribute_ContributesItsRules()
    {
        var rule = SingleRule(GetDescriptor<CustomAdapterModel>(nameof(CustomAdapterModel.Value))!, nameof(CustomAdapterModel.Value));

        Assert.Equal("custom", rule.Name);
        Assert.Equal("custom message", rule.ErrorMessage);
        Assert.Equal("bar", rule.Parameters!["foo"]);
    }

    [Fact]
    public void DisplayNameAttribute_IsUsedInErrorMessage()
    {
        var rule = SingleRule(GetDescriptor<DisplayNameModel>(nameof(DisplayNameModel.Field))!, nameof(DisplayNameModel.Field));

        Assert.Contains("Custom Label", rule.ErrorMessage);
    }

    [Fact]
    public void Localizer_PopulatesContextsAndLocalizesMessage()
    {
        var localizer = new RecordingLocalizer();
        var options = new ValidationOptions { Localizer = localizer };

        var rule = SingleRule(GetDescriptor<DisplayNameModel>(options, nameof(DisplayNameModel.Field))!, nameof(DisplayNameModel.Field));

        // The rule message is the localizer's output.
        Assert.Equal("localized-error", rule.ErrorMessage);

        // The display-name context carries the literal display name as the lookup key.
        Assert.Equal("Custom Label", localizer.LastDisplayContext!.Value.DisplayName);
        Assert.Equal(nameof(DisplayNameModel.Field), localizer.LastDisplayContext!.Value.MemberName);

        // The error-message context carries the resolved display name, member, declaring type, and attribute.
        Assert.Equal("localized-display", localizer.LastErrorContext!.Value.DisplayName);
        Assert.Equal(nameof(DisplayNameModel.Field), localizer.LastErrorContext!.Value.MemberName);
        Assert.Equal(typeof(DisplayNameModel), localizer.LastErrorContext!.Value.DeclaringType);
        Assert.IsType<RequiredAttribute>(localizer.LastErrorContext!.Value.Attribute);
    }

    [Fact]
    public void TopLevelField_IsEmitted_WithoutMev()
    {
        // No MEV configured: the DataAnnotations submit path validates top-level properties, so a
        // top-level field is emitted.
        var descriptor = GetDescriptor<TwoFieldModel>(nameof(TwoFieldModel.First));

        Assert.NotNull(descriptor);
        Assert.Equal(nameof(TwoFieldModel.First), Assert.Single(descriptor!.Fields).Name);
    }

    [Fact]
    public void NestedField_IsSkipped_WithoutMev()
    {
        // No MEV configured: the DataAnnotations submit path does not recurse into nested
        // sub-models, so a client rule must NOT be emitted for a nested field (it would reject a
        // value the server silently accepts).
        var provider = CreateProvider();
        var model = new ParentModel { Child = new ChildModel() };
        var fields = new Dictionary<FieldIdentifier, string>
        {
            [new FieldIdentifier(model.Child, nameof(ChildModel.Street))] = "Child.Street",
        };

        var descriptor = provider.GetFormDescriptor(new EditContext(model), fields);

        Assert.Null(descriptor);
    }

    [Fact]
    public void NestedField_IsEmitted_WhenMevRecognizesOwningType()
    {
        var options = CreateMevOptions(typeof(ParentModel), (typeof(ChildModel), nameof(ChildModel.Street)));
        var provider = CreateProvider(options);
        var model = new ParentModel { Child = new ChildModel() };
        var fields = new Dictionary<FieldIdentifier, string>
        {
            [new FieldIdentifier(model.Child, nameof(ChildModel.Street))] = "Child.Street",
        };

        var descriptor = provider.GetFormDescriptor(new EditContext(model), fields);

        var field = Assert.Single(descriptor!.Fields);
        Assert.Equal("Child.Street", field.Name);
        Assert.Equal("required", Assert.Single(field.Rules).Name);
    }

    [Fact]
    public void NestedField_IsSuppressed_WhenMevDoesNotRecognizeOwningType()
    {
        // MEV is configured and recognizes the form model, but NOT the nested child type. The
        // submit-time MEV walk would not reach the nested property, so the client rule must be
        // suppressed to preserve client/server parity.
        var options = CreateMevOptions(typeof(ParentModel) /* ChildModel deliberately not registered */);
        var provider = CreateProvider(options);
        var model = new ParentModel { Child = new ChildModel() };
        var fields = new Dictionary<FieldIdentifier, string>
        {
            [new FieldIdentifier(model.Child, nameof(ChildModel.Street))] = "Child.Street",
        };

        var descriptor = provider.GetFormDescriptor(new EditContext(model), fields);

        Assert.Null(descriptor);
    }

    // ---- Helpers ----

    private static EndpointClientValidationProvider CreateProvider(ValidationOptions? options = null)
    {
        var opts = Options.Create(options ?? new ValidationOptions());
        var cache = new ClientValidationCache(opts);
        return new EndpointClientValidationProvider(cache, opts);
    }

    private static ClientValidationFormDescriptor? GetDescriptor<TModel>(params string[] fieldNames)
        where TModel : new()
        => GetDescriptor<TModel>(options: null, fieldNames);

    private static ClientValidationFormDescriptor? GetDescriptor<TModel>(ValidationOptions? options, params string[] fieldNames)
        where TModel : new()
    {
        var provider = CreateProvider(options);
        var model = new TModel();
        var fields = new Dictionary<FieldIdentifier, string>();
        foreach (var name in fieldNames)
        {
            fields[new FieldIdentifier(model, name)] = name;
        }
        return provider.GetFormDescriptor(new EditContext(model), fields);
    }

    private static ClientValidationRule SingleRule(ClientValidationFormDescriptor descriptor, string fieldName)
    {
        var field = Assert.Single(descriptor.Fields, f => f.Name == fieldName);
        return Assert.Single(field.Rules);
    }

#pragma warning disable ASP0029 // Microsoft.Extensions.Validation evaluation APIs.
    private static ValidationOptions CreateMevOptions(Type formModelType, params (Type Type, string Member)[] nestedMembers)
    {
        var map = new Dictionary<Type, IValidatableInfo>
        {
            [formModelType] = new TestTypeInfo(formModelType, Array.Empty<TestPropertyInfo>()),
        };
        foreach (var (type, member) in nestedMembers)
        {
            map[type] = new TestTypeInfo(type, new[] { new TestPropertyInfo(type, typeof(string), member) });
        }

        var options = new ValidationOptions();
        options.Resolvers.Add(new TestResolver(map));
        return options;
    }

    private sealed class TestResolver(Dictionary<Type, IValidatableInfo> map) : IValidatableInfoResolver
    {
        public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
            => map.TryGetValue(type, out validatableInfo);

        public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
        {
            validatableInfo = null;
            return false;
        }
    }

    private sealed class TestTypeInfo(Type type, IReadOnlyList<TestPropertyInfo> members) : ValidatableTypeInfo(type, members)
    {
        protected override ValidationAttribute[] GetValidationAttributes() => Array.Empty<ValidationAttribute>();
    }

    private sealed class TestPropertyInfo(Type declaringType, Type propertyType, string name)
        : ValidatablePropertyInfo(declaringType, propertyType, name)
    {
        protected override ValidationAttribute[] GetValidationAttributes() => Array.Empty<ValidationAttribute>();
    }
#pragma warning restore ASP0029

    private sealed class RecordingLocalizer : IValidationLocalizer
    {
        public DisplayNameLocalizationContext? LastDisplayContext { get; private set; }
        public ErrorMessageLocalizationContext? LastErrorContext { get; private set; }

        public string? ResolveDisplayName(in DisplayNameLocalizationContext context)
        {
            LastDisplayContext = context;
            return "localized-display";
        }

        public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context)
        {
            LastErrorContext = context;
            return "localized-error";
        }
    }

    // ---- Test models ----

    private sealed class AllAttributesModel
    {
        [Required] public string Required { get; set; } = "";
        [StringLength(100, MinimumLength = 8)] public string Length { get; set; } = "";
        [MaxLength(50)] public string MaxLen { get; set; } = "";
        [MinLength(5)] public string MinLen { get; set; } = "";
        [Range(1, 10)] public int NumericRange { get; set; }
        [RegularExpression("[a-z]+")] public string Pattern { get; set; } = "";
        [Compare(nameof(Required))] public string Compared { get; set; } = "";
        [EmailAddress] public string Email { get; set; } = "";
        [Url] public string Website { get; set; } = "";
        [Phone] public string PhoneNumber { get; set; } = "";
        [CreditCard] public string Card { get; set; } = "";
        [FileExtensions(Extensions = "png,jpg")] public string Upload { get; set; } = "";
    }

    private sealed class MultiAttributeModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }

    private sealed class TwoFieldModel
    {
        [Required] public string First { get; set; } = "";
        [Required] public string Second { get; set; } = "";
        public string Unvalidated { get; set; } = "";
    }

    private sealed class StringLengthModel
    {
        [StringLength(50)] public string MaxOnly { get; set; } = "";
        [StringLength(int.MaxValue, MinimumLength = 5)] public string MinOnly { get; set; } = "";
    }

    private sealed class DateRangeModel
    {
        [Range(typeof(DateTime), "2020-01-01", "2020-12-31")]
        public DateTime Date { get; set; }
    }

    private sealed class DisplayNameModel
    {
        [Required]
        [Display(Name = "Custom Label")]
        public string Field { get; set; } = "";
    }

    private sealed class CustomAdapterModel
    {
        [CustomAdapter]
        public string Value { get; set; } = "";
    }

    private sealed class CustomAdapterAttribute : ValidationAttribute, IClientValidationAdapter
    {
        public IEnumerable<ClientValidationRule> GetClientValidationRules(string errorMessage)
        {
            yield return new ClientValidationRule("custom", "custom message",
                new Dictionary<string, string> { ["foo"] = "bar" });
        }
    }

    private sealed class ParentModel
    {
        public ChildModel Child { get; set; } = new();
    }

    private sealed class ChildModel
    {
        [Required] public string Street { get; set; } = "";
    }
}
