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
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// This <see cref="ModelValidatorProvider"/> provides a required ModelValidator for members marked as [DataMember(IsRequired=true)].
    /// </summary>
    public class DataMemberModelValidatorProvider : AssociatedValidatorProvider
    {
        protected override IEnumerable<IModelValidator> GetValidators(ModelMetadata metadata,
                                                                      IEnumerable<Attribute> attributes)
        {
            // Types cannot be required; only properties can
            if (metadata.ContainerType == null || string.IsNullOrEmpty(metadata.PropertyName))
            {
                return Enumerable.Empty<IModelValidator>();
            }

            if (IsRequiredDataMember(metadata.ContainerType, attributes))
            {
                return new[] { new RequiredMemberModelValidator() };
            }

            return Enumerable.Empty<IModelValidator>();
        }

        internal static bool IsRequiredDataMember(Type containerType, IEnumerable<Attribute> attributes)
        {
            var dataMemberAttribute = attributes.OfType<DataMemberAttribute>()
                                                .FirstOrDefault();
            if (dataMemberAttribute != null)
            {
                // isDataContract == true iff the container type has at least one DataContractAttribute
                bool isDataContract = containerType.GetTypeInfo()
                                                   .GetCustomAttributes()
                                                   .OfType<DataContractAttribute>()
                                                   .Any();
                if (isDataContract && dataMemberAttribute.IsRequired)
                {
                    return true;
                }
            }
            return false;
        }
    }
}