// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// Cache for <see cref="IClientModelValidator"/>s.
/// </summary>
public class ClientValidatorCache
{
    private readonly ConcurrentDictionary<ModelMetadata, CacheEntry> _cacheEntries = new ConcurrentDictionary<ModelMetadata, CacheEntry>();

    /// <summary>
    /// Gets the <see cref="IClientModelValidator"/> for the metadata from the cache, using the validatorProvider to create when needed.
    /// </summary>
    /// <param name="metadata">The <see cref="ModelMetadata"/> being validated.</param>
    /// <param name="validatorProvider">The <see cref="IClientModelValidatorProvider"/> which will be used to create validators when needed.</param>
    /// <returns>The list of <see cref="IClientModelValidator"/>s.</returns>
    public IReadOnlyList<IClientModelValidator> GetValidators(ModelMetadata metadata, IClientModelValidatorProvider validatorProvider)
    {
        if (metadata.MetadataKind == ModelMetadataKind.Property &&
            metadata.ContainerMetadata?.BoundConstructor != null &&
            metadata.ContainerMetadata.BoundConstructorPropertyMapping.TryGetValue(metadata, out var parameter))
        {
            // "metadata" typically points to properties. When working with record types, we want to read validation details from the
            // constructor parameter instead. So let's switch it out.
            metadata = parameter;
        }

        if (_cacheEntries.TryGetValue(metadata, out var entry))
        {
            return GetValidatorsFromEntry(entry, metadata, validatorProvider);
        }

        var items = new List<ClientValidatorItem>(metadata.ValidatorMetadata.Count);
        for (var i = 0; i < metadata.ValidatorMetadata.Count; i++)
        {
            items.Add(new ClientValidatorItem(metadata.ValidatorMetadata[i]));
        }

        ExecuteProvider(validatorProvider, metadata, items);

        var validators = ExtractValidators(items);

        var allValidatorsCached = true;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!item.IsReusable)
            {
                item.Validator = null;
                allValidatorsCached = false;
            }
        }

        if (allValidatorsCached)
        {
            entry = new CacheEntry(validators);
        }
        else
        {
            entry = new CacheEntry(items);
        }

        _cacheEntries.TryAdd(metadata, entry);

        return validators;
    }

    private static IReadOnlyList<IClientModelValidator> GetValidatorsFromEntry(CacheEntry entry, ModelMetadata metadata, IClientModelValidatorProvider validationProvider)
    {
        if (entry.Validators != null)
        {
            return entry.Validators;
        }

        Debug.Assert(entry.Items != null);

        var items = new List<ClientValidatorItem>(entry.Items.Count);
        for (var i = 0; i < entry.Items.Count; i++)
        {
            var item = entry.Items[i];
            if (item.IsReusable)
            {
                items.Add(item);
            }
            else
            {
                items.Add(new ClientValidatorItem(item.ValidatorMetadata));
            }
        }

        ExecuteProvider(validationProvider, metadata, items);

        return ExtractValidators(items);
    }

    private static void ExecuteProvider(IClientModelValidatorProvider validatorProvider, ModelMetadata metadata, List<ClientValidatorItem> items)
    {
        var context = new ClientValidatorProviderContext(metadata, items);

        validatorProvider.CreateValidators(context);
    }

    private static IReadOnlyList<IClientModelValidator> ExtractValidators(List<ClientValidatorItem> items)
    {
        var count = 0;
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Validator != null)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return Array.Empty<IClientModelValidator>();
        }

        var validators = new IClientModelValidator[count];
        var clientValidatorIndex = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var validator = items[i].Validator;
            if (validator != null)
            {
                validators[clientValidatorIndex++] = validator;
            }
        }

        return validators;
    }

    private readonly struct CacheEntry
    {
        public CacheEntry(IReadOnlyList<IClientModelValidator> validators)
        {
            Validators = validators;
            Items = null;
        }

        public CacheEntry(List<ClientValidatorItem> items)
        {
            Items = items;
            Validators = null;
        }

        public IReadOnlyList<IClientModelValidator>? Validators { get; }

        public List<ClientValidatorItem>? Items { get; }
    }
}
