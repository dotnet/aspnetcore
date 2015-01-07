// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An implementation of <see cref="IModelValidatorProvider"/> which providers validators
    /// for attributes which derive from <see cref="ValidationAttribute"/>. It also provides
    /// a validator for types which implement <see cref="IValidatableObject"/>. To support
    /// client side validation, you can either register adapters through the static methods
    /// on this class, or by having your validation attributes implement
    /// <see cref="IClientModelValidator"/>. The logic to support <see cref="IClientModelValidator"/>
    /// is implemented in <see cref="DataAnnotationsModelValidator"/>.
    /// </summary>
    public class DataAnnotationsModelValidatorProvider : AssociatedValidatorProvider
    {
        // A factory for validators based on ValidationAttribute.
        internal delegate IModelValidator DataAnnotationsModelValidationFactory(ValidationAttribute attribute);

        // Factories for validation attributes
        private static readonly DataAnnotationsModelValidationFactory _defaultAttributeFactory =
            (attribute) => new DataAnnotationsModelValidator(attribute);

        // Factories for IValidatableObject models
        private static readonly DataAnnotationsValidatableObjectAdapterFactory _defaultValidatableFactory =
            () => new ValidatableObjectAdapter();

        private static bool _addImplicitRequiredAttributeForValueTypes = true;
        private readonly Dictionary<Type, DataAnnotationsModelValidationFactory> _attributeFactories =
            BuildAttributeFactoriesDictionary();

        // A factory for validators based on IValidatableObject
        private delegate IModelValidator DataAnnotationsValidatableObjectAdapterFactory();

        internal Dictionary<Type, DataAnnotationsModelValidationFactory> AttributeFactories
        {
            get { return _attributeFactories; }
        }

        private static bool AddImplicitRequiredAttributeForValueTypes
        {
            get { return _addImplicitRequiredAttributeForValueTypes; }
            set { _addImplicitRequiredAttributeForValueTypes = value; }
        }

        protected override IEnumerable<IModelValidator> GetValidators(ModelMetadata metadata,
                                                                      IEnumerable<Attribute> attributes)
        {
            var results = new List<IModelValidator>();

            // Produce a validator for each validation attribute we find
            foreach (var attribute in attributes.OfType<ValidationAttribute>())
            {
                DataAnnotationsModelValidationFactory factory;
                if (!_attributeFactories.TryGetValue(attribute.GetType(), out factory))
                {
                    factory = _defaultAttributeFactory;
                }
                results.Add(factory(attribute));
            }

            // Produce a validator if the type supports IValidatableObject
            if (typeof(IValidatableObject).IsAssignableFrom(metadata.ModelType))
            {
                results.Add(_defaultValidatableFactory());
            }

            return results;
        }

        private static Dictionary<Type, DataAnnotationsModelValidationFactory> BuildAttributeFactoriesDictionary()
        {
            var dict = new Dictionary<Type, DataAnnotationsModelValidationFactory>();
            AddValidationAttributeAdapter(dict, typeof(RegularExpressionAttribute),
                (attribute) => new RegularExpressionAttributeAdapter((RegularExpressionAttribute)attribute));

            AddValidationAttributeAdapter(dict, typeof(MaxLengthAttribute),
                (attribute) => new MaxLengthAttributeAdapter((MaxLengthAttribute)attribute));

            AddValidationAttributeAdapter(dict, typeof(MinLengthAttribute),
                (attribute) => new MinLengthAttributeAdapter((MinLengthAttribute)attribute));

            AddValidationAttributeAdapter(dict, typeof(CompareAttribute),
                (attribute) => new CompareAttributeAdapter((CompareAttribute)attribute));

            AddValidationAttributeAdapter(dict, typeof(RequiredAttribute),
                (attribute) => new RequiredAttributeAdapter((RequiredAttribute)attribute));

            AddValidationAttributeAdapter(dict, typeof(RangeAttribute),
                (attribute) => new RangeAttributeAdapter((RangeAttribute)attribute));

            AddValidationAttributeAdapter(dict, typeof(StringLengthAttribute),
                (attribute) => new StringLengthAttributeAdapter((StringLengthAttribute)attribute));

            AddDataTypeAttributeAdapter(dict, typeof(CreditCardAttribute), "creditcard");
            AddDataTypeAttributeAdapter(dict, typeof(EmailAddressAttribute), "email");
            AddDataTypeAttributeAdapter(dict, typeof(PhoneAttribute), "phone");
            AddDataTypeAttributeAdapter(dict, typeof(UrlAttribute), "url");

            return dict;
        }

        private static void AddValidationAttributeAdapter(
            Dictionary<Type, DataAnnotationsModelValidationFactory> dictionary,
            Type validationAttributeType,
            DataAnnotationsModelValidationFactory factory)
        {
            if (validationAttributeType != null)
            {
                dictionary.Add(validationAttributeType, factory);
            }
        }

        private static void AddDataTypeAttributeAdapter(
            Dictionary<Type, DataAnnotationsModelValidationFactory> dictionary,
            Type attributeType,
            string ruleName)
        {
            AddValidationAttributeAdapter(
                dictionary,
                attributeType,
                (attribute) => new DataTypeAttributeAdapter((DataTypeAttribute)attribute, ruleName));
        }
    }
}
