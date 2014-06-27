using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class BaseController : Controller
    {
        private IModelBindersProvider _modelBinders;
        private IModelMetadataProvider _modelMetadataProvider;
        private IEnumerable<IModelValidatorProvider> _validatorProviders;
        private IEnumerable<IValueProviderFactory> _valueProviderFactories;
        private CompositeModelBinder _modelBinder;
        private CompositeValueProvider _compositeValueProvider;
        private bool _modelBinderInitialized;
        private object _modelBinderInitLocker = new object();

        public BaseController()
        {

        }

        public void Initialize(
            IModelBindersProvider modelBinders,
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
            LazyInitializer.EnsureInitialized(ref _modelBinder, ref _modelBinderInitialized, ref _modelBinderInitLocker, () =>
            {
                var factoryContext = new ValueProviderFactoryContext(Context, ActionContext.RouteData.Values);
                _compositeValueProvider = new CompositeValueProvider(_valueProviderFactories.Select(vpf => vpf.GetValueProvider(factoryContext)));
                return new CompositeModelBinder(_modelBinders);
            });

            var bindingContext = new ModelBindingContext
            {
                MetadataProvider = _modelMetadataProvider,
                Model = model,
                ModelState = ModelState,
                ValidatorProviders = _validatorProviders,
                ModelBinder = _modelBinder,
                HttpContext = Context,
                ValueProvider = _compositeValueProvider
            };

            return _modelBinder.BindModelAsync(bindingContext);
        }
    }
}