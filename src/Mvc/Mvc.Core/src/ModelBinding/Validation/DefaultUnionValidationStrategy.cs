// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

internal sealed class DefaultUnionValidationStrategy : IValidationStrategy
{
    private readonly IModelMetadataProvider _metadataProvider;

    public DefaultUnionValidationStrategy(IModelMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    public IEnumerator<ValidationEntry> GetChildren(ModelMetadata metadata, string key, object model)
    {
        var hasValueProperty = false;
        using var enumerator = DefaultComplexObjectValidationStrategy.Instance.GetChildren(metadata, key, model);
        while (enumerator.MoveNext())
        {
            var entry = enumerator.Current;
            if (entry.Metadata.PropertyName != nameof(IUnion.Value))
            {
                yield return entry;
                continue;
            }

            hasValueProperty = true;
            if (entry.Metadata.ModelType != typeof(object))
            {
                yield return entry;
                continue;
            }

            yield return new ValidationEntry(entry.Metadata, entry.Key, () => ((IUnion)model).Value);
        }

        if (!hasValueProperty)
        {
            // IUnion.Value can be implemented explicitly.
            yield return new ValidationEntry(
                _metadataProvider.GetMetadataForType(typeof(object)),
                ModelNames.CreatePropertyModelName(key, nameof(IUnion.Value)),
                () => ((IUnion)model).Value);
        }
    }

    public ModelMetadata GetChildMetadata(ModelMetadata metadata, object? value)
    {
        if (metadata.ModelType != typeof(object) || value is null ||
            (metadata.MetadataKind != ModelMetadataKind.Type && metadata.PropertyName != nameof(IUnion.Value)))
        {
            return metadata;
        }

        // Retain the Value property's validation attributes and filters while validating the active case.
        return _metadataProvider is ModelMetadataProvider provider &&
            metadata is DefaultModelMetadata propertyMetadata &&
            propertyMetadata.Details.Key.PropertyInfo is { } propertyInfo
            ? provider.GetMetadataForProperty(propertyInfo, value.GetType())
            : _metadataProvider.GetMetadataForType(value.GetType());
    }
}
