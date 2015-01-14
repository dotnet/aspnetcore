// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FormValueProviderFactory : IValueProviderFactory
    {
        private static MediaTypeHeaderValue _formEncodedContentType =
            MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

        public IValueProvider GetValueProvider([NotNull] ValueProviderFactoryContext context)
        {
            var request = context.HttpContext.Request;

            if (IsSupportedContentType(request))
            {
                var culture = GetCultureInfo(request);
                return new ReadableStringCollectionValueProvider<IFormDataValueProviderMetadata>(
                    async () => await request.ReadFormAsync(),
                    culture);
            }

            return null;
        }

        private bool IsSupportedContentType(HttpRequest request)
        {
            MediaTypeHeaderValue requestContentType = null;
            return MediaTypeHeaderValue.TryParse(request.ContentType, out requestContentType) &&
                _formEncodedContentType.IsSubsetOf(requestContentType);
        }

        private static CultureInfo GetCultureInfo(HttpRequest request)
        {
            return CultureInfo.CurrentCulture;
        }
    }
}
