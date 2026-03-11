// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Default implementation of <see cref="IClientValidationService"/> that discovers
/// <see cref="ValidationAttribute"/>s on model properties via reflection, maps them
/// to <c>data-val-*</c> HTML attributes using registered adapters, and caches results.
/// </summary>
internal sealed class DefaultClientValidationService : IClientValidationService
{
    private readonly ClientValidationAdapterRegistry _adapterRegistry;

    private readonly ConcurrentDictionary<(Type ModelType, string FieldName), IReadOnlyDictionary<string, string>> _cache = new();

    public DefaultClientValidationService(ClientValidationAdapterRegistry adapterRegistry)
    {
        ArgumentNullException.ThrowIfNull(adapterRegistry);
        _adapterRegistry = adapterRegistry;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2077",
        Justification = "Model types used with EditForm are expected to preserve public properties.")]
    public IReadOnlyDictionary<string, string> GetValidationAttributes(FieldIdentifier fieldIdentifier)
    {
        var key = (fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);

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
        var displayName = ResolveDisplayName(property, fieldName);
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var context = new ClientValidationContext(attributes);

        foreach (var validationAttribute in validationAttributes)
        {
            ThrowIfRemoteAttribute(validationAttribute, modelType, fieldName);

            var adapter = _adapterRegistry.GetAdapter(validationAttribute);
            if (adapter is not null)
            {
                var errorMessage = validationAttribute.FormatErrorMessage(displayName);
                adapter.AddClientValidation(in context, errorMessage);
            }
        }

        if (attributes.Count == 0)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        return attributes;
    }

    private static string ResolveDisplayName(PropertyInfo property, string fieldName)
    {
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
        if (displayAttribute is not null)
        {
            var name = displayAttribute.GetName();
            if (name is not null)
            {
                return name;
            }
        }

        var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();
        if (displayNameAttribute?.DisplayName is not null)
        {
            return displayNameAttribute.DisplayName;
        }

        return fieldName;
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
