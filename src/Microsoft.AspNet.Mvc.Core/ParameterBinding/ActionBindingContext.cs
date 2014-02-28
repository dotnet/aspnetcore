using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class ActionBindingContext
    {
        public ActionBindingContext(ActionContext context,
                                    IModelMetadataProvider metadataProvider,
                                    IModelBinder modelBinder,
                                    IValueProvider valueProvider,
                                    IInputFormatter inputFormatter,
                                    IEnumerable<IModelValidatorProvider> validatorProviders)
        {
            ActionContext = context;
            MetadataProvider = metadataProvider;
            ModelBinder = modelBinder;
            ValueProvider = valueProvider;
            InputFormatter = inputFormatter;
            ValidatorProviders = validatorProviders;
        }

        public ActionContext ActionContext { get; private set; }

        public IModelMetadataProvider MetadataProvider { get; private set; }

        public IModelBinder ModelBinder { get; private set; }

        public IValueProvider ValueProvider { get; private set; }

        public IInputFormatter InputFormatter { get; private set; }

        public IEnumerable<IModelValidatorProvider> ValidatorProviders { get; private set; }
    }
}
