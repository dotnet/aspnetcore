// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FormValueProviderFactory : IValueProviderFactory
    {
        public IValueProvider GetValueProvider([NotNull] ValueProviderFactoryContext context)
        {
            var request = context.HttpContext.Request;

            if (request.HasFormContentType)
            {
                var culture = GetCultureInfo(request);

                return new ReadableStringCollectionValueProvider(
                    BindingSource.Form,
                    async () => await request.ReadFormAsync(),
                    culture);
            }

            return null;
        }

        private static CultureInfo GetCultureInfo(HttpRequest request)
        {
            return CultureInfo.CurrentCulture;
        }
    }
}
