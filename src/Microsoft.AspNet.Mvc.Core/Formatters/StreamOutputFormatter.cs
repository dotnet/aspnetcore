// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Always copies the stream to the response, regardless of requested content type.
    /// </summary>
    public class StreamOutputFormatter : IOutputFormatter
    {
        /// <summary>
        /// Echos the <paramref name="contentType"/> if the <paramref name="runtimeType"/> implements 
        /// <see cref="Stream"/> and <paramref name="contentType"/> is not <c>null</c>.
        /// </summary>
        /// <param name="declaredType">The declared type for which the supported content types are desired.</param>
        /// <param name="runtimeType">The runtime type for which the supported content types are desired.</param>
        /// <param name="contentType">
        /// The content type for which the supported content types are desired, or <c>null</c> if any content
        /// type can be used.
        /// </param>
        /// <returns>Content types which are supported by this formatter.</returns>
        public IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(
            Type declaredType,
            Type runtimeType,
            MediaTypeHeaderValue contentType)
        {
            if (contentType != null &&
                runtimeType != null &&
                typeof(Stream).IsAssignableFrom(runtimeType))
            {
                return new[] { contentType };
            }

            return null;
        }

        /// <inheritdoc />
        public bool CanWriteResult([NotNull] OutputFormatterContext context, MediaTypeHeaderValue contentType)
        {
            // Ignore the passed in content type, if the object is a Stream.
            if (context.Object is Stream)
            {
                context.SelectedContentType = contentType;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public async Task WriteAsync([NotNull] OutputFormatterContext context)
        {
            using (var valueAsStream = ((Stream)context.Object))
            {
                var response = context.ActionContext.HttpContext.Response;

                if (context.SelectedContentType != null)
                {
                    response.ContentType = context.SelectedContentType.ToString();
                }

                await valueAsStream.CopyToAsync(response.Body);
            }
        }
    }
}
