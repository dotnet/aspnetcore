// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Localization
{
    /// <summary>
    /// Details about the culture for an <see cref="Http.HttpRequest"/>.
    /// </summary>
    public class RequestCulture
    {
        private static readonly ConcurrentDictionary<CacheKey, RequestCulture> _cache = new ConcurrentDictionary<CacheKey, RequestCulture>();

        private RequestCulture([NotNull] CultureInfo culture)
            : this (culture, culture)
        {

        }

        private RequestCulture([NotNull] CultureInfo culture, [NotNull] CultureInfo uiCulture)
        {
            Culture = culture;
            UICulture = uiCulture;
        }

        /// <summary>
        /// Gets the <see cref="CultureInfo"/> for the request to be used for formatting.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Gets the <see cref="CultureInfo"/> for the request to be used for text, i.e. language;
        /// </summary>
        public CultureInfo UICulture { get; }

        /// <summary>
        /// Gets a cached <see cref="RequestCulture"/> instance that has its <see cref="Culture"/> and <see cref="UICulture"/>
        /// properties set to the same <see cref="CultureInfo"/> value.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> for the request.</param>
        public static RequestCulture GetRequestCulture([NotNull] CultureInfo culture)
        {
            return GetRequestCulture(culture, culture);
        }

        /// <summary>
        /// Gets a cached <see cref="RequestCulture"/> instance that has its <see cref="Culture"/> and <see cref="UICulture"/>
        /// properties set to the respective <see cref="CultureInfo"/> values provided.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> for the request to be used for formatting.</param>
        /// <param name="uiCulture">The <see cref="CultureInfo"/> for the request to be used for text, i.e. language.</param>
        /// <returns></returns>
        public static RequestCulture GetRequestCulture([NotNull] CultureInfo culture, [NotNull] CultureInfo uiCulture)
        {
            var key = new CacheKey(culture, uiCulture);
            return _cache.GetOrAdd(key, k => new RequestCulture(culture, uiCulture));
        }

        private class CacheKey
        {
            private readonly int _hashCode;

            public CacheKey(CultureInfo culture, CultureInfo uiCulture)
            {
                Culture = culture;
                UICulture = uiCulture;
                _hashCode = new { Culture, UICulture }.GetHashCode();
            }

            public CultureInfo Culture { get; }

            public CultureInfo UICulture { get; }

            public bool Equals(CacheKey other)
            {
                return Culture == other.Culture && UICulture == other.UICulture;
            }

            public override bool Equals(object obj)
            {
                var other = obj as CacheKey;

                if (other != null)
                {
                    return Equals(other);
                }

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }
    }
}
