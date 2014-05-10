using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class BaseController : Controller
    {
        private IEnumerable<IModelBinder> _modelBinders;
        private IModelMetadataProvider _modelMetadataProvider;
        private IEnumerable<IModelValidatorProvider> _validatorProviders;
        private IEnumerable<IValueProviderFactory> _valueProviderFactories;

        public BaseController()
        {

        }

        public void Initialize(
            IEnumerable<IModelBinder> modelBinders,
            IModelMetadataProvider modelMetadataProvider,
            IEnumerable<IModelValidatorProvider> validatorProviders,
            IEnumerable<IValueProviderFactory> valueProviderFactories)
        {
            _modelBinders = modelBinders;
            _modelMetadataProvider = modelMetadataProvider;
            _validatorProviders = validatorProviders;
            _valueProviderFactories = valueProviderFactories;
        }

        protected Task<bool> TryUpdateModelAsync<TModel>(TModel model)
        {
            var binder = new CompositeModelBinder(_modelBinders);
            var requestContext = new RequestContext(Context, ActionContext.RouteValues);
            var bindingContext = new ModelBindingContext
            {
                MetadataProvider = _modelMetadataProvider,
                Model = model,
                ModelState = ModelState,
                ValidatorProviders = _validatorProviders,
                ModelBinder = binder,
                HttpContext = Context,
                ValueProvider = new CompositeValueProvider(_valueProviderFactories.Select(
                    vpf => vpf.GetValueProviderAsync(requestContext).Result))
            };

            return binder.BindModelAsync(bindingContext);
        }
    }
}