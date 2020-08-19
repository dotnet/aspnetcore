// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting
{
    using static HostingRequestStartingLog;

    internal class HostingRequestFinishedLog : IReadOnlyList<KeyValuePair<string, object>>
    {
        internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestFinishedLog)state).ToString();

        private readonly HostingApplication.Context _context;

        private string _cachedToString;
        public TimeSpan Elapsed { get; }

        public int Count => 11;

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                var request = _context.HttpContext.Request;
                var response = _context.HttpContext.Response;

                return index switch
                {
                    0 => new KeyValuePair<string, object>("ElapsedMilliseconds", Elapsed.TotalMilliseconds),
                    1 => new KeyValuePair<string, object>(nameof(response.StatusCode), response.StatusCode),
                    2 => new KeyValuePair<string, object>(nameof(response.ContentType), response.ContentType),
                    3 => new KeyValuePair<string, object>(nameof(response.ContentLength), response.ContentLength),
                    4 => new KeyValuePair<string, object>(nameof(request.Protocol), request.Protocol),
                    5 => new KeyValuePair<string, object>(nameof(request.Method), request.Method),
                    6 => new KeyValuePair<string, object>(nameof(request.Scheme), request.Scheme),
                    7 => new KeyValuePair<string, object>(nameof(request.Host), request.Host.Value),
                    8 => new KeyValuePair<string, object>(nameof(request.PathBase), request.PathBase.Value),
                    9 => new KeyValuePair<string, object>(nameof(request.Path), request.Path.Value),
                    10 => new KeyValuePair<string, object>(nameof(request.QueryString), request.QueryString.Value),
                    _ => throw new IndexOutOfRangeException(nameof(index)),
                };
            }
        }

        public HostingRequestFinishedLog(HostingApplication.Context context, TimeSpan elapsed)
        {
            _context = context;
            Elapsed = elapsed;
        }

        public override string ToString()
        {
            if (_cachedToString == null)
            {
                var response = _context.HttpContext.Response;
                _cachedToString = $"Request finished {_context.StartLog.ToStringWithoutPreamble()} - {response.StatusCode.ToString(CultureInfo.InvariantCulture)} {ValueOrEmptyMarker(response.ContentLength)} {EscapedValueOrEmptyMarker(response.ContentType)} {Elapsed.TotalMilliseconds.ToString("0.0000", CultureInfo.InvariantCulture)}ms";
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
