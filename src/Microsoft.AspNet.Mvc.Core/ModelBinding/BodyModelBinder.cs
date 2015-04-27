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

            var actionContext = requestServices.GetRequiredService<IScopedInstance<ActionContext>>().Value;
            var formatters = requestServices.GetRequiredService<IScopedInstance<ActionBindingContext>>().Value.InputFormatters;

            var formatterContext = new InputFormatterContext(actionContext, bindingContext.ModelType);
            var formatter = formatters.FirstOrDefault(f => f.CanRead(formatterContext));

            if (formatter == null)
            {
                var unsupportedContentType = Resources.FormatUnsupportedContentType(
                    bindingContext.OperationBindingContext.HttpContext.Request.ContentType);
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, unsupportedContentType);

                // This model binder is the only handler for the Body binding source.
                // Always tell the model binding system to skip other model binders i.e. return non-null.
                return new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);
            }

            try
            {
                var model = await formatter.ReadAsync(formatterContext);

                var isTopLevelObject = bindingContext.ModelMetadata.ContainerType == null;

                // For compatibility with MVC 5.0 for top level object we want to consider an empty key instead of 
                // the parameter name/a custom name. In all other cases (like when binding body to a property) we
                // consider the entire ModelName as a prefix.
                var modelBindingKey = isTopLevelObject ? string.Empty : bindingContext.ModelName;
                return new ModelBindingResult(model, key: modelBindingKey, isModelSet: true);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, ex);

                // This model binder is the only handler for the Body binding source.
                // Always tell the model binding system to skip other model binders i.e. return non-null.
                return new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);
            }
        }
    }
}
