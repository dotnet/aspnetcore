// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Localization.Tests;

/// <summary>
/// Tests for the internal <see cref="DefaultValidationLocalizer"/> exercised through the
/// <see cref="IValidationLocalizer"/> contract.
/// </summary>
public class DefaultValidationLocalizerTests
{
    // --- ResolveDisplayName ---

    [Fact]
    public void ResolveDisplayName_NullDisplayName_ReturnsNull()
    {
        // Returning null (rather than echoing MemberName) lets the LocalizationHelper apply
        // its standard fallback (member name). It also matches the IValidationLocalizer
        // contract: null means "no localization available".
        var localizer = CreateLocalizer(translations: []);

        var result = localizer.ResolveDisplayName(new()
        {
            MemberName = "Name",
            DisplayName = null,
        });

        Assert.Null(result);
    }

    [Fact]
    public void ResolveDisplayName_LiteralLookupHit_ReturnsTranslation()
    {
        var localizer = CreateLocalizer(new() { ["Customer Name"] = "Nom du client" });

        var result = localizer.ResolveDisplayName(new()
        {
            MemberName = "Name",
            DisplayName = "Customer Name",
            DeclaringType = typeof(object),
        });

        Assert.Equal("Nom du client", result);
    }

    [Fact]
    public void ResolveDisplayName_LiteralLookupMiss_ReturnsLiteral()
    {
        var localizer = CreateLocalizer(translations: []);

        var result = localizer.ResolveDisplayName(new()
        {
            MemberName = "Name",
            DisplayName = "Customer Name",
            DeclaringType = typeof(object),
        });

        Assert.Equal("Customer Name", result);
    }

    // --- ResolveErrorMessage ---

    [Fact]
    public void ResolveErrorMessage_NoErrorMessageOrKeyProvider_ReturnsNull()
    {
        var localizer = CreateLocalizer(translations: []);

        var result = localizer.ResolveErrorMessage(new()
        {
            MemberName = "Name",
            DisplayName = "Name",
            Attribute = new RequiredAttribute(),
        });

        Assert.Null(result);
    }

    [Fact]
    public void ResolveErrorMessage_ErrorMessageAsKey_LookupHit_FormatsWithDisplayName()
    {
        var localizer = CreateLocalizer(new() { ["RequiredKey"] = "Le {0} est requis." });

        var result = localizer.ResolveErrorMessage(new()
        {
            MemberName = "Name",
            DisplayName = "Nom",
            Attribute = new RequiredAttribute { ErrorMessage = "RequiredKey" },
        });

        Assert.Equal("Le Nom est requis.", result);
    }

    [Fact]
    public void ResolveErrorMessage_ErrorMessageAsKey_LookupMiss_ReturnsNull()
    {
        var localizer = CreateLocalizer(translations: []);

        var result = localizer.ResolveErrorMessage(new()
        {
            MemberName = "Name",
            DisplayName = "Name",
            Attribute = new RequiredAttribute { ErrorMessage = "MissingKey" },
        });

        Assert.Null(result);
    }

