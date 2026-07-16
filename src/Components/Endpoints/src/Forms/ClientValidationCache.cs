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
    private readonly ConcurrentDictionary<(Type FormType, string RenderedName), bool> _fieldReachabilityCache = new();
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
        var formType = formModel.GetType();

        // The form model type determines which validation pipeline (DataAnnotations.Validator vs MEV) the server uses.
        var formHasValidatableInfo = HasValidatableTypeInfo(formType);

        foreach (var (fieldIdentifier, renderedName) in fields)
        {
            // Don't enable client-side validation for fields that would not get server-side validation
            // to help developers avoid security mistakes.
            if (!IsServerValidatable(fieldIdentifier, renderedName, formType, formModel, formHasValidatableInfo))
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

    private bool IsServerValidatable(
        in FieldIdentifier fieldIdentifier,
        string renderedName,
        Type formType,
        object formModel,
        bool formHasValidatableInfo)
    {
        if (formHasValidatableInfo)
        {
            // MEV submit path (ValidateAsync) validates a field only if it recurses to it from th form model.
            // The result depends only on the form type + rendered name, so it is cached across requests.
            return _fieldReachabilityCache.GetOrAdd(
                (formType, renderedName),
                static (key, self) => self.IsFieldValidatedByMev(key.FormType, key.RenderedName),
                this);
        }
        else
        {
            // DataAnnotations submit path (Validator.TryValidateObject) validates only the form
            // model's top-level properties and does not recurse.
            // A field is top-level iff its owner is the form model instance.
            return ReferenceEquals(fieldIdentifier.Model, formModel);
        }
    }

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates.
    private bool IsFieldValidatedByMev(Type formType, string renderedName)
    {
        var path = GetModelRelativePath(renderedName);
        if (path.Count == 0)
        {
            return false;
        }

        var currentType = formType;
        for (var i = 0; i < path.Count - 1; i++)
        {
            if (!_validationOptions.TryGetValidatableTypeInfo(currentType, out var typeInfo)
                || !typeInfo.TryFindProperty(path[i], _validationOptions, out _))
            {
                return false;
            }

            var memberType = GetRecursableMemberType(currentType, path[i]);
            if (memberType is null || !_validationOptions.TryGetValidatableTypeInfo(memberType, out _))
            {
                // MEV only recurses into a member whose type is itself validatable.
                return false;
            }

            currentType = memberType;
        }

        // The leaf must be a member MEV validates on the owner type.
        return _validationOptions.TryGetValidatableTypeInfo(currentType, out var ownerInfo)
            && ownerInfo.TryFindProperty(path[^1], _validationOptions, out _);
    }

    private bool HasValidatableTypeInfo(Type type) =>
        _validationOptions.Resolvers.Count > 0
            && _typeHasValidatableInfo.GetOrAdd(type,
                key => _validationOptions.TryGetValidatableTypeInfo(key, out _));
#pragma warning restore ASP0029

    // Derives the field's property-name path relative to the form model from the rendered HTML name.
    // The rendered name is the binder key "{modelPrefix}.{path}" (e.g. "Person.Address.Street"):
    // the single leading prefix segment maps to the form model, and collection indices ("Items[0]")
    // do not affect type reachability, so both are dropped.
    private static List<string> GetModelRelativePath(string renderedName)
    {
        var segments = new List<string>();

        var start = renderedName.IndexOf('.') + 1; // skip the leading model-prefix segment
        if (start <= 0)
        {
            return segments; // no '.', so there is nothing below the model root
        }

        foreach (var rawSegment in renderedName.Substring(start).Split('.'))
        {
            var bracketIndex = rawSegment.IndexOf('[');
            var name = bracketIndex >= 0 ? rawSegment[..bracketIndex] : rawSegment;
            if (name.Length > 0)
            {
                segments.Add(name);
            }
        }

        return segments;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Model types are application code and are preserved by default.")]
    private static Type? GetRecursableMemberType(Type ownerType, string propertyName)
    {
        var propertyType = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.PropertyType;
        if (propertyType is null)
        {
            return null;
        }

        propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (propertyType != typeof(string) && GetEnumerableElementType(propertyType) is { } elementType)
        {
            return elementType;
        }

        return propertyType;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Model types are application code and are preserved by default.")]
    private static Type? GetEnumerableElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private void ClearCache()
    {
        _metadataCache.Clear();
        _typeHasValidatableInfo.Clear();
        _fieldReachabilityCache.Clear();
    }

    public void Dispose()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied -= ClearCache;
        }
    }

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
