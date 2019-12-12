// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Headers
{
    public class ResponseHeaders
    {
        public ResponseHeaders(IHeaderDictionary headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            Headers = headers;
        }

        public IHeaderDictionary Headers { get; }

        public CacheControlHeaderValue CacheControl
        {
            get
            {
                return Headers.Get<CacheControlHeaderValue>(HeaderNames.CacheControl);
            }
            set
            {
                Headers.Set(HeaderNames.CacheControl, value);
            }
        }

        public ContentDispositionHeaderValue ContentDisposition
        {
            get
            {
                return Headers.Get<ContentDispositionHeaderValue>(HeaderNames.ContentDisposition);
            }
            set
            {
                Headers.Set(HeaderNames.ContentDisposition, value);
            }
        }

        public long? ContentLength
        {
            get
            {
                return Headers.ContentLength;
            }
            set
            {
                Headers.ContentLength = value;
            }
        }

        public ContentRangeHeaderValue ContentRange
        {
            get
            {
                return Headers.Get<ContentRangeHeaderValue>(HeaderNames.ContentRange);
            }
            set
            {
                Headers.Set(HeaderNames.ContentRange, value);
            }
        }

        public MediaTypeHeaderValue ContentType
        {
            get
            {
                return Headers.Get<MediaTypeHeaderValue>(HeaderNames.ContentType);
            }
            set
            {
                Headers.Set(HeaderNames.ContentType, value);
            }
        }

        public DateTimeOffset? Date
        {
            get
            {
                return Headers.GetDate(HeaderNames.Date);
            }
            set
            {
                Headers.SetDate(HeaderNames.Date, value);
            }
        }

        public EntityTagHeaderValue ETag
        {
            get
            {
                return Headers.Get<EntityTagHeaderValue>(HeaderNames.ETag);
            }
            set
            {
                Headers.Set(HeaderNames.ETag, value);
            }
        }
        public DateTimeOffset? Expires
        {
            get
            {
                return Headers.GetDate(HeaderNames.Expires);
            }
            set
            {
                Headers.SetDate(HeaderNames.Expires, value);
            }
        }

        public DateTimeOffset? LastModified
        {
            get
            {
                return Headers.GetDate(HeaderNames.LastModified);
            }
            set
            {
                Headers.SetDate(HeaderNames.LastModified, value);
            }
        }

        public Uri Location
        {
            get
            {
                Uri uri;
                if (Uri.TryCreate(Headers[HeaderNames.Location], UriKind.RelativeOrAbsolute, out uri))
                {
                    return uri;
                }
                return null;
            }
            set
            {
                Headers.Set(HeaderNames.Location, value == null ? null : UriHelper.Encode(value));
            }
        }

        public IList<SetCookieHeaderValue> SetCookie
        {
            get
            {
                return Headers.GetList<SetCookieHeaderValue>(HeaderNames.SetCookie);
            }
            set
            {
                Headers.SetList(HeaderNames.SetCookie, value);
            }
        }

        /// <summary>
        /// Gets the value of header with <paramref name="name"/>.
        /// </summary>
        /// <remarks><typeparamref name="T"/> must contain a TryParse method with the signature <c>public static bool TryParse(string, out T)</c>.</remarks>
        /// <typeparam name="T">The type of the header.
        /// The given type must have a static TryParse method.</typeparam>
        /// <param name="name">The name of the header to retrieve.</param>
        /// <returns>The value of the header.</returns>
        public T Get<T>(string name)
        {
            return Headers.Get<T>(name);
        }

        /// <summary>
        /// Gets the values of header with <paramref name="name"/>.
        /// </summary>
        /// <remarks><typeparamref name="T"/> must contain a TryParseList method with the signature <c>public static bool TryParseList(IList&lt;string&gt;, out IList&lt;T&gt;)</c>.</remarks>
        /// <typeparam name="T">The type of the header.
        /// The given type must have a static TryParseList method.</typeparam>
        /// <param name="name">The name of the header to retrieve.</param>
        /// <returns>List of values of the header.</returns>
        public IList<T> GetList<T>(string name)
        {
            return Headers.GetList<T>(name);
        }

        public void Set(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Headers.Set(name, value);
        }

        public void SetList<T>(string name, IList<T> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Headers.SetList<T>(name, values);
        }

        public void Append(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Headers.Append(name, value.ToString());
        }

        public void AppendList<T>(string name, IList<T> values)
        {
            Headers.AppendList<T>(name, values);
        }
    }
}
