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
        protected async override Task<ModelBindingResult> BindModelCoreAsync(
            [NotNull] ModelBindingContext bindingContext)
        {
            // For compatibility with MVC 5.0 for top level object we want to consider an empty key instead of
            // the parameter name/a custom name. In all other cases (like when binding body to a property) we
            // consider the entire ModelName as a prefix.
            var modelBindingKey = bindingContext.IsTopLevelObject ? string.Empty : bindingContext.ModelName;

            var httpContext = bindingContext.OperationBindingContext.HttpContext;

            var formatterContext = new InputFormatterContext(
                httpContext,
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
                var model = await formatter.ReadAsync(formatterContext);
                
                bindingContext.ModelState.SetModelValue(modelBindingKey, rawValue: model, attemptedValue: null);

                if (bindingContext.ModelState.ErrorCount != previousCount)
                {
                    // Formatter added an error. Do not use the model it returned. As above, tell the model binding
                    // system to skip other model binders and never to fall back.
                    return ModelBindingResult.Failed(modelBindingKey);
                }

                var validationNode = new ModelValidationNode(modelBindingKey, bindingContext.ModelMetadata, model)
                {
                    ValidateAllProperties = true
                };

                return ModelBindingResult.Success(modelBindingKey, model, validationNode);
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
