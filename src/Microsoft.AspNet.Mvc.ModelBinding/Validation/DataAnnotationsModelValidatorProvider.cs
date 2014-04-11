// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An implementation of <see cref="ModelValidatorProvider"/> which providers validators
    /// for attributes which derive from <see cref="ValidationAttribute"/>. It also provides
    /// a validator for types which implement <see cref="IValidatableObject"/>. To support
    /// client side validation, you can either register adapters through the static methods
    /// on this class, or by having your validation attributes implement
    /// <see cref="IClientValidatable"/>. The logic to support IClientValidatable
    /// is implemented in <see cref="DataAnnotationsModelValidator"/>.
    /// </summary>
    public class DataAnnotationsModelValidatorProvider : AssociatedValidatorProvider
    {
        // A factory for validators based on ValidationAttribute
        private delegate IModelValidator DataAnnotationsModelValidationFactory(ValidationAttribute attribute);

        // A factory for validators based on IValidatableObject
        private delegate IModelValidator DataAnnotationsValidatableObjectAdapterFactory();

        private static bool _addImplicitRequiredAttributeForValueTypes = true;

        // Factories for validation attributes
        private static DataAnnotationsModelValidationFactory DefaultAttributeFactory =
            (attribute) => new DataAnnotationsModelValidator(attribute);

        private static Dictionary<Type, DataAnnotationsModelValidationFactory> AttributeFactories =
            new Dictionary<Type, DataAnnotationsModelValidationFactory>();

        // Factories for IValidatableObject models
        private static DataAnnotationsValidatableObjectAdapterFactory DefaultValidatableFactory =
            () => new ValidatableObjectAdapter();

        private static Dictionary<Type, DataAnnotationsValidatableObjectAdapterFactory> ValidatableFactories =
            new Dictionary<Type, DataAnnotationsValidatableObjectAdapterFactory>();

        public static bool AddImplicitRequiredAttributeForValueTypes
        {
            get { return _addImplicitRequiredAttributeForValueTypes; }
            set { _addImplicitRequiredAttributeForValueTypes = value; }
        }

        protected override IEnumerable<IModelValidator> GetValidators(ModelMetadata metadata, IEnumerable<Attribute> attributes)
        {
            var results = new List<IModelValidator>();

            // Produce a validator for each validation attribute we find
            foreach (var attribute in attributes.OfType<ValidationAttribute>())
            {
                DataAnnotationsModelValidationFactory factory;
                if (!AttributeFactories.TryGetValue(attribute.GetType(), out factory))
                {
                    factory = DefaultAttributeFactory;
                }
                results.Add(factory(attribute));
            }

            // Produce a validator if the type supports IValidatableObject
            if (typeof(IValidatableObject).IsAssignableFrom(metadata.ModelType))
            {
                DataAnnotationsValidatableObjectAdapterFactory factory;
                if (!ValidatableFactories.TryGetValue(metadata.ModelType, out factory))
                {
                    factory = DefaultValidatableFactory;
                }
                results.Add(factory());
            }

            return results;
        }
    }
}
