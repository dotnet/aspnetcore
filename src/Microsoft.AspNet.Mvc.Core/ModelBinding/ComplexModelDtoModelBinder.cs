// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class ComplexModelDtoModelBinder : IModelBinder
    {
        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(ComplexModelDto))
            {
                return null;
            }

            ModelBindingHelper.ValidateBindingContext(bindingContext, typeof(ComplexModelDto), allowNullModel: false);

            var dto = (ComplexModelDto)bindingContext.Model;
            foreach (var propertyMetadata in dto.PropertyMetadata)
            {
                var propertyModelName = ModelNames.CreatePropertyModelName(
                    bindingContext.ModelName,
                    propertyMetadata.BinderModelName ?? propertyMetadata.PropertyName);

                var propertyBindingContext = ModelBindingContext.GetChildModelBindingContext(
                    bindingContext,
                    propertyModelName,
                    propertyMetadata);

                var modelBindingResult =
                    await bindingContext.OperationBindingContext.ModelBinder.BindModelAsync(propertyBindingContext);
                if (modelBindingResult == null)
                {
                    // Could not bind. Let MutableObjectModelBinder know explicitly.
                    dto.Results[propertyMetadata] =
                        new ModelBindingResult(model: null, key: propertyModelName, isModelSet: false);
                }
                else
                {
                    dto.Results[propertyMetadata] = modelBindingResult;
                }
            }

            return new ModelBindingResult(dto, bindingContext.ModelName, isModelSet: true);
        }
    }
}
