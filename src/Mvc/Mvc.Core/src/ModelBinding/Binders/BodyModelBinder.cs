// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which binds models from the request body using an <see cref="IInputFormatter"/>
    /// when a model has the binding source <see cref="BindingSource.Body"/>.
    /// </summary>
    public class BodyModelBinder : IModelBinder
    {
        private readonly ILogger _logger;
        private readonly BodyModelBinderWorker _worker;

        /// <summary>
        /// Creates a new <see cref="BodyModelBinder"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">
        /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
        /// instances for reading the request body.
        /// </param>
        public BodyModelBinder(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory)
            : this(formatters, readerFactory, loggerFactory: NullLoggerFactory.Instance)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BodyModelBinder"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">
        /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
        /// instances for reading the request body.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public BodyModelBinder(
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory,
            ILoggerFactory loggerFactory)
            : this(formatters, readerFactory, loggerFactory, options: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BodyModelBinder"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">
        /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
        /// instances for reading the request body.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public BodyModelBinder(
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory,
            ILoggerFactory loggerFactory,
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

            _logger = loggerFactory.CreateLogger<BodyModelBinder>();
            _worker = new BodyModelBinderWorker(formatters, readerFactory, _logger, options);
        }

        /// <inheritdoc />
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            _logger.AttemptingToBindModel(bindingContext);

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

            await _worker.ExecuteAsync(bindingContext, modelBindingKey);
        }
    }
}
