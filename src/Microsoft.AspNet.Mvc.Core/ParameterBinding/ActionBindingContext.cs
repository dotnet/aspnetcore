using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class ActionBindingContext
    {
        public ActionBindingContext(ActionContext context,
                                    IModelMetadataProvider metadataProvider,
                                    IModelBinder modelBinder,
                                    IValueProvider valueProvider,
                                    IInputFormatter inputFormatter)
        {
            ActionContext = context;
            MetadataProvider = metadataProvider;
            ModelBinder = modelBinder;
            ValueProvider = valueProvider;
            InputFormatter = inputFormatter;
        }

        public ActionContext ActionContext { get; private set; }

        public IModelMetadataProvider MetadataProvider { get; private set; }

        public IModelBinder ModelBinder { get; private set; }

        public IValueProvider ValueProvider { get; private set; }

        public IInputFormatter InputFormatter { get; private set; }
    }
}
