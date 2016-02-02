// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request body using an <see cref="IInputFormatter"/>
    /// when a model has the binding source <see cref="BindingSource.Body"/>/
    /// </summary>
    public class BodyModelBinder : IModelBinder
    {
        private readonly Func<Stream, Encoding, TextReader> _readerFactory;

        /// <summary>
        /// Creates a new <see cref="BodyModelBinder"/>.
        /// </summary>
        /// <param name="readerFactory">
        /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
        /// instances for reading the request body.
        /// </param>
        public BodyModelBinder(IHttpRequestStreamReaderFactory readerFactory)
        {
            _readerFactory = readerFactory.CreateReader;
        }

        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // This method is optimized to use cached tasks when possible and avoid allocating
            // using Task.FromResult. If you need to make changes of this nature, profile
            // allocations afterwards and look for Task<ModelBindingResult>.

            var allowedBindingSource = bindingContext.BindingSource;
            if (allowedBindingSource == null ||
                !allowedBindingSource.CanAcceptDataFrom(BindingSource.Body))
            {
                // Formatters are opt-in. This model either didn't specify [FromBody] or specified something
                // incompatible so let other binders run.
                return TaskCache.CompletedTask;
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
        private async Task BindModelCoreAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // Special logic for body, treat the model name as string.Empty for the top level
            // object, but allow an override via BinderModelName. The purpose of this is to try
            // and be similar to the behavior for POCOs bound via traditional model binding.
            string modelBindingKey;
            if (bindingContext.IsTopLevelObject)
            {
                modelBindingKey = bindingContext.BinderModelName ?? string.Empty;
            }
            else
            {
                modelBindingKey = bindingContext.ModelName;
            }

            var httpContext = bindingContext.OperationBindingContext.HttpContext;

            var formatterContext = new InputFormatterContext(
                httpContext,
                modelBindingKey,
                bindingContext.ModelState,
                bindingContext.ModelMetadata,
                _readerFactory);

            var formatters = bindingContext.OperationBindingContext.InputFormatters;
            var formatter = formatters.FirstOrDefault(f => f.CanRead(formatterContext));

            if (formatter == null)
            {
                var message = Resources.FormatUnsupportedContentType(
                    bindingContext.OperationBindingContext.HttpContext.Request.ContentType);

                var exception = new UnsupportedContentTypeException(message);
                bindingContext.ModelState.AddModelError(modelBindingKey, exception, bindingContext.ModelMetadata);

                // This model binder is the only handler for the Body binding source and it cannot run twice. Always
                // tell the model binding system to skip other model binders and never to fall back i.e. indicate a
                // fatal error.
                bindingContext.Result = ModelBindingResult.Failed(modelBindingKey);
                return;
            }

            try
            {
                var previousCount = bindingContext.ModelState.ErrorCount;
                var result = await formatter.ReadAsync(formatterContext);
                var model = result.Model;

                if (result.HasError)
                {
                    // Formatter encountered an error. Do not use the model it returned. As above, tell the model
                    // binding system to skip other model binders and never to fall back.
                    bindingContext.Result = ModelBindingResult.Failed(modelBindingKey);
                    return;
                }

                bindingContext.Result = ModelBindingResult.Success(modelBindingKey, model);
                return;
            }
            catch (Exception ex)
            {
                bindingContext.ModelState.AddModelError(modelBindingKey, ex, bindingContext.ModelMetadata);

                // This model binder is the only handler for the Body binding source and it cannot run twice. Always
                // tell the model binding system to skip other model binders and never to fall back i.e. indicate a
                // fatal error.
                bindingContext.Result = ModelBindingResult.Failed(modelBindingKey);
                return;
            }
        }
    }
}
