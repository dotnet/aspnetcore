// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Endpoints.Forms;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.FormValidation;

// Integration tests for the SSR client-validation rule pipeline:
// DataAnnotationsClientValidationProvider + ClientValidationCache. These exercise the real reflection,
// rule mapping, server-validation gating, and localization, driven by the set of fields an input
// rendered for (the renderedFields map that ClientValidationData passes in). Assertions run against
// the serialized JSON payload the provider writes, which is the actual client-validation wire format.
public class ClientValidationProviderTests
{
    [Fact]
    public void AllBuiltInValidators_ProduceExpectedRulesAndParameters()
    {
        var form = GetData<AllAttributesModel>(
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

        Assert.NotNull(form);

        Assert.Equal("required", SingleRule(form!, nameof(AllAttributesModel.Required)).Name);

        var length = SingleRule(form!, nameof(AllAttributesModel.Length));
        Assert.Equal("length", length.Name);
        Assert.Equal("8", length.Params!["min"]);
        Assert.Equal("100", length.Params!["max"]);

        var maxlen = SingleRule(form!, nameof(AllAttributesModel.MaxLen));
        Assert.Equal("maxlength", maxlen.Name);
        Assert.Equal("50", maxlen.Params!["max"]);

        var minlen = SingleRule(form!, nameof(AllAttributesModel.MinLen));
        Assert.Equal("minlength", minlen.Name);
        Assert.Equal("5", minlen.Params!["min"]);

        var range = SingleRule(form!, nameof(AllAttributesModel.NumericRange));
        Assert.Equal("range", range.Name);
        Assert.Equal("1", range.Params!["min"]);
        Assert.Equal("10", range.Params!["max"]);

        var regex = SingleRule(form!, nameof(AllAttributesModel.Pattern));
        Assert.Equal("regex", regex.Name);
        Assert.Equal("[a-z]+", regex.Params!["pattern"]);

        var equalto = SingleRule(form!, nameof(AllAttributesModel.Compared));
        Assert.Equal("equalto", equalto.Name);
        Assert.Equal("*." + nameof(AllAttributesModel.Required), equalto.Params!["other"]);

        Assert.Equal("email", SingleRule(form!, nameof(AllAttributesModel.Email)).Name);
        Assert.Equal("url", SingleRule(form!, nameof(AllAttributesModel.Website)).Name);
        Assert.Equal("phone", SingleRule(form!, nameof(AllAttributesModel.PhoneNumber)).Name);
        Assert.Equal("creditcard", SingleRule(form!, nameof(AllAttributesModel.Card)).Name);

        var file = SingleRule(form!, nameof(AllAttributesModel.Upload));
        Assert.Equal("fileextensions", file.Name);
        Assert.Equal(".png,.jpg", file.Params!["extensions"]);

        // Every rule carries a non-empty formatted error message.
        Assert.All(form!.Fields, f => Assert.All(f.Rules, r => Assert.False(string.IsNullOrEmpty(r.Message))));
    }

    [Fact]
    public void MultipleAttributesOnOneProperty_ProduceRulesInDeclarationOrder()
    {
        var form = GetData<MultiAttributeModel>(nameof(MultiAttributeModel.Email));

        var field = Assert.Single(form!.Fields);
        Assert.Collection(field.Rules,
            r => Assert.Equal("required", r.Name),
            r => Assert.Equal("email", r.Name));
    }

    [Fact]
    public void OnlyRenderedFields_AreEmitted()
    {
        // The model has two validated properties; only one input rendered, so only that field
        // appears. This is the core behavior of render-driven generation.
        var form = GetData<TwoFieldModel>(nameof(TwoFieldModel.First));

        var field = Assert.Single(form!.Fields);
        Assert.Equal(nameof(TwoFieldModel.First), field.Name);
    }

    [Fact]
    public void PropertyWithoutValidationAttributes_IsOmitted()
    {
        var form = GetData<TwoFieldModel>(nameof(TwoFieldModel.Unvalidated));

        Assert.Null(form);
    }

    [Fact]
    public void EmptyRenderedFields_ReturnsNull()
    {
        var provider = CreateProvider();
        var model = new TwoFieldModel();

        var form = Serialize(provider, new EditContext(model), new Dictionary<FieldIdentifier, string>());

        Assert.Null(form);
    }

    [Fact]
    public void StringLength_OmitsSentinelBounds()
    {
        var maxOnly = SingleRule(GetData<StringLengthModel>(nameof(StringLengthModel.MaxOnly))!, nameof(StringLengthModel.MaxOnly));
        Assert.Equal("length", maxOnly.Name);
        Assert.True(maxOnly.Params!.ContainsKey("max"));
        Assert.False(maxOnly.Params!.ContainsKey("min"));

        var minOnly = SingleRule(GetData<StringLengthModel>(nameof(StringLengthModel.MinOnly))!, nameof(StringLengthModel.MinOnly));
        Assert.Equal("length", minOnly.Name);
        Assert.True(minOnly.Params!.ContainsKey("min"));
        Assert.False(minOnly.Params!.ContainsKey("max"));
    }

    [Fact]
    public void Range_WithNonNumericOperand_ProducesNoRule()
    {
        // The JS range validator is numeric-only; a DateTime range must not emit a rule, and
        // because it is the only attribute, the field is omitted entirely.
        var form = GetData<DateRangeModel>(nameof(DateRangeModel.Date));

        Assert.Null(form);
    }

    [Fact]
    public void CustomRuleProviderAttribute_ContributesItsRules()
    {
        var rule = SingleRule(GetData<CustomRuleProviderModel>(nameof(CustomRuleProviderModel.Value))!, nameof(CustomRuleProviderModel.Value));

        Assert.Equal("custom", rule.Name);
        Assert.Equal("custom message", rule.Message);
        Assert.Equal("bar", rule.Params!["foo"]);
    }

    [Fact]
    public void DisplayNameAttribute_IsUsedInErrorMessage()
    {
        var rule = SingleRule(GetData<DisplayNameModel>(nameof(DisplayNameModel.Field))!, nameof(DisplayNameModel.Field));

        Assert.Contains("Custom Label", rule.Message);
    }

    [Fact]
    public void Localizer_PopulatesContextsAndLocalizesMessage()
    {
        var localizer = new RecordingLocalizer();
        var options = new ValidationOptions { Localizer = localizer };

        var rule = SingleRule(GetData<DisplayNameModel>(options, nameof(DisplayNameModel.Field))!, nameof(DisplayNameModel.Field));

        // The rule message is the localizer's output.
        Assert.Equal("localized-error", rule.Message);

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
        var form = GetData<TwoFieldModel>(nameof(TwoFieldModel.First));

        Assert.NotNull(form);
        Assert.Equal(nameof(TwoFieldModel.First), Assert.Single(form!.Fields).Name);
    }

    [Fact]
    public void NestedField_IsSkipped_WithoutMev()
    {
        // No MEV configured: the DataAnnotations submit path does not recurse into nested
        // sub-models, so a client rule must not be emitted for a nested field (it would reject a
        // value the server silently accepts).
        var provider = CreateProvider();
        var model = new OrderModel();
        var fields = new Dictionary<FieldIdentifier, string>
        {
            [new FieldIdentifier(model.ShippingAddress, nameof(AddressModel.Street))] = "Order.ShippingAddress.Street",
        };

        var form = Serialize(provider, new EditContext(model), fields);

        Assert.Null(form);
    }

    [Fact]
    public void NestedField_IsEmitted_WhenReachableThroughValidatableTypes()
    {
        // ShippingAddress is a validatable-typed member of the form model, so the MEV submit walk
        // recurses into it and validates Street. The client rule is emitted to match.
        var options = CreateMevOptions(typeof(OrderModel), typeof(AddressModel));
        var provider = CreateProvider(options);
        var model = new OrderModel();
        var fields = new Dictionary<FieldIdentifier, string>
        {
            [new FieldIdentifier(model.ShippingAddress, nameof(AddressModel.Street))] = "Order.ShippingAddress.Street",
        };

        var form = Serialize(provider, new EditContext(model), fields);

        var field = Assert.Single(form!.Fields);
        Assert.Equal("Order.ShippingAddress.Street", field.Name);
        Assert.Equal("required", Assert.Single(field.Rules).Name);
    }

    [Fact]
    public void NestedField_IsSuppressed_WhenReachableOnlyThroughSkippedMember()
    {
        // Wrapper is skipped, so the MEV submit walk never recurses into it - even though the field's
        // owner type (AddressModel) is validatable and reachable elsewhere (via ShippingAddress).
        var options = CreateMevOptions(typeof(OrderModel), typeof(AddressModel));
        var provider = CreateProvider(options);
        var model = new OrderModel();
        var fields = new Dictionary<FieldIdentifier, string>
        {
            [new FieldIdentifier(model.Wrapper.Nested, nameof(AddressModel.Street))] = "Order.Wrapper.Nested.Street",
        };

        var form = Serialize(provider, new EditContext(model), fields);

        Assert.Null(form);
    }

    [Fact]
    public void EmittedFields_MatchTheFieldsMevValidatesOnSubmit()
    {
        // End-to-end reachability check: the client must emit rules for exactly the set of fields
        // MEV actually validates when the form is submitted. AddressModel is reachable via
        // ShippingAddress (validated) but the same type reached via the skipped Wrapper is not, so
        // this exercises the path-sensitive distinction against real MEV validation.
        var options = CreateMevOptions(typeof(OrderModel), typeof(AddressModel));
        var model = new OrderModel(); // all [Required] strings empty -> everything reachable is invalid

        var fields = new Dictionary<FieldIdentifier, string>
        {
            [new FieldIdentifier(model, nameof(OrderModel.OrderName))] = "Order.OrderName",
            [new FieldIdentifier(model.ShippingAddress, nameof(AddressModel.Street))] = "Order.ShippingAddress.Street",
            [new FieldIdentifier(model.Wrapper.Nested, nameof(AddressModel.Street))] = "Order.Wrapper.Nested.Street",
        };

        // The fields the client emits rules for, as model-relative paths (drop the "Order" prefix).
        var provider = CreateProvider(options);
        var form = Serialize(provider, new EditContext(model), fields);
        var clientPaths = form!.Fields
            .Select(f => f.Name[(f.Name.IndexOf('.') + 1)..])
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        // The fields MEV actually validates on submit (error keys are model-relative paths).
        var mevPaths = GetMevValidatedPaths(options, model)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(mevPaths, clientPaths);
        Assert.Equal(new[] { "OrderName", "ShippingAddress.Street" }, clientPaths);
    }

    [Fact]
    public void GlobalDisableClientValidation_EmitsNoCarrier()
    {
        var model = new TwoFieldModel();
        var editContext = new EditContext(model);
        var fields = new Dictionary<FieldIdentifier, string>
        {
            [new FieldIdentifier(model, nameof(TwoFieldModel.First))] = "First",
        };

        // Sanity: with client validation enabled, the provider produces a carrier fragment.
        Assert.NotNull(CreateProvider().RenderClientValidationRules(editContext, fields));

        // With the global opt-out (RazorComponentsServiceOptions.DisableClientValidation), the
        // provider emits nothing so the JS engine never activates for any form.
        Assert.Null(CreateProvider(disableClientValidation: true).RenderClientValidationRules(editContext, fields));
    }

    // ---- Helpers ----

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    private static DataAnnotationsClientValidationProvider CreateProvider(ValidationOptions? options = null, bool disableClientValidation = false)
    {
        var opts = Options.Create(options ?? new ValidationOptions());
        var cache = new ClientValidationCache(opts);
        var razorOptions = Options.Create(new RazorComponentsServiceOptions { DisableClientValidation = disableClientValidation });
        return new DataAnnotationsClientValidationProvider(cache, opts, razorOptions);
    }

    private static FormData? GetData<TModel>(params string[] fieldNames)
        where TModel : new()
        => GetData<TModel>(options: null, fieldNames);

    private static FormData? GetData<TModel>(ValidationOptions? options, params string[] fieldNames)
        where TModel : new()
    {
        var provider = CreateProvider(options);
        var model = new TModel();
        var fields = new Dictionary<FieldIdentifier, string>();
        foreach (var name in fieldNames)
        {
            fields[new FieldIdentifier(model, name)] = name;
        }
        return Serialize(provider, new EditContext(model), fields);
    }

    // Serializes via the provider and parses the JSON payload back into the wire shape, or null
    // when the provider emits nothing.
    private static FormData? Serialize(
        DataAnnotationsClientValidationProvider provider,
        EditContext editContext,
        Dictionary<FieldIdentifier, string> fields)
    {
        var json = provider.SerializeClientValidationData(editContext, fields);
        return json is null ? null : JsonSerializer.Deserialize<FormData>(json, s_jsonOptions);
    }

    private static RuleData SingleRule(FormData form, string fieldName)
    {
        var field = Assert.Single(form.Fields, f => f.Name == fieldName);
        return Assert.Single(field.Rules);
    }

    // Mirrors the JSON wire shape produced by the provider.
    private sealed record FormData([property: JsonPropertyName("fields")] List<FieldData> Fields);

    private sealed record FieldData(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("rules")] List<RuleData> Rules);

