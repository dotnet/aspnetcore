// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Default implementation of <see cref="IClientValidationService"/> that discovers
/// <see cref="ValidationAttribute"/>s on model properties via reflection, maps them
/// to <c>data-val-*</c> HTML attributes using registered adapters, and caches results.
/// When <see cref="ValidationOptions"/> is configured with localization providers,
/// error messages and display names are resolved using the current request culture.
/// </summary>
internal sealed class DefaultClientValidationService : IClientValidationService
{
    private readonly ClientValidationAdapterRegistry _adapterRegistry;
    private readonly ValidationOptions _validationOptions;
    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<(Type ModelType, string FieldName, string CultureName), IReadOnlyDictionary<string, string>> _cache = new();

    public DefaultClientValidationService(
        ClientValidationAdapterRegistry adapterRegistry,
        IOptions<ValidationOptions> validationOptions,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(adapterRegistry);
        ArgumentNullException.ThrowIfNull(validationOptions);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _adapterRegistry = adapterRegistry;
        _validationOptions = validationOptions.Value;
        _serviceProvider = serviceProvider;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2077",
        Justification = "Model types used with EditForm are expected to preserve public properties.")]
    public IReadOnlyDictionary<string, string> GetValidationAttributes(FieldIdentifier fieldIdentifier)
    {
        var cultureName = CultureInfo.CurrentUICulture.Name;
        var key = (fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName, cultureName);

        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = ComputeAttributes(key.Item1, key.Item2);
        _cache.TryAdd(key, result);

        return result;
    }

    private IReadOnlyDictionary<string, string> ComputeAttributes(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type modelType,
        string fieldName)
    {
        var property = modelType.GetProperty(fieldName);
        if (property is null)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true);
        var displayName = ResolveDisplayName(property, fieldName, modelType);
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var context = new ClientValidationContext(attributes);

        foreach (var validationAttribute in validationAttributes)
        {
            ThrowIfRemoteAttribute(validationAttribute, modelType, fieldName);

            var adapter = _adapterRegistry.GetAdapter(validationAttribute);
            if (adapter is not null)
            {
                var errorMessage = ResolveErrorMessage(validationAttribute, modelType, displayName);
                adapter.AddClientValidation(in context, errorMessage);
            }
        }

        if (attributes.Count == 0)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        return attributes;
    }

    private string ResolveDisplayName(PropertyInfo property, string fieldName, Type modelType)
    {
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();

        // If the DisplayAttribute uses compile-time resource localization (ResourceType is set),
        // GetName() returns the translated string — use it directly.
        if (displayAttribute?.ResourceType is not null)
        {
            return displayAttribute.GetName() ?? fieldName;
        }

        // Try the runtime localization provider (e.g. AddValidationLocalization<T>()).
        var displayName = displayAttribute?.Name;
        if (displayName is not null && _validationOptions.DisplayNameProvider is { } displayNameProvider)
        {
            var displayNameContext = new DisplayNameProviderContext
            {
                DeclaringType = modelType,
                Name = displayName,
                Services = _serviceProvider
            };

            return displayNameProvider(displayNameContext) ?? displayName;
        }

        // Fall back to DisplayNameAttribute or field name.
        var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();
        if (displayNameAttribute?.DisplayName is not null)
        {
            return displayNameAttribute.DisplayName;
        }

        return displayName ?? fieldName;
    }

    private string ResolveErrorMessage(ValidationAttribute attribute, Type modelType, string displayName)
    {
        // Skip if the attribute handles its own resource-based localization.
        if (attribute.ErrorMessageResourceType is not null)
        {
            return attribute.FormatErrorMessage(displayName);
        }

        // Try the localization provider (e.g. AddValidationLocalization<T>()).
        if (_validationOptions.ErrorMessageProvider is { } errorMessageProvider)
        {
            var errorMessageContext = new ErrorMessageProviderContext
            {
                Attribute = attribute,
                DisplayName = displayName,
                DeclaringType = modelType,
                Services = _serviceProvider
            };

            var localizedMessage = errorMessageProvider(errorMessageContext);
            if (localizedMessage is not null)
            {
                return localizedMessage;
            }
        }

        return attribute.FormatErrorMessage(displayName);
    }

    /// <summary>
    /// Throws <see cref="NotSupportedException"/> if the attribute inherits from
    /// <c>Microsoft.AspNetCore.Mvc.RemoteAttributeBase</c>. RemoteAttribute depends on MVC
    /// conventions (URL routing, controller endpoints) that are not available in Blazor.
    /// The check is done by type name to avoid an assembly dependency on Mvc.ViewFeatures.
    /// </summary>
    private static void ThrowIfRemoteAttribute(ValidationAttribute attribute, Type modelType, string fieldName)
    {
        var type = attribute.GetType();
        while (type is not null)
        {
            if (string.Equals(type.FullName, "Microsoft.AspNetCore.Mvc.RemoteAttributeBase", StringComparison.Ordinal))
            {
                throw new NotSupportedException(
                    $"The '{attribute.GetType().Name}' attribute on property '{fieldName}' of type " +
                    $"'{modelType.Name}' is not supported for client-side validation in Blazor. " +
                    $"RemoteAttribute requires server-side AJAX endpoints which are an MVC pattern. " +
                    $"Consider using a custom validation approach for Blazor forms.");
            }

            type = type.BaseType;
        }
    }
}
