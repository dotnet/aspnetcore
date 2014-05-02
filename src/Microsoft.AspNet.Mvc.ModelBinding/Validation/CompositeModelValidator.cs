// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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

        private static ModelValidationResult CreateSubPropertyResult(ModelMetadata propertyMetadata, ModelValidationResult propertyResult)
        {
            return new ModelValidationResult(propertyMetadata.PropertyName + '.' + propertyResult.MemberName,
                                             propertyResult.Message);
        }
    }
}
