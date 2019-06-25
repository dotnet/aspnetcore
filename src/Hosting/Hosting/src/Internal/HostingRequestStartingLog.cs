// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting
{
    internal class HostingRequestStartingLog : IReadOnlyList<KeyValuePair<string, object>>
    {
        internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestStartingLog)state).ToString();

        private readonly HttpRequest _request;

        private string _cachedToString;

        public int Count => 9;

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return new KeyValuePair<string, object>("Protocol", _request.Protocol);
                    case 1:
                        return new KeyValuePair<string, object>("Method", _request.Method);
                    case 2:
                        return new KeyValuePair<string, object>("ContentType", _request.ContentType);
                    case 3:
                        return new KeyValuePair<string, object>("ContentLength", _request.ContentLength);
                    case 4:
                        return new KeyValuePair<string, object>("Scheme", _request.Scheme);
                    case 5:
                        return new KeyValuePair<string, object>("Host", _request.Host.ToString());
                    case 6:
                        return new KeyValuePair<string, object>("PathBase", _request.PathBase.ToString());
                    case 7:
                        return new KeyValuePair<string, object>("Path", _request.Path.ToString());
                    case 8:
                        return new KeyValuePair<string, object>("QueryString", _request.QueryString.ToString());
                    default:
                        throw new IndexOutOfRangeException(nameof(index));
                }
            }
        }

        public HostingRequestStartingLog(HttpContext httpContext)
        {
            _request = httpContext.Request;
        }

        public override string ToString()
        {
            if (_cachedToString == null)
            {
                _cachedToString = string.Format(
                    CultureInfo.InvariantCulture,
                    "Request starting {0} {1} {2}://{3}{4}{5}{6} {7} {8}",
                    _request.Protocol,
                    _request.Method,
                    _request.Scheme,
                    _request.Host.Value,
                    _request.PathBase.Value,
                    _request.Path.Value,
                    _request.QueryString.Value,
                    _request.ContentType,
                    _request.ContentLength);
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
