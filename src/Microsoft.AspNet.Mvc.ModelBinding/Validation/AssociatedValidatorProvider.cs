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

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public abstract class AssociatedValidatorProvider : IModelValidatorProvider
    {
        public IEnumerable<IModelValidator> GetValidators([NotNull] ModelMetadata metadata)
        {
            if (metadata.ContainerType != null && !string.IsNullOrEmpty(metadata.PropertyName))
            {
                return GetValidatorsForProperty(metadata);
            }

            return GetValidatorsForType(metadata);
        }

        protected abstract IEnumerable<IModelValidator> GetValidators(ModelMetadata metadata, 
                                                                      IEnumerable<Attribute> attributes);

        private IEnumerable<IModelValidator> GetValidatorsForProperty(ModelMetadata metadata)
        {
            var propertyName = metadata.PropertyName;
            var property = metadata.ContainerType
                                   .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                
            if (property == null)
            {
                throw new ArgumentException(
                    Resources.FormatCommon_PropertyNotFound(
                        metadata.ContainerType.FullName, 
                        metadata.PropertyName),
                    "metadata");
            }

            var attributes = property.GetCustomAttributes();
            return GetValidators(metadata, attributes);
        }

        private IEnumerable<IModelValidator> GetValidatorsForType(ModelMetadata metadata)
        {
            var attributes = metadata.ModelType
                                     .GetTypeInfo()
                                     .GetCustomAttributes();
            return GetValidators(metadata, attributes);
        }
    }
}
