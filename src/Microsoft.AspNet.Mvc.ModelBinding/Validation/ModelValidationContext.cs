using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelValidationContext
    {
        public ModelValidationContext([NotNull] ModelBindingContext bindingContext,
                                      [NotNull] ModelMetadata metadata)
            : this(bindingContext.MetadataProvider, 
                   bindingContext.ValidatorProviders, 
                   bindingContext.ModelState, 
                   metadata, 
                   bindingContext.ModelMetadata)
        {
        }

        public ModelValidationContext([NotNull] IModelMetadataProvider metadataProvider, 
                                      [NotNull] IEnumerable<IModelValidatorProvider> validatorProviders, 
                                      [NotNull] ModelStateDictionary modelState, 
                                      [NotNull] ModelMetadata metadata, 
                                      ModelMetadata containerMetadata)
        {
            ModelMetadata = metadata;
            ModelState = modelState;
            MetadataProvider = metadataProvider;
            ValidatorProviders = validatorProviders;
            ContainerMetadata = containerMetadata;
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
