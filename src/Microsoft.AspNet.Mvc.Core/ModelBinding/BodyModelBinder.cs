// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request body using an <see cref="IInputFormatter"/>
    /// when a model has the binding source <see cref="BindingSource.Body"/>/
    /// </summary>
    public class BodyModelBinder : IModelBinder
    {
        /// <inheritdoc />
        public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            // This method is optimized to use cached tasks when possible and avoid allocating
            // using Task.FromResult. If you need to make changes of this nature, profile
            // allocations afterwards and look for Task<ModelBindingResult>.

            var allowedBindingSource = bindingContext.BindingSource;
            if (allowedBindingSource == null ||
                !allowedBindingSource.CanAcceptDataFrom(BindingSource.Body))
            {
                // Formatters are opt-in. This model either didn't specify [FromBody] or specified something
                // incompatible so let other binders run.
                return ModelBindingResult.NoResultAsync;
            }

            return BindModelCoreAsync(bindingContext);
        }

        /// <summary>
        /// Attempts to bind the model using formatters.
        /// </summary>
        /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
        /// <returns>
        /// A <see cref="Task{ModelBindingResult}"/> which when completed returns a <see cref="ModelBindingResult"/>.
        /// </returns>
        private async Task<ModelBindingResult> BindModelCoreAsync([NotNull] ModelBindingContext bindingContext)
        {
            // For compatibility with MVC 5.0 for top level object we want to consider an empty key instead of
            // the parameter name/a custom name. In all other cases (like when binding body to a property) we
            // consider the entire ModelName as a prefix.
            var modelBindingKey = bindingContext.IsTopLevelObject ? string.Empty : bindingContext.ModelName;

            var httpContext = bindingContext.OperationBindingContext.HttpContext;

            var formatterContext = new InputFormatterContext(
                httpContext,
                modelBindingKey,
                bindingContext.ModelState,
                bindingContext.ModelType);
            var formatters = bindingContext.OperationBindingContext.InputFormatters;
            var formatter = formatters.FirstOrDefault(f => f.CanRead(formatterContext));

            if (formatter == null)
            {
                var unsupportedContentType = Resources.FormatUnsupportedContentType(
                    bindingContext.OperationBindingContext.HttpContext.Request.ContentType);
                bindingContext.ModelState.AddModelError(modelBindingKey, unsupportedContentType);

                // This model binder is the only handler for the Body binding source and it cannot run twice. Always
                // tell the model binding system to skip other model binders and never to fall back i.e. indicate a
                // fatal error.
                return ModelBindingResult.Failed(modelBindingKey);
            }

            try
            {
                var previousCount = bindingContext.ModelState.ErrorCount;
                var result = await formatter.ReadAsync(formatterContext);
                var model = result.Model;

                // Ensure a "modelBindingKey" entry exists whether or not formatting was successful.
                bindingContext.ModelState.SetModelValue(modelBindingKey, rawValue: model, attemptedValue: null);

                if (result.HasError)
                {
                    // Formatter encountered an error. Do not use the model it returned. As above, tell the model
                    // binding system to skip other model binders and never to fall back.
                    return ModelBindingResult.Failed(modelBindingKey);
                }

                return ModelBindingResult.Success(modelBindingKey, model);
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.AddModelError(modelBindingKey, ex);

                // This model binder is the only handler for the Body binding source and it cannot run twice. Always
                // tell the model binding system to skip other model binders and never to fall back i.e. indicate a
                // fatal error.
                return ModelBindingResult.Failed(modelBindingKey);
            }
        }
    }
}
