// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

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
                    () => request.GetFormAsync(), 
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
            // TODO: Tracked via https://github.com/aspnet/HttpAbstractions/issues/10. 
            // Determine what's the right way to map Accept-Language to culture.
            return CultureInfo.CurrentCulture;
        }
    }
}