    private sealed record RuleData(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("params")] Dictionary<string, string>? Params);

#pragma warning disable ASP0029 // Microsoft.Extensions.Validation evaluation APIs.
    private static ValidationOptions CreateMevOptions(params Type[] validatableTypes)
    {
        var services = new ServiceCollection();
        services.AddValidation();
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<ValidationOptions>>().Value;

        foreach (var type in validatableTypes)
        {
            Assert.True(options.TryGetValidatableTypeInfo(type, out _));
        }

        return options;
    }

    // Runs MEV validation the way the submit path does and returns the set of field paths that were
    // validated (surfaced as error keys) - used to assert the client emits rules for exactly those.
    private static string[] GetMevValidatedPaths(ValidationOptions options, object model)
    {
        Assert.True(options.TryGetValidatableTypeInfo(model.GetType(), out var typeInfo));
        var validateContext = new ValidateContext
        {
            ValidationOptions = options,
        };
        typeInfo!.Validate(model, validateContext);
        return validateContext.ValidationErrors?.Keys.ToArray() ?? Array.Empty<string>();
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

    private sealed class CustomRuleProviderModel
    {
        [CustomRuleProvider(ErrorMessage = "custom message")]
        public string Value { get; set; } = "";
    }

    private sealed class CustomRuleProviderAttribute : ValidationAttribute, IClientValidationRuleProvider
    {
        public IEnumerable<ClientValidationRule> GetClientValidationRules()
        {
            yield return new ClientValidationRule("custom",
                new Dictionary<string, string> { ["foo"] = "bar" });
        }
    }

    [Microsoft.Extensions.Validation.ValidatableType]
    public sealed class OrderModel
    {
        [Required] public string OrderName { get; set; } = "";

        // Validatable-typed member -> the MEV submit walk recurses into it.
        public AddressModel ShippingAddress { get; set; } = new();

        // Skipped member -> the MEV submit walk does NOT recurse into it, so anything reachable
        // only through it is not validated on submit.
        [SkipValidation]
        public WrapperModel Wrapper { get; set; } = new();
    }

    [Microsoft.Extensions.Validation.ValidatableType]
    public sealed class AddressModel
    {
        [Required] public string Street { get; set; } = "";
    }

    public sealed class WrapperModel
    {
        public AddressModel Nested { get; set; } = new();
    }
}
