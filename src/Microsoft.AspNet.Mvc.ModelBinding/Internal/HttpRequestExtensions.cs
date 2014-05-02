// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using Microsoft.AspNet.Abstractions;

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
                if (tokens.Length > 1 && tokens[1].TrimStart().StartsWith(CharSetToken, StringComparison.OrdinalIgnoreCase))
                {
                    charSet = tokens[1].TrimStart().Substring(CharSetToken.Length);
                }
                return new ContentTypeHeaderValue(tokens[0], charSet);
                
            }
            return null;
        }
    }
}
