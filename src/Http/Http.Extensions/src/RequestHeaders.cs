// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Headers
{
    public class RequestHeaders
    {
        public RequestHeaders(IHeaderDictionary headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            Headers = headers;
        }

        public IHeaderDictionary Headers { get; }

        public IList<MediaTypeHeaderValue> Accept
        {
            get
            {
                return Headers.GetList<MediaTypeHeaderValue>(HeaderNames.Accept);
            }
            set
            {
                Headers.SetList(HeaderNames.Accept, value);
            }
        }

        public IList<StringWithQualityHeaderValue> AcceptCharset
        {
            get
            {
                return Headers.GetList<StringWithQualityHeaderValue>(HeaderNames.AcceptCharset);
            }
            set
            {
                Headers.SetList(HeaderNames.AcceptCharset, value);
            }
        }

        public IList<StringWithQualityHeaderValue> AcceptEncoding
        {
            get
            {
                return Headers.GetList<StringWithQualityHeaderValue>(HeaderNames.AcceptEncoding);
            }
            set
            {
                Headers.SetList(HeaderNames.AcceptEncoding, value);
            }
        }

        public IList<StringWithQualityHeaderValue> AcceptLanguage
        {
            get
            {
                return Headers.GetList<StringWithQualityHeaderValue>(HeaderNames.AcceptLanguage);
            }
            set
            {
                Headers.SetList(HeaderNames.AcceptLanguage, value);
            }
        }

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

        public IList<CookieHeaderValue> Cookie
        {
            get
            {
                return Headers.GetList<CookieHeaderValue>(HeaderNames.Cookie);
            }
            set
            {
                Headers.SetList(HeaderNames.Cookie, value);
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

        public HostString Host
        {
            get
            {
                return HostString.FromUriComponent(Headers[HeaderNames.Host]);
            }
            set
            {
                Headers[HeaderNames.Host] = value.ToUriComponent();
            }
        }

        public IList<EntityTagHeaderValue> IfMatch
        {
            get
            {
                return Headers.GetList<EntityTagHeaderValue>(HeaderNames.IfMatch);
            }
            set
            {
                Headers.SetList(HeaderNames.IfMatch, value);
            }
        }

        public DateTimeOffset? IfModifiedSince
        {
            get
            {
                return Headers.GetDate(HeaderNames.IfModifiedSince);
            }
            set
            {
                Headers.SetDate(HeaderNames.IfModifiedSince, value);
            }
        }

        public IList<EntityTagHeaderValue> IfNoneMatch
        {
            get
            {
                return Headers.GetList<EntityTagHeaderValue>(HeaderNames.IfNoneMatch);
            }
            set
            {
                Headers.SetList(HeaderNames.IfNoneMatch, value);
            }
        }

        public RangeConditionHeaderValue IfRange
        {
            get
            {
                return Headers.Get<RangeConditionHeaderValue>(HeaderNames.IfRange);
            }
            set
            {
                Headers.Set(HeaderNames.IfRange, value);
            }
        }

        public DateTimeOffset? IfUnmodifiedSince
        {
            get
            {
                return Headers.GetDate(HeaderNames.IfUnmodifiedSince);
            }
            set
            {
                Headers.SetDate(HeaderNames.IfUnmodifiedSince, value);
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

        public RangeHeaderValue Range
        {
            get
            {
                return Headers.Get<RangeHeaderValue>(HeaderNames.Range);
            }
            set
            {
                Headers.Set(HeaderNames.Range, value);
            }
        }

        public Uri Referer
        {
            get
            {
                Uri uri;
                if (Uri.TryCreate(Headers[HeaderNames.Referer], UriKind.RelativeOrAbsolute, out uri))
                {
                    return uri;
                }
                return null;
            }
            set
            {
                Headers.Set(HeaderNames.Referer, value == null ? null : UriHelper.Encode(value));
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
