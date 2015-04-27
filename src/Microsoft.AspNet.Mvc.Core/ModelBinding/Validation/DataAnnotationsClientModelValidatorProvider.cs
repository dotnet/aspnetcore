// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// An implementation of <see cref="IClientModelValidatorProvider"/> which provides client validators
    /// for attributes which derive from <see cref="ValidationAttribute"/>. It also provides
    /// a validator for types which implement <see cref="IClientModelValidator"/>.
    /// The logic to support <see cref="IClientModelValidator"/>
    /// is implemented in <see cref="DataAnnotationsClientModelValidator{}"/>.
    /// </summary>
    public class DataAnnotationsClientModelValidatorProvider : IClientModelValidatorProvider
    {
        // A factory for validators based on ValidationAttribute.
        internal delegate IClientModelValidator
            DataAnnotationsClientModelValidationFactory(ValidationAttribute attribute);

        private readonly Dictionary<Type, DataAnnotationsClientModelValidationFactory> _attributeFactories =
            BuildAttributeFactoriesDictionary();

        internal Dictionary<Type, DataAnnotationsClientModelValidationFactory> AttributeFactories
        {
            get { return _attributeFactories; }
        }

        /// <inheritdoc />
        public void GetValidators(ClientValidatorProviderContext context)
        {
            foreach (var attribute in context.ValidatorMetadata.OfType<ValidationAttribute>())
            {
                DataAnnotationsClientModelValidationFactory factory;
                if (_attributeFactories.TryGetValue(attribute.GetType(), out factory))
                {
                    context.Validators.Add(factory(attribute));
                }
            }
        }

        private static Dictionary<Type, DataAnnotationsClientModelValidationFactory> BuildAttributeFactoriesDictionary()
        {
            return new Dictionary<Type, DataAnnotationsClientModelValidationFactory>()
            {
                {
                    typeof(RegularExpressionAttribute),
                    (attribute) => new RegularExpressionAttributeAdapter((RegularExpressionAttribute)attribute)
                },
                {
                    typeof(MaxLengthAttribute),
                    (attribute) => new MaxLengthAttributeAdapter((MaxLengthAttribute)attribute)
                },
                {
                    typeof(MinLengthAttribute),
                    (attribute) => new MinLengthAttributeAdapter((MinLengthAttribute)attribute)
                },
                {
                    typeof(CompareAttribute),
                    (attribute) => new CompareAttributeAdapter((CompareAttribute)attribute)
                },
                {
                    typeof(RequiredAttribute),
                    (attribute) => new RequiredAttributeAdapter((RequiredAttribute)attribute)
                },
                {
                    typeof(RangeAttribute),
                    (attribute) => new RangeAttributeAdapter((RangeAttribute)attribute)
                },
                {
                    typeof(StringLengthAttribute),
                    (attribute) => new StringLengthAttributeAdapter((StringLengthAttribute)attribute)
                },
                {
                    typeof(CreditCardAttribute),
                    (attribute) => new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "creditcard")
                },
                {
                    typeof(EmailAddressAttribute),
                    (attribute) => new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "email")
                },
                {
                    typeof(PhoneAttribute),
                    (attribute) => new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "phone")
                },
                {
                    typeof(UrlAttribute),
                    (attribute) => new DataTypeAttributeAdapter((DataTypeAttribute)attribute, "url")
                }
            };
        }
    }
}
