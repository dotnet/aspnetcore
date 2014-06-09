// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CompositeModelValidator : IModelValidator
    {
        private readonly IEnumerable<IModelValidator> _validators;

        public CompositeModelValidator(IEnumerable<IModelValidator> validators)
        {
            _validators = validators;
        }

        public bool IsRequired
        {
            get { return false; }
        }

        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
        {
            var propertiesValid = true;
            var metadata = context.ModelMetadata;

            foreach (var propertyMetadata in metadata.Properties)
            {
                var propertyContext = new ModelValidationContext(context, propertyMetadata);

                foreach (var propertyValidator in _validators)
                {
                    foreach (var validationResult in propertyValidator.Validate(propertyContext))
                    {
                        propertiesValid = false;
                        yield return CreateSubPropertyResult(propertyMetadata, validationResult);
                    }
                }
            }

            if (propertiesValid)
            {
                foreach (var typeValidator in _validators)
                {
                    foreach (var typeResult in typeValidator.Validate(context))
                    {
                        yield return typeResult;
                    }
                }
            }
        }

        private static ModelValidationResult CreateSubPropertyResult(ModelMetadata propertyMetadata,
                                                                     ModelValidationResult propertyResult)
        {
            return new ModelValidationResult(propertyMetadata.PropertyName + '.' + propertyResult.MemberName,
                                             propertyResult.Message);
        }
    }
}
