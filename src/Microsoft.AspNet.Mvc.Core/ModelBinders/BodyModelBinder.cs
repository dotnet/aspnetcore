// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request body using an <see cref="IInputFormatter"/>
    /// when a model has the binding source <see cref="BindingSource.Body"/>/
    /// </summary>
    public class BodyModelBinder : BindingSourceModelBinder
    {
        /// <summary>
        /// Creates a new <see cref="BodyModelBinder"/>.
        /// </summary>
        public BodyModelBinder()
            : base(BindingSource.Body)
        {
        }

        /// <inheritdoc />
        protected async override Task<ModelBindingResult> BindModelCoreAsync([NotNull] ModelBindingContext bindingContext)
        {
            var requestServices = bindingContext.OperationBindingContext.HttpContext.RequestServices;

            var formatterSelector = requestServices.GetRequiredService<IInputFormatterSelector>();
            var actionContext = requestServices.GetRequiredService<IScopedInstance<ActionContext>>().Value;
            var formatters = requestServices.GetRequiredService<IScopedInstance<ActionBindingContext>>().Value.InputFormatters;

            var formatterContext = new InputFormatterContext(actionContext, bindingContext.ModelType);
            var formatter = formatterSelector.SelectFormatter(formatters.ToList(), formatterContext);

            if (formatter == null)
            {
                var unsupportedContentType = Resources.FormatUnsupportedContentType(
                    bindingContext.OperationBindingContext.HttpContext.Request.ContentType);
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, unsupportedContentType);

                // This model binder is the only handler for the Body binding source.
                // Always tell the model binding system to skip other model binders i.e. return non-null.
                return new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);
            }

            object model = null;
            try
            {
                model = await formatter.ReadAsync(formatterContext);
            }
            catch (Exception ex)
            {
                model = GetDefaultValueForType(bindingContext.ModelType);
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, ex);

                // This model binder is the only handler for the Body binding source.
                // Always tell the model binding system to skip other model binders i.e. return non-null.
                return new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);
            }

            // Success
            // key is empty to ensure that the model name is not used as a prefix for validation.
            return new ModelBindingResult(model, key: string.Empty, isModelSet: true);
        }

        private object GetDefaultValueForType(Type modelType)
        {
            if (modelType.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(modelType);
            }

            return null;
        }
    }
}
