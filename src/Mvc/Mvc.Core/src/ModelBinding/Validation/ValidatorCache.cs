// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    public class ValidatorCache
    {
        private readonly ConcurrentDictionary<ModelMetadata, CacheEntry> _cacheEntries = new ConcurrentDictionary<ModelMetadata, CacheEntry>();

        public IReadOnlyList<IModelValidator> GetValidators(ModelMetadata metadata, IModelValidatorProvider validatorProvider)
        {
            if (_cacheEntries.TryGetValue(metadata, out var entry))
            {
                return GetValidatorsFromEntry(entry, metadata, validatorProvider);
            }

            var items = new List<ValidatorItem>(metadata.ValidatorMetadata.Count);
            for (var i = 0; i < metadata.ValidatorMetadata.Count; i++)
            {
                items.Add(new ValidatorItem(metadata.ValidatorMetadata[i]));
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

        private IReadOnlyList<IModelValidator> GetValidatorsFromEntry(CacheEntry entry, ModelMetadata metadata, IModelValidatorProvider validationProvider)
        {
            Debug.Assert(entry.Validators != null || entry.Items != null);

            if (entry.Validators != null)
            {
                return entry.Validators;
            }

            var items = new List<ValidatorItem>(entry.Items.Count);
            for (var i = 0; i < entry.Items.Count; i++)
            {
                var item = entry.Items[i];
                if (item.IsReusable)
                {
                    items.Add(item);
                }
                else
                {
                    items.Add(new ValidatorItem(item.ValidatorMetadata));
                }
            }

            ExecuteProvider(validationProvider, metadata, items);

            return ExtractValidators(items);
        }

        private void ExecuteProvider(IModelValidatorProvider validatorProvider, ModelMetadata metadata, List<ValidatorItem> items)
        {
            var context = new ModelValidatorProviderContext(metadata, items);
            validatorProvider.CreateValidators(context);
        }

        private IReadOnlyList<IModelValidator> ExtractValidators(List<ValidatorItem> items)
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
                return Array.Empty<IModelValidator>();
            }

            var validators = new IModelValidator[count];

            var validatorIndex = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var validator = items[i].Validator;
                if (validator != null)
                {
                    validators[validatorIndex++] = validator;
                }
            }

            return validators;
        }

        private readonly struct CacheEntry
        {
            public CacheEntry(IReadOnlyList<IModelValidator> validators)
            {
                Validators = validators;
                Items = null;
            }

            public CacheEntry(List<ValidatorItem> items)
            {
                Items = items;
                Validators = null;
            }

            public IReadOnlyList<IModelValidator> Validators { get; }

            public List<ValidatorItem> Items { get; }
        }
    }
}
