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
            if (IsUnionValueProperty(entry.Metadata, model.GetType()))
            {
                hasValueProperty = true;
            }

            yield return entry;
        }

        if (!hasValueProperty)
        {
            // IUnion.Value can be implemented explicitly.
            yield return new ValidationEntry(
                metadata is DefaultModelMetadata defaultMetadata
                    ? defaultMetadata.CreateUnionValueMetadata()
                    : _metadataProvider.GetMetadataForType(typeof(object)),
                ModelNames.CreatePropertyModelName(key, nameof(IUnion.Value)),
                () => ((IUnion)model).Value);
        }
    }

    internal static bool IsUnionValueProperty(ModelMetadata metadata, Type unionType)
    {
        if (metadata.MetadataKind != ModelMetadataKind.Property || metadata.PropertyName != nameof(IUnion.Value))
        {
            return false;
        }

        var getter = metadata is DefaultModelMetadata propertyMetadata
            ? propertyMetadata.Details.Key.PropertyInfo?.GetMethod
            : metadata.ContainerType?.GetProperty(nameof(IUnion.Value))?.GetMethod;
        var unionGetter = unionType.GetInterfaceMap(typeof(IUnion)).TargetMethods[0];

        return getter is not null && (getter.DeclaringType == typeof(IUnion) || getter.GetBaseDefinition() == unionGetter.GetBaseDefinition());
    }
}
