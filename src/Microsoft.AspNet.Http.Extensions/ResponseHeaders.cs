// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Headers
{
    public class ResponseHeaders
    {
        public ResponseHeaders([NotNull] IHeaderDictionary headers)
        {
            Headers = headers;
        }

        public IHeaderDictionary Headers { get; private set; }

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
                return Headers.Get<long?>(HeaderNames.ContentLength);
            }
            set
            {
                Headers.Set(HeaderNames.ContentLength, value.HasValue ? HeaderUtilities.FormatInt64(value.Value) : null);
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

        public T Get<T>(string name)
        {
            return Headers.Get<T>(name);
        }

        public IList<T> GetList<T>(string name)
        {
            return Headers.GetList<T>(name);
        }

        public void Set([NotNull] string name, object value)
        {
            Headers.Set(name, value);
        }

        public void SetList<T>([NotNull] string name, IList<T> values)
        {
            Headers.SetList<T>(name, values);
        }

        public void Append([NotNull] string name, [NotNull] object value)
        {
            Headers.Append(name, value.ToString());
        }

        public void AppendList<T>([NotNull] string name, [NotNull] IList<T> values)
        {
            Headers.Append(name, values.Select(value => value.ToString()).ToArray());
        }
    }
}