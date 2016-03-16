// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for deserializing the request body using a formatter.
    /// </summary>
    public class BodyModelBinderProvider : IModelBinderProvider
    {
        private readonly IHttpRequestStreamReaderFactory _readerFactory;

        /// <summary>
        /// Creates a new <see cref="BodyModelBinderProvider"/>.
        /// </summary>
        /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
        public BodyModelBinderProvider(IHttpRequestStreamReaderFactory readerFactory)
        {
            if (readerFactory == null)
            {
                throw new ArgumentNullException(nameof(readerFactory));
            }

            _readerFactory = readerFactory;
        }

        /// <inheritdoc />
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.BindingInfo?.BindingSource != null &&
                context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Body))
            {
                return new BodyModelBinder(_readerFactory);
            }

            return null;
        }
    }
}
