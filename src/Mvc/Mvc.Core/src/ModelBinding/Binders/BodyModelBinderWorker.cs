// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    internal class BodyModelBinderWorker
    {
        private readonly IList<IInputFormatter> _formatters;
        private readonly Func<Stream, Encoding, TextReader> _readerFactory;
        private readonly ILogger _logger;
        private readonly MvcOptions _options;

        public BodyModelBinderWorker(
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory,
            ILogger logger,
            MvcOptions options)
        {
            _formatters = formatters;
            _readerFactory = readerFactory.CreateReader;
            _logger = logger;
            _options = options;
        }

        internal async Task ExecuteAsync(
            ModelBindingContext bindingContext,
            string modelBindingKey)
        {
            var allowEmptyInputInModelBinding = _options?.AllowEmptyInputInBodyModelBinding == true;
            var httpContext = bindingContext.HttpContext;

            var formatterContext = new InputFormatterContext(
                httpContext,
                modelBindingKey,
                bindingContext.ModelState,
                bindingContext.ModelMetadata,
                _readerFactory,
                allowEmptyInputInModelBinding);

            var formatter = (IInputFormatter)null;
            for (var i = 0; i < _formatters.Count; i++)
            {
                if (_formatters[i].CanRead(formatterContext))
                {
                    formatter = _formatters[i];
                    _logger?.InputFormatterSelected(formatter, formatterContext);
                    break;
                }
                else
                {
                    _logger?.InputFormatterRejected(_formatters[i], formatterContext);
                }
            }

            if (formatter == null)
            {
                _logger?.NoInputFormatterSelected(formatterContext);

                var message = Resources.FormatUnsupportedContentType(httpContext.Request.ContentType);
                var exception = new UnsupportedContentTypeException(message);
                bindingContext.ModelState.AddModelError(modelBindingKey, exception, bindingContext.ModelMetadata);
                _logger?.DoneAttemptingToBindModel(bindingContext);
                return;
            }

            try
            {
                var result = await formatter.ReadAsync(formatterContext);

                if (result.HasError)
                {
                    // Formatter encountered an error. Do not use the model it returned.
                    _logger?.DoneAttemptingToBindModel(bindingContext);
                    return;
                }

                if (result.IsModelSet)
                {
                    var model = result.Model;
                    bindingContext.Result = ModelBindingResult.Success(model);
                }
                else
                {
                    // If the input formatter gives a "no value" result, that's always a model state error,
                    // because BodyModelBinder implicitly regards input as being required for model binding.
                    // If instead the input formatter wants to treat the input as optional, it must do so by
                    // returning InputFormatterResult.Success(defaultForModelType), because input formatters
                    // are responsible for choosing a default value for the model type.
                    var message = bindingContext
                        .ModelMetadata
                        .ModelBindingMessageProvider
                        .MissingRequestBodyRequiredValueAccessor();
                    bindingContext.ModelState.AddModelError(modelBindingKey, message);
                }
            }
            catch (Exception exception) when (exception is InputFormatterException || ShouldHandleException(formatter))
            {
                bindingContext.ModelState.AddModelError(modelBindingKey, exception, bindingContext.ModelMetadata);
            }

            _logger?.DoneAttemptingToBindModel(bindingContext);
        }

        private static bool ShouldHandleException(IInputFormatter formatter)
        {
            // Any explicit policy on the formatters overrides the default.
            var policy = (formatter as IInputFormatterExceptionPolicy)?.ExceptionPolicy ??
                InputFormatterExceptionPolicy.MalformedInputExceptions;

            return policy == InputFormatterExceptionPolicy.AllExceptions;
        }
    }
}