    [Fact]
    public void ResolveErrorMessage_KeyProviderUsedWhenErrorMessageMissing()
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredAttribute_Default"] = "Required: {0}",
        };
        var localizer = CreateLocalizer(translations, options =>
        {
            options.ErrorMessageKeyProvider = ctx => $"{ctx.Attribute.GetType().Name}_Default";
        });

        var result = localizer.ResolveErrorMessage(new()
        {
            MemberName = "Name",
            DisplayName = "Name",
            Attribute = new RequiredAttribute(),
        });

        Assert.Equal("Required: Name", result);
    }

    [Fact]
    public void ResolveErrorMessage_KeyProviderReturnsNull_NoLookup_ReturnsNull()
    {
        var localizer = CreateLocalizer(new() { ["RequiredAttribute_Default"] = "Should not be used" },
            options => options.ErrorMessageKeyProvider = _ => null);

        var result = localizer.ResolveErrorMessage(new()
        {
            MemberName = "Name",
            DisplayName = "Name",
            Attribute = new RequiredAttribute(),
        });

        Assert.Null(result);
    }

    [Fact]
    public void ResolveErrorMessage_ExplicitErrorMessageWinsOverKeyProvider()
    {
        var translations = new Dictionary<string, string>
        {
            ["ExplicitKey"] = "Explicit: {0}",
            ["RequiredAttribute_Default"] = "Convention: {0}",
        };
        var localizer = CreateLocalizer(translations, options =>
            options.ErrorMessageKeyProvider = ctx => $"{ctx.Attribute.GetType().Name}_Default");

        var result = localizer.ResolveErrorMessage(new()
        {
            MemberName = "Name",
            DisplayName = "Name",
            Attribute = new RequiredAttribute { ErrorMessage = "ExplicitKey" },
        });

        Assert.Equal("Explicit: Name", result);
    }

    [Fact]
    public void ResolveErrorMessage_RangeAttribute_FormatsMinAndMax()
    {
        var translations = new Dictionary<string, string>
        {
            ["RangeKey"] = "{0} must be between {1} and {2}.",
        };
        var localizer = CreateLocalizer(translations);

        var result = localizer.ResolveErrorMessage(new()
        {
            MemberName = "Score",
            DisplayName = "Score",
            Attribute = new RangeAttribute(1, 100) { ErrorMessage = "RangeKey" },
        });

        Assert.Equal("Score must be between 1 and 100.", result);
    }

    [Fact]
    public void ResolveErrorMessage_AttributeWithoutFormatter_FormatsWithDisplayNameOnly()
    {
        // EmailAddressAttribute has no built-in multi-arg formatter; only {0} is substituted.
        var translations = new Dictionary<string, string>
        {
            ["EmailKey"] = "{0} is not valid.",
        };
        var localizer = CreateLocalizer(translations);

        var result = localizer.ResolveErrorMessage(new()
        {
            MemberName = "Email",
            DisplayName = "Email",
            Attribute = new EmailAddressAttribute { ErrorMessage = "EmailKey" },
        });

        Assert.Equal("Email is not valid.", result);
    }

    // --- LocalizerProvider ---

    [Fact]
    public void LocalizerProvider_InvokedWithDeclaringType()
    {
        var seenTypes = new List<Type>();
        var translations = new Dictionary<string, string> { ["Key"] = "Value" };
        var localizer = CreateLocalizer(translations, options =>
        {
            options.LocalizerProvider = (type, factory) =>
            {
                seenTypes.Add(type);
                return factory.Create(type);
            };
        });

        localizer.ResolveDisplayName(new()
        {
            MemberName = "M",
            DisplayName = "Key",
            DeclaringType = typeof(SomeModel),
        });

        Assert.Single(seenTypes);
        Assert.Equal(typeof(SomeModel), seenTypes[0]);
    }

    [Fact]
    public void LocalizerProvider_NullDeclaringType_FallsBackToObject()
    {
        // For parameter validation (DeclaringType is null), the provider receives typeof(object).
        var seenTypes = new List<Type>();
        var localizer = CreateLocalizer(translations: [], options =>
        {
            options.LocalizerProvider = (type, factory) =>
            {
                seenTypes.Add(type);
                return factory.Create(type);
            };
        });

        localizer.ResolveDisplayName(new()
        {
            MemberName = "param",
            DisplayName = "Param",
            DeclaringType = null,
        });

        Assert.Single(seenTypes);
        Assert.Equal(typeof(object), seenTypes[0]);
    }

    [Fact]
    public void LocalizerProvider_ReturnsNull_ThrowsInvalidOperationException()
    {
        var localizer = CreateLocalizer(translations: [], options =>
            options.LocalizerProvider = (_, _) => null!);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            localizer.ResolveDisplayName(new()
            {
                MemberName = "Name",
                DisplayName = "Customer Name",
                DeclaringType = typeof(object),
            }));

        Assert.Contains(nameof(ValidationLocalizationOptions.LocalizerProvider), ex.Message);
    }

    [Fact]
    public void LocalizerProvider_SharedResource_AllTypesUseSameLocalizer()
    {
        // Recommended pattern for shared validation resources.
        var perTypeTranslations = new Dictionary<Type, Dictionary<string, string>>
        {
            [typeof(SharedValidationMessages)] = new() { ["Required"] = "Required (shared)" },
        };
        var localizer = CreateLocalizerPerType(perTypeTranslations, options =>
            options.LocalizerProvider = (_, f) => f.Create(typeof(SharedValidationMessages)));

        var result = localizer.ResolveDisplayName(new()
        {
            MemberName = "Name",
            DisplayName = "Required",
            DeclaringType = typeof(SomeModel),
        });

        Assert.Equal("Required (shared)", result);
    }

    [Fact]
    public void LocalizerProvider_PerTypeIsolation()
    {
        // Per-type lookup ensures translations don't leak between types.
        var perTypeTranslations = new Dictionary<Type, Dictionary<string, string>>
        {
            [typeof(SomeModel)] = new() { ["Key"] = "Translation A" },
            [typeof(OtherModel)] = new() { ["Key"] = "Translation B" },
        };
        var localizer = CreateLocalizerPerType(perTypeTranslations);

        var resultA = localizer.ResolveDisplayName(new() { MemberName = "M", DisplayName = "Key", DeclaringType = typeof(SomeModel) });
        var resultB = localizer.ResolveDisplayName(new() { MemberName = "M", DisplayName = "Key", DeclaringType = typeof(OtherModel) });

        Assert.Equal("Translation A", resultA);
        Assert.Equal("Translation B", resultB);
    }

    // --- AttributeFormatters integration ---

    [Fact]
    public void ResolveErrorMessage_CustomAttributeFormatter_AppliedToTemplate()
    {
        var translations = new Dictionary<string, string>
        {
            ["CustomKey"] = "Custom {0}: extra={1}",
        };
        var localizer = CreateLocalizer(translations, options =>
            options.AttributeFormatters.AddFormatter<CustomAttribute>(
                attr => new CustomAttributeFormatter(attr)));

        var result = localizer.ResolveErrorMessage(new()
        {
            MemberName = "M",
            DisplayName = "TheField",
            Attribute = new CustomAttribute { ErrorMessage = "CustomKey", Extra = "EXTRA-VAL" },
        });

        Assert.Equal("Custom TheField: extra=EXTRA-VAL", result);
    }

    // --- Helpers ---

    private static IValidationLocalizer CreateLocalizer(
        Dictionary<string, string> translations,
        Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        var options = new ValidationLocalizationOptions();
        configureOptions?.Invoke(options);
        var factory = new TestStringLocalizerFactory(translations);
        return new DefaultValidationLocalizer(factory, Microsoft.Extensions.Options.Options.Create(options));
    }

    private static IValidationLocalizer CreateLocalizerPerType(
        Dictionary<Type, Dictionary<string, string>> perTypeTranslations,
        Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        var options = new ValidationLocalizationOptions();
        configureOptions?.Invoke(options);
        var factory = new TestStringLocalizerFactory(perTypeTranslations);
        return new DefaultValidationLocalizer(factory, Microsoft.Extensions.Options.Options.Create(options));
    }

    private sealed class SomeModel { public string? Name { get; set; } }
    private sealed class OtherModel { public string? Name { get; set; } }
    private sealed class SharedValidationMessages { }

    private sealed class CustomAttribute : ValidationAttribute
    {
        public string? Extra { get; set; }
    }

    private sealed class CustomAttributeFormatter(CustomAttribute attribute) : IValidationAttributeFormatter
    {
        public string FormatErrorMessage(System.Globalization.CultureInfo culture, string messageTemplate, string displayName)
            => string.Format(culture, messageTemplate, displayName, attribute.Extra);
    }
}
