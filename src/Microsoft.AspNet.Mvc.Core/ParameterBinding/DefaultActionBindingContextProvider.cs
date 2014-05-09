// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionBindingContextProvider : IActionBindingContextProvider
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IEnumerable<IModelBinder> _modelBinders;
        private readonly IEnumerable<IValueProviderFactory> _valueProviderFactories;
        private readonly IInputFormatterProvider _inputFormatterProvider;
        private readonly IEnumerable<IModelValidatorProvider> _validatorProviders;

        public DefaultActionBindingContextProvider(IModelMetadataProvider modelMetadataProvider,
                                                   IEnumerable<IModelBinder> modelBinders,
                                                   IEnumerable<IValueProviderFactory> valueProviderFactories,
                                                   IInputFormatterProvider inputFormatterProvider,
                                                   IEnumerable<IModelValidatorProvider> validatorProviders)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _modelBinders = modelBinders.OrderBy(binder => binder.GetType() == typeof(ComplexModelDtoModelBinder) ? 1 : 0);
            _valueProviderFactories = valueProviderFactories;
            _inputFormatterProvider = inputFormatterProvider;
            _validatorProviders = validatorProviders;
        }

        public async Task<ActionBindingContext> GetActionBindingContextAsync(ActionContext actionContext)
        {
            var requestContext = new RequestContext(actionContext.HttpContext, actionContext.RouteValues);
            var valueProviders = await Task.WhenAll(_valueProviderFactories.Select(factory => factory.GetValueProviderAsync(requestContext)));
            valueProviders = valueProviders.Where(vp => vp != null)
                                            .ToArray();

            return new ActionBindingContext(
                actionContext,
                _modelMetadataProvider,
                new CompositeModelBinder(_modelBinders),
                new CompositeValueProvider(valueProviders),
                _inputFormatterProvider,
                _validatorProviders
            );
        }
    }
}
