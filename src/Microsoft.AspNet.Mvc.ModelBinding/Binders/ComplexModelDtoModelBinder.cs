using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class ComplexModelDtoModelBinder : IModelBinder
    {
        public bool BindModel(ModelBindingContext bindingContext)
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

                    if (bindingContext.ModelBinder.BindModel(propertyBindingContext))
                    {
                        dto.Results[propertyMetadata] = new ComplexModelDtoResult(propertyBindingContext.Model/*, propertyBindingContext.ValidationNode*/);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
