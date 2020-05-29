// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for deserializing the request body using a formatter.
    /// </summary>
    public class MultipartSectionModelBinderProvider : IModelBinderProvider
    {
        private readonly IList<IInputFormatter> _formatters;
        private readonly IHttpRequestStreamReaderFactory _readerFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MvcOptions _options;

        /// <summary>
        /// Creates a new <see cref="MultipartSectionModelBinderProvider"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public MultipartSectionModelBinderProvider(
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

            _formatters = formatters;
            _readerFactory = readerFactory;
            _loggerFactory = loggerFactory;
            _options = options;
        }

        /// <inheritdoc />
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.BindingInfo.BindingSource != null &&
                context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.MultipartSection))
            {
                if (_formatters.Count == 0)
                {
                    throw new InvalidOperationException(Resources.FormatInputFormattersAreRequired(
                        typeof(MvcOptions).FullName,
                        nameof(MvcOptions.InputFormatters),
                        typeof(IInputFormatter).FullName));
                }

                return new MultipartSectionModelBinder(_formatters, _readerFactory, _loggerFactory.CreateLogger<MultipartSectionModelBinder>(), _options);
            }

            return null;
        }
    }
}
