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

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class ComplexModelDtoModelBinder : IModelBinder
    {
        public async Task<bool> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType == typeof(ComplexModelDto))
            {
                ModelBindingHelper.ValidateBindingContext(bindingContext, typeof(ComplexModelDto), allowNullModel: false);

                ComplexModelDto dto = (ComplexModelDto)bindingContext.Model;
                foreach (ModelMetadata propertyMetadata in dto.PropertyMetadata)
                {
                    ModelBindingContext propertyBindingContext = new ModelBindingContext(bindingContext)
                    {
                        ModelMetadata = propertyMetadata,
                        ModelName = ModelBindingHelper.CreatePropertyModelName(bindingContext.ModelName, propertyMetadata.PropertyName)
                    };

                    // bind and propagate the values
                    // If we can't bind, then leave the result missing (don't add a null).
                    if (await bindingContext.ModelBinder.BindModelAsync(propertyBindingContext))
                    {
                        dto.Results[propertyMetadata] = new ComplexModelDtoResult(propertyBindingContext.Model, propertyBindingContext.ValidationNode);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
