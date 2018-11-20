// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal class HostingRequestFinishedLog : IReadOnlyList<KeyValuePair<string, object>>
    {
        internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestFinishedLog)state).ToString();

        private readonly HttpContext _httpContext;
        private readonly TimeSpan _elapsed;

        private string _cachedToString;

        public int Count => 3;

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return new KeyValuePair<string, object>("ElapsedMilliseconds", _elapsed.TotalMilliseconds);
                    case 1:
                        return new KeyValuePair<string, object>("StatusCode", _httpContext.Response.StatusCode);
                    case 2:
                        return new KeyValuePair<string, object>("ContentType", _httpContext.Response.ContentType);
                    default:
                        throw new IndexOutOfRangeException(nameof(index));
                }
            }
        }

        public HostingRequestFinishedLog(HttpContext httpContext, TimeSpan elapsed)
        {
            _httpContext = httpContext;
            _elapsed = elapsed;
        }

        public override string ToString()
        {
            if (_cachedToString == null)
            {
                _cachedToString = string.Format(
                    CultureInfo.InvariantCulture,
                    "Request finished in {0}ms {1} {2}",
                    _elapsed.TotalMilliseconds,
                    _httpContext.Response.StatusCode,
                    _httpContext.Response.ContentType);
            }

            return _cachedToString;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
