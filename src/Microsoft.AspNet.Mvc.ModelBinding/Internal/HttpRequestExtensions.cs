// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    internal static class HttpRequestExtensions
    {
        private const string ContentTypeHeader = "Content-Type";
        private const string CharSetToken = "charset=";

        public static ContentTypeHeaderValue GetContentType(this HttpRequest httpRequest)
        {
            var headerValue = httpRequest.Headers[ContentTypeHeader];
            if (!string.IsNullOrEmpty(headerValue))
            {
                var tokens = headerValue.Split(new[] { ';' }, 2);
                string charSet = null;
                if (tokens.Length > 1 &&
                    tokens[1].TrimStart().StartsWith(CharSetToken, StringComparison.OrdinalIgnoreCase))
                {
                    charSet = tokens[1].TrimStart().Substring(CharSetToken.Length);
                }
                return new ContentTypeHeaderValue(tokens[0], charSet);
            }
            return null;
        }
    }
}
