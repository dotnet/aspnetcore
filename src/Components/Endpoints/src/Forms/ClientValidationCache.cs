// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

using FieldKey = (Type ModelType, string FieldName);

internal sealed class ClientValidationCache : IDisposable
{
    private readonly ConcurrentDictionary<FieldKey, ClientValidationFieldMetadata?> _metadataCache = new();
    private readonly ConcurrentDictionary<Type, bool> _typeHasValidatableInfo = new();
    private readonly ConcurrentDictionary<FieldKey, bool> _propertyHasValidatableInfo = new();
    private readonly ValidationOptions _validationOptions;

    [UnconditionalSuppressMessage("Trimming", "IL2066",
        Justification = "Preserves ValidationOptions's parameterless constructor used by Microsoft.Extensions.Options to materialize IOptions<ValidationOptions>.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ValidationOptions))]
    public ClientValidationCache(IOptions<ValidationOptions> validationOptions)
    {
        _validationOptions = validationOptions.Value;

        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearCache;
        }
    }

    public IEnumerable<(string renderedName, ClientValidationFieldMetadata metadata)> GetValidatableFieldMetadata(
        IReadOnlyDictionary<FieldIdentifier, string> fields,
        object formModel)
    {
        // The form model type determines which validation pipeline (DataAnnotations.Validator vs MEV) the server uses.
        var formHasValidatableInfo = HasValidatableTypeInfo(formModel.GetType());

        foreach (var (fieldIdentifier, renderedName) in fields)
        {
            // Don't enable client-side validation for fields that would not get server-side validation
            // to help developers avoid security mistakes.
            if (!IsServerValidatable(fieldIdentifier, formModel, formHasValidatableInfo))
            {
                continue;
            }

            var fieldKey = (fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
            var cachedMetadata = _metadataCache.GetOrAdd(
                fieldKey,
                static key => BuildFieldMetadata(key.ModelType, key.FieldName));

            if (cachedMetadata is { } fieldMetadata)
            {
                yield return (renderedName, fieldMetadata);
            }
        }
    }

    /// <summary>
    /// Determines whether the server will validate the specified field on submit, which is the
    /// condition under which client-side rules may be safely emitted.
    /// </summary>
    /// <param name="fieldIdentifier">The field an input was rendered for.</param>
    /// <param name="formModel">The form's top-level model (<see cref="EditContext.Model"/>).</param>
    /// <param name="formHasValidatableInfo">
    /// Whether the form model type is recognized by MEV. Computed once per form by the caller via
    /// <see cref="HasValidatableTypeInfo"/> and threaded in so the per-field path does no extra
    /// type-level lookup.
    /// </param>
    private bool IsServerValidatable(in FieldIdentifier fieldIdentifier, object formModel, bool formHasValidatableInfo)
    {
        if (formHasValidatableInfo)
        {
            // MEV submit path (ValidateAsync) recurses. A field is validated iff MEV has
            // ValidatablePropertyInfo for it on its owner type. This also naturally excludes
            // members/types filtered by [SkipValidation], matching the server.
            return HasValidatablePropertyInfo(fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
        }
        else
        {
            // DataAnnotations submit path (Validator.TryValidateObject) validates only the form
            // model's top-level properties and does not recurse. A field is top-level iff its owner
            // is the form model instance.
            return ReferenceEquals(fieldIdentifier.Model, formModel);
        }
    }

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates.
    /// <summary>
    /// Returns whether MEV recognizes <paramref name="type"/> as a validatable type. Cached.
    /// Returns <see langword="false"/> when MEV is not configured.
    /// </summary>
    private bool HasValidatableTypeInfo(Type type) =>
        _validationOptions.Resolvers.Count > 0
            && _typeHasValidatableInfo.GetOrAdd(type,
                key => _validationOptions.TryGetValidatableTypeInfo(key, out _));

    private bool HasValidatablePropertyInfo(Type ownerType, string fieldName) =>
        _validationOptions.Resolvers.Count > 0
            && _propertyHasValidatableInfo.GetOrAdd((ownerType, fieldName),
                key => _validationOptions.TryGetValidatablePropertyInfo(key.ModelType, key.FieldName, out _));
#pragma warning restore ASP0029

    private void ClearCache()
    {
        _metadataCache.Clear();
        _typeHasValidatableInfo.Clear();
        _propertyHasValidatableInfo.Clear();
    }

    public void Dispose()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied -= ClearCache;
        }
    }

    // Builds reflection metadata for a single field. Culture-independent; localized text is
    // resolved per call by the provider. At most one of ResourceDisplayAttribute and
    // LiteralDisplayName is non-null; both null means the property has no display attribute.
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Model types are application code and are preserved by default.")]
    private static ClientValidationFieldMetadata? BuildFieldMetadata(Type modelType, string propertyName)
    {
        var property = modelType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        if (property is null)
        {
            return null;
        }

        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToArray();

        if (validationAttributes.Length == 0)
        {
            return null;
        }

        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>(inherit: true);
        DisplayAttribute? resourceDisplayAttribute = null;
        string? literalDisplayName = null;

        if (displayAttribute is { ResourceType: not null, Name: not null })
        {
            resourceDisplayAttribute = displayAttribute;
        }
        else
        {
            literalDisplayName = displayAttribute?.Name
                ?? property.GetCustomAttribute<DisplayNameAttribute>(inherit: true)?.DisplayName;
        }

        return new ClientValidationFieldMetadata(
            propertyName: property.Name,
            validationAttributes,
            declaringType: property.DeclaringType,
            resourceDisplayAttribute,
            literalDisplayName);
    }
}
