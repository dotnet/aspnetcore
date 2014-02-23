using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class TypeMatchModelBinder : IModelBinder
    {
        public bool BindModel(ModelBindingContext bindingContext)
        {
            ValueProviderResult valueProviderResult = GetCompatibleValueProviderResult(bindingContext);
            if (valueProviderResult == null)
            {
                // conversion would have failed
                return false;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
            object model = valueProviderResult.RawValue;
            ModelBindingHelper.ReplaceEmptyStringWithNull(bindingContext.ModelMetadata, ref model);
            bindingContext.Model = model;
            if (bindingContext.ModelMetadata.IsComplexType)
            {
                // TODO: Validation
                //IBodyModelValidator validator = services.GetBodyModelValidator();
                //ModelMetadataProvider metadataProvider = services.GetModelMetadataProvider();
                //if (validator != null && metadataProvider != null)
                //{
                //    validator.Validate(model, bindingContext.ModelType, metadataProvider, context, bindingContext.ModelName);
                //}
            }

            return true;
        }

        internal static ValueProviderResult GetCompatibleValueProviderResult(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext);

            ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == null)
            {
                return null; // the value doesn't exist
            }

            if (!bindingContext.ModelType.IsCompatibleWith(valueProviderResult.RawValue))
            {
                return null; // value is of incompatible type
            }

            return valueProviderResult;
        }
    }
}
