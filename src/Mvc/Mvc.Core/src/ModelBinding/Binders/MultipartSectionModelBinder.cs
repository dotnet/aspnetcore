// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request body using an <see cref="IInputFormatter"/>
    /// when a model has the binding source <see cref="BindingSource.Body"/>.
    /// </summary>
    public sealed class MultipartSectionModelBinder : IModelBinder
    {
        private readonly BodyModelBinderWorker _worker;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="BodyModelBinder"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">
        /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
        /// instances for reading the request body.
        /// </param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public MultipartSectionModelBinder(
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory,
            ILogger<MultipartSectionModelBinder> logger,
            MvcOptions options)
        {
            if (formatters == null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            if (readerFactory == null)
            {
                throw new ArgumentNullException(nameof(readerFactory));
            }

            _worker = new BodyModelBinderWorker(formatters, readerFactory, logger, options);
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var request = bindingContext.HttpContext.Request;
            var requestContentType = request.ContentType;
            if (!MediaTypeHeaderValue.TryParse(requestContentType, out var mediaType) || !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                // log.debug("not multipart/form-data")
                return;
            }

            var modelBindingKey = bindingContext.BinderModelName ?? bindingContext.ModelName;
            var form = await request.ReadFormAsync();
            if (!form.TryGetValue(modelBindingKey, out var values) || values.Count == 0)
            {
                // log.debug("no value found for '{modelBindingKey}'
                return;
            }


            if (!form.TryGetContentType(modelBindingKey, out var multipartContentTypeString) ||
                multipartContentTypeString.Count == 0 ||
                !MediaTypeHeaderValue.TryParse(multipartContentTypeString[0], out var multipartContentType))
            {
                // log.debug("no content-type found for '{modelBindingKey}'
                return;
            }

            _logger?.AttemptingToBindModel(bindingContext);

            var originalBody = request.Body;
            var value = values[0];

            try
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                request.Body = new MemoryStream(bytes);
                request.ContentType = multipartContentType.MediaType.Value;

                await _worker.ExecuteAsync(bindingContext, modelBindingKey);
            }
            finally
            {
                request.Body = originalBody;
                request.ContentType = requestContentType;
            }
        }
    }
}
