// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// A default implementation of <see cref="ITempDataDictionaryFactory"/>.
    /// </summary>
    public class TempDataDictionaryFactory : ITempDataDictionaryFactory
    {
        private static readonly object Key = typeof(ITempDataDictionary);

        private readonly ITempDataProvider _provider;

        /// <summary>
        /// Creates a new <see cref="TempDataDictionaryFactory"/>.
        /// </summary>
        /// <param name="provider">The <see cref="ITempDataProvider"/>.</param>
        public TempDataDictionaryFactory(ITempDataProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _provider = provider;
        }

        /// <inheritdoc />
        public ITempDataDictionary GetTempData(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            object obj;
            ITempDataDictionary result;
            if (context.Items.TryGetValue(Key, out obj))
            {
                result = (ITempDataDictionary)obj;
            }
            else
            {
                result = new TempDataDictionary(context, _provider);
                context.Items.Add(Key, result);
            }

            return result;
        }
    }
}
