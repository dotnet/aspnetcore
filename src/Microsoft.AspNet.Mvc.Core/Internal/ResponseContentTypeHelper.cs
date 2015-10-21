// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Text;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.Internal
{
    public static class ResponseContentTypeHelper
    {
        /// <summary>
        /// Gets the content type and encoding that need to be used for the response.
        /// The priority for selecting the content type is:
        /// 1. ContentType property set on the action result
        /// 2. <see cref="HttpResponse.ContentType"/> property set on <see cref="HttpResponse"/>
        /// 3. Default content type set on the action result
        /// </summary>
        /// <remarks>
        /// The user supplied content type is not modified and is used as is. For example, if user
        /// sets the content type to be "text/plain" without any encoding, then the default content type's
        /// encoding is used to write the response and the ContentType header is set to be "text/plain" without any
        /// "charset" information.
        /// </remarks>
        /// <param name="actionResultContentType">ContentType set on the action result</param>
        /// <param name="httpResponseContentType"><see cref="HttpResponse.ContentType"/> property set
        /// on <see cref="HttpResponse"/></param>
        /// <param name="defaultContentType">The default content type of the action result.</param>
        /// <param name="resolvedContentType">The content type to be used for the response content type header</param>
        /// <param name="resolvedContentTypeEncoding">Encoding to be used for writing the response</param>
        public static void ResolveContentTypeAndEncoding(
            MediaTypeHeaderValue actionResultContentType,
            string httpResponseContentType,
            MediaTypeHeaderValue defaultContentType,
            out string resolvedContentType,
            out Encoding resolvedContentTypeEncoding)
        {
            Debug.Assert(defaultContentType != null);
            Debug.Assert(defaultContentType.Encoding != null);

            // 1. User sets the ContentType property on the action result
            if (actionResultContentType != null)
            {
                resolvedContentType = actionResultContentType.ToString();
                resolvedContentTypeEncoding = actionResultContentType.Encoding ?? defaultContentType.Encoding;
                return;
            }

            // 2. User sets the ContentType property on the http response directly
            if (!string.IsNullOrEmpty(httpResponseContentType))
            {
                MediaTypeHeaderValue mediaType;
                if (MediaTypeHeaderValue.TryParse(httpResponseContentType, out mediaType))
                {
                    resolvedContentType = httpResponseContentType;
                    resolvedContentTypeEncoding = mediaType.Encoding ?? defaultContentType.Encoding;
                }
                else
                {
                    resolvedContentType = httpResponseContentType;
                    resolvedContentTypeEncoding = defaultContentType.Encoding;
                }

                return;
            }

            // 3. Fall-back to the default content type
            resolvedContentType = defaultContentType.ToString();
            resolvedContentTypeEncoding = defaultContentType.Encoding;
        }
    }
}
