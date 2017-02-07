// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class DefaultPageArgumentBinder : PageArgumentBinder
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IObjectModelValidator _validator;

        public DefaultPageArgumentBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IObjectModelValidator validator)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _modelBinderFactory = modelBinderFactory;
            _validator = validator;
        }

        protected override async Task<ModelBindingResult> BindAsync(PageContext pageContext, object value, string name, Type type)
        {
            var factories = pageContext.ValueProviderFactories;
            var valueProviderFactoryContext = new ValueProviderFactoryContext(pageContext);
            for (var i = 0; i < factories.Count; i++)
            {
                var factory = factories[i];
                await factory.CreateValueProviderAsync(valueProviderFactoryContext);
            }

            var valueProvider = new CompositeValueProvider(valueProviderFactoryContext.ValueProviders);

            var metadata = _modelMetadataProvider.GetMetadataForType(type);
            var binder = _modelBinderFactory.CreateBinder(new ModelBinderFactoryContext()
            {
                BindingInfo = null,
                Metadata = metadata,
                CacheToken = null,
            });

            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                pageContext,
                valueProvider,
                metadata,
                null,
                name);
            modelBindingContext.Model = value;

            if (modelBindingContext.ValueProvider.ContainsPrefix(name))
            {
                // We have a match for the parameter name, use that as that prefix.
                modelBindingContext.ModelName = name;
            }
            else
            {
                // No match, fallback to empty string as the prefix.
                modelBindingContext.ModelName = string.Empty;
            }

            await binder.BindModelAsync(modelBindingContext);

            var result = modelBindingContext.Result;
            if (result.IsModelSet)
            {
                _validator.Validate(
                    pageContext,
                    modelBindingContext.ValidationState,
                    modelBindingContext.ModelName,
                    result.Model);
            }

            return result;
        }
    }
}
