// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FormValueProviderFactory : IValueProviderFactory
    {
        private const string FormEncodedContentType = "application/x-www-form-urlencoded";

        public IValueProvider GetValueProvider([NotNull] ValueProviderFactoryContext context)
        {
            var request = context.HttpContext.Request;

            if (IsSupportedContentType(request))
            {
                var culture = GetCultureInfo(request);
                return new ReadableStringCollectionValueProvider(request.GetFormAsync, culture);
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
            // TODO: Tracked via https://github.com/aspnet/HttpAbstractions/issues/10. 
            // Determine what's the right way to map Accept-Language to culture.
            return CultureInfo.CurrentCulture;
        }
    }
}
