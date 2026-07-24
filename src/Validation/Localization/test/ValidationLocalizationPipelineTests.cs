// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation.Tests;

namespace Microsoft.Extensions.Validation.Localization.Tests;

/// <summary>
/// Pipeline tests verifying that <see cref="ValidationLocalizationServiceCollectionExtensions.AddValidationLocalization"/>
/// produces a fully functional pipeline: validation runs, picks up the configured
/// <see cref="IStringLocalizerFactory"/>, and produces localized error messages.
/// </summary>
public class ValidationLocalizationPipelineTests : ValidationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_ProducesLocalizedErrorMessage(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredKey"] = "Le champ {0} est obligatoire.",
            ["Customer Name"] = "Nom du client",
        };
        var context = SetupPipeline(translations);
        var model = new PipelineRequiredLocalizedModel { Name = null };
        var typeInfo = GetTypeInfo<PipelineRequiredLocalizedModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Le champ Nom du client est obligatoire.", context.ValidationErrors["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_RangeAttribute_FormatsMinMaxIntoLocalizedTemplate(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["RangeKey"] = "{0}: valeur entre {1} et {2} attendue.",
        };
        var context = SetupPipeline(translations);
        var model = new PipelineRangeAttributeModel { Age = -5 };
        var typeInfo = GetTypeInfo<PipelineRangeAttributeModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Age: valeur entre 18 et 120 attendue.", context.ValidationErrors["Age"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_ErrorMessageResourceType_BypassesLocalization(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["This field is required."] = "Should NOT be used",
        };
        var context = SetupPipeline(translations);
        var model = new PipelineResourceErrorModel { Name = null };
        var typeInfo = GetTypeInfo<PipelineResourceErrorModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal(PipelineIntegrationResources.RequiredError, context.ValidationErrors["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_ErrorMessageKeyProvider_LocalizesAttributesWithoutErrorMessage(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredAttribute_Default"] = "Field {0} is required (convention).",
        };
        var context = SetupPipeline(translations, options =>
        {
            options.ErrorMessageKeyProvider = ctx => $"{ctx.Attribute.GetType().Name}_Default";
        });
        var model = new PipelineRequiredDefaultModel { Name = null };
        var typeInfo = GetTypeInfo<PipelineRequiredDefaultModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Field Name is required (convention).", context.ValidationErrors["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_TypeLevelAttribute_Localized(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["StartLessThanEndKey"] = "Le début doit être inférieur à la fin.",
        };
        var context = SetupPipeline(translations);
        var model = new PipelineTypeLevelRangeModel { Start = 10, End = 5 };
        var typeInfo = GetTypeInfo<PipelineTypeLevelRangeModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        var errors = context.ValidationErrors.Values.SelectMany(v => v).Select(c => c.ErrorMessage).ToList();
        Assert.Contains("Le début doit être inférieur à la fin.", errors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_LocalizationLookupMiss_FallsBackToAttributeDefault(bool useAsync)
    {
        var context = SetupPipeline(translations: []);
        var model = new PipelineRequiredKeyModel { Name = null };
        var typeInfo = GetTypeInfo<PipelineRequiredKeyModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("RequiredKey", context.ValidationErrors["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_DisplayNameLookup_LocalizesIntoDefaultErrorTemplate(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["Customer Name"] = "Nom du client",
        };
        var context = SetupPipeline(translations);
        var model = new PipelineDisplayNameModel { Name = null };
        var typeInfo = GetTypeInfo<PipelineDisplayNameModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Nom du client field is required.", context.ValidationErrors["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_ParameterLevel_SharedResource_Localized(bool useAsync)
    {
        var sharedTranslations = new Dictionary<Type, Dictionary<string, string>>
        {
            [typeof(PipelineSharedValidationMessages)] = new()
            {
                ["RequiredKey"] = "Param {0} requis.",
                ["Param Display"] = "Paramètre",
            },
        };
        var context = SetupPipelinePerType(sharedTranslations, options =>
        {
            options.LocalizerProvider = (_, factory) => factory.Create(typeof(PipelineSharedValidationMessages));
        });
        var paramInfo = GetParameterInfo(context, nameof(PipelineParameterModels.RequiredParameter));

        await ValidateAsync(paramInfo, null, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Param Paramètre requis.", context.ValidationErrors["myParam"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_StringLength_MaxOnly_FormatsLengthIntoTemplate(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["MaxLengthKey"] = "{0} doit avoir au plus {1} caractères.",
        };
        var context = SetupPipeline(translations);
        var model = new PipelineStringLengthMaxModel { Name = new string('a', 50) };
        var typeInfo = GetTypeInfo<PipelineStringLengthMaxModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Name doit avoir au plus 10 caractères.", context.ValidationErrors["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_StringLength_MinAndMax_FormatsBothIntoTemplate(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["StringLengthRangeKey"] = "{0} doit avoir entre {2} et {1} caractères.",
        };
        var context = SetupPipeline(translations);
        var model = new PipelineStringLengthRangeModel { Name = "ab" };
        var typeInfo = GetTypeInfo<PipelineStringLengthRangeModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Name doit avoir entre 3 et 10 caractères.", context.ValidationErrors["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_PerInvocationLocalizerOverride_LatestValueWins(bool useAsync)
    {
        var context = SetupPipeline(translations: []);
        var override1 = new ConstantLocalizer("FROM-OVERRIDE-1");
        var override2 = new ConstantLocalizer("FROM-OVERRIDE-2");
        var typeInfo = GetTypeInfo<PipelineRequiredDefaultModel>(context);

        context.ValidationOptions.Localizer = override1;

        await ValidateAsync(typeInfo, new PipelineRequiredDefaultModel { Name = null }, context, useAsync, default);

        Assert.Equal("FROM-OVERRIDE-1", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());

        context.ValidationOptions.Localizer = override2;

        await ValidateAsync(typeInfo, new PipelineRequiredDefaultModel { Name = null }, context, useAsync, default);

        Assert.Equal("FROM-OVERRIDE-2", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Last());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullPipeline_InheritedProperty_LocalizerProviderReceivesDeclaringBaseType(bool useAsync)
    {
        var seenTypes = new List<Type?>();
        var translations = new Dictionary<Type, Dictionary<string, string>>
        {
            [typeof(PipelineBaseInheritedModel)] = new() { ["RequiredKey"] = "{0} is required (from base resource)." },
        };
        var context = SetupPipelinePerType(translations, options =>
        {
            options.LocalizerProvider = (type, factory) =>
            {
                seenTypes.Add(type);
                return factory.Create(type ?? typeof(object));
            };
        });
        var model = new PipelineDerivedInheritedModel { Name = null };
        var typeInfo = GetTypeInfo<PipelineDerivedInheritedModel>(context);

        await ValidateAsync(typeInfo, model, context, useAsync, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("Name is required (from base resource).", context.ValidationErrors["Name"].Select(e => e.ErrorMessage).Single());
        Assert.Contains(typeof(PipelineBaseInheritedModel), seenTypes);
        Assert.DoesNotContain(typeof(PipelineDerivedInheritedModel), seenTypes);
    }

    private static ValidateContext SetupPipeline(
        Dictionary<string, string> translations,
        Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IStringLocalizerFactory>(new TestStringLocalizerFactory(translations));
        services.AddValidation();
        services.AddValidationLocalization(configureOptions);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        return new ValidateContext
        {
            ValidationOptions = options,
            ServiceProvider = provider,
        };
    }

    private static ValidateContext SetupPipelinePerType(
        Dictionary<Type, Dictionary<string, string>> perTypeTranslations,
        Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IStringLocalizerFactory>(new TestStringLocalizerFactory(perTypeTranslations));
        services.AddValidation();
        services.AddValidationLocalization(configureOptions);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        return new ValidateContext
        {
            ValidationOptions = options,
            ServiceProvider = provider,
        };
    }

    private static IValidatableTypeInfo GetTypeInfo<T>(ValidateContext context)
    {
        Assert.True(context.ValidationOptions.TryGetValidatableTypeInfo(typeof(T), out var typeInfo));
        return typeInfo;
    }

    private static IValidatableParameterInfo GetParameterInfo(ValidateContext context, string methodName)
    {
        var parameterInfo = typeof(PipelineParameterModels)
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!
            .GetParameters()[0];

        Assert.True(context.ValidationOptions.TryGetValidatableParameterInfo(parameterInfo, out var parameterValidatableInfo));
        return parameterValidatableInfo;
    }

    private sealed class ConstantLocalizer(string message) : IValidationLocalizer
    {
        public string? ResolveDisplayName(in DisplayNameLocalizationContext context) => null;
        public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context) => message;
    }
}

[ValidatableType]
public sealed class PipelineRequiredLocalizedModel
{
    [Required(ErrorMessage = "RequiredKey")]
    [Display(Name = "Customer Name")]
    public string? Name { get; set; }
}

[ValidatableType]
public sealed class PipelineRangeAttributeModel
{
    [Range(18, 120, ErrorMessage = "RangeKey")]
    public int Age { get; set; }
}

[ValidatableType]
public sealed class PipelineResourceErrorModel
{
    [Required(ErrorMessageResourceType = typeof(PipelineIntegrationResources), ErrorMessageResourceName = nameof(PipelineIntegrationResources.RequiredError))]
    public string? Name { get; set; }
}

[ValidatableType]
public sealed class PipelineRequiredDefaultModel
{
    [Required]
    public string? Name { get; set; }
}

[ValidatableType]
[PipelineStartLessThanEnd(ErrorMessage = "StartLessThanEndKey")]
public sealed class PipelineTypeLevelRangeModel
{
    public int Start { get; set; }
    public int End { get; set; }
}

[ValidatableType]
public sealed class PipelineRequiredKeyModel
{
    [Required(ErrorMessage = "RequiredKey")]
    public string? Name { get; set; }
}

[ValidatableType]
public sealed class PipelineDisplayNameModel
{
    [Required]
    [Display(Name = "Customer Name")]
    public string? Name { get; set; }
}

[ValidatableType]
public sealed class PipelineStringLengthMaxModel
{
    [StringLength(10, ErrorMessage = "MaxLengthKey")]
    public string? Name { get; set; }
}

[ValidatableType]
public sealed class PipelineStringLengthRangeModel
{
    [StringLength(10, MinimumLength = 3, ErrorMessage = "StringLengthRangeKey")]
    public string? Name { get; set; }
}

public static class PipelineParameterModels
{
    public static void RequiredParameter(
        [Required(ErrorMessage = "RequiredKey")]
        [Display(Name = "Param Display")]
        string? myParam)
    {
    }
}

public sealed class PipelineSharedValidationMessages
{
}

public class PipelineBaseInheritedModel
{
    [Required(ErrorMessage = "RequiredKey")]
    public string? Name { get; set; }
}

[ValidatableType]
public sealed class PipelineDerivedInheritedModel : PipelineBaseInheritedModel
{
}

public static class PipelineIntegrationResources
{
    public static string RequiredError => "Resource: This field is required.";
}

public sealed class PipelineStartLessThanEndAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is PipelineTypeLevelRangeModel model && model.Start >= model.End)
        {
            return new ValidationResult(ErrorMessage ?? "Start must be less than End.", [nameof(PipelineTypeLevelRangeModel.Start)]);
        }
        return ValidationResult.Success;
    }
}
