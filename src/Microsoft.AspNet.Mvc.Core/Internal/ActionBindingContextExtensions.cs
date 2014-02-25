using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Internal
{
    public static class ActionBindingContextExtensions
    {
        public static InputFormatterContext CreateInputFormatterContext(this ActionBindingContext actionBindingContext,
                                                                        ModelStateDictionary modelState,
                                                                        ParameterDescriptor parameter)
        {
            var metadataProvider = actionBindingContext.MetadataProvider;
            var parameterType = parameter.BodyParameterInfo.ParameterType;
            var modelMetadata = metadataProvider.GetMetadataForType(modelAccessor: null, modelType: parameterType);
            return new InputFormatterContext(modelMetadata, modelState);
        }

        public static ModelBindingContext CreateModelBindingContext(this ActionBindingContext actionBindingContext,
                                                                    ModelStateDictionary modelState,
                                                                    ParameterDescriptor parameter)
        {
            var metadataProvider = actionBindingContext.MetadataProvider;
            var parameterType = parameter.ParameterBindingInfo.ParameterType;
            var modelMetadata = metadataProvider.GetMetadataForType(modelAccessor: null, modelType: parameterType);

            return new ModelBindingContext
            {
                ModelName = parameter.Name,
                ModelState = modelState,
                ModelMetadata = modelMetadata,
                ModelBinder = actionBindingContext.ModelBinder,
                ValueProvider = actionBindingContext.ValueProvider,
                MetadataProvider = metadataProvider,
                HttpContext = actionBindingContext.ActionContext.HttpContext,
                FallbackToEmptyPrefix = true
            };
        }
    }
}
