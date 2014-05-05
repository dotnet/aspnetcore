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
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FormValueProviderFactory : IValueProviderFactory
    {
        private const string FormEncodedContentType = "application/x-www-form-urlencoded";

        public async Task<IValueProvider> GetValueProviderAsync(RequestContext requestContext)
        {
            var request = requestContext.HttpContext.Request;
            
            if (IsSupportedContentType(request))
            {
                var queryCollection = await request.GetFormAsync();
                var culture = GetCultureInfo(request);
                return new ReadableStringCollectionValueProvider(queryCollection, culture);
            }

            return null;
        }

        private bool IsSupportedContentType(HttpRequest request)
        {
            var contentType = request.GetContentType();
            return contentType != null &&
                   string.Equals(contentType.ContentType, FormEncodedContentType, StringComparison.OrdinalIgnoreCase);
        }

        private static CultureInfo GetCultureInfo(HttpRequest request)
        {
            // TODO: Tracked via https://github.com/aspnet/HttpAbstractions/issues/10. Determine what's the right way to 
            // map Accept-Language to culture.
            return CultureInfo.CurrentCulture;
        }
    }
}
