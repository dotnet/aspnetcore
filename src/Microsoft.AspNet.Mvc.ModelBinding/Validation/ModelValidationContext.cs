using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelValidationContext
    {
        public ModelValidationContext([NotNull] ModelMetadata metadata,
                                      [NotNull] ModelStateDictionary modelState,
                                      [NotNull] IModelMetadataProvider metadataProvider,
                                      [NotNull] IEnumerable<IModelValidatorProvider> validatorProviders)
        {
            ModelMetadata = metadata;
            ModelState = modelState;
            MetadataProvider = metadataProvider;
            ValidatorProviders = validatorProviders;
        }

        public ModelValidationContext([NotNull] ModelValidationContext parentContext, 
                                      [NotNull] ModelMetadata metadata)
        {
            ModelMetadata = metadata;
            ContainerMetadata = parentContext.ModelMetadata;
            ModelState = parentContext.ModelState;
            MetadataProvider = parentContext.MetadataProvider;
            ValidatorProviders = parentContext.ValidatorProviders;
        }

        public ModelMetadata ModelMetadata { get; private set; }

        public ModelMetadata ContainerMetadata { get; private set; }

        public ModelStateDictionary ModelState { get; private set; }

        public IModelMetadataProvider MetadataProvider { get; private set; }

        public IEnumerable<IModelValidatorProvider> ValidatorProviders { get; private set; }
    }
}
