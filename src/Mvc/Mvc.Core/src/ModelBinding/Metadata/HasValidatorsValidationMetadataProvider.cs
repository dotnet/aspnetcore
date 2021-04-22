// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    internal class HasValidatorsValidationMetadataProvider : IValidationMetadataProvider
    {
        private readonly bool _hasOnlyMetadataBasedValidators;
        private readonly IMetadataBasedModelValidatorProvider[]? _validatorProviders;

        public HasValidatorsValidationMetadataProvider(IList<IModelValidatorProvider> modelValidatorProviders)
        {
            if (modelValidatorProviders.Count > 0 && modelValidatorProviders.All(p => p is IMetadataBasedModelValidatorProvider))
            {
                _hasOnlyMetadataBasedValidators = true;
                _validatorProviders = modelValidatorProviders.Cast<IMetadataBasedModelValidatorProvider>().ToArray();
            }
        }

        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!_hasOnlyMetadataBasedValidators)
            {
                return;
            }

            for (var i = 0; i < _validatorProviders!.Length; i++)
            {
                var provider = _validatorProviders[i];
                if (provider.HasValidators(context.Key.ModelType, context.ValidationMetadata.ValidatorMetadata))
                {
                    context.ValidationMetadata.HasValidators = true;

                    if (context.Key.MetadataKind == ModelMetadataKind.Property)
                    {
                        // For properties, additionally determine that if there's validators defined exclusively
                        // from property attributes. This is later used to produce a error for record types
                        // where a record type property that is bound as a parameter defines validation attributes.

                        if (context.PropertyAttributes is not IList<object> propertyAttributes)
                        {
                            propertyAttributes = context.PropertyAttributes!.ToList();
                        }

                        if (provider.HasValidators(typeof(object), propertyAttributes))
                        {
                            context.ValidationMetadata.PropertyHasValidators = true;
                        }
                    }
                }
            }

            if (context.ValidationMetadata.HasValidators == null)
            {
                context.ValidationMetadata.HasValidators = false;
            }
        }
    }
}
