// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http
{
    public static class RequestFormReaderExtensions
    {
        /// <summary>
        /// Read the request body as a form with the given options. These options will only be used
        /// if the form has not already been read.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="options">Options for reading the form.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The parsed form.</returns>
        public static Task<IFormCollection> ReadFormAsync(this HttpRequest request, FormOptions options,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!request.HasFormContentType)
            {
                throw new InvalidOperationException("Incorrect Content-Type: " + request.ContentType);
            }

            var features = request.HttpContext.Features;
            var formFeature = features.Get<IFormFeature>();
            if (formFeature == null || formFeature.Form == null)
            {
                // We haven't read the form yet, replace the reader with one using our own options.
                features.Set<IFormFeature>(new FormFeature(request, options));
            }
            return request.ReadFormAsync(cancellationToken);
        }
    }
}
