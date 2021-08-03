// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.AspNetCore.Hosting
{
    using static HostingRequestStartingLog;

    internal struct HostingRequestFinishedLog : IReadOnlyList<KeyValuePair<string, object?>>
    {
        internal static readonly Func<HostingRequestFinishedLog, Exception?, string> Callback = (state, exception) => state.ToString();

        private readonly HostingApplication.Context _context;

        private string? _cachedToString;
        public TimeSpan Elapsed { get; }

        public int Count => 11;

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                Debug.Assert(_context.HttpContext != null);

                return index switch
                {
                    0 => new KeyValuePair<string, object?>("ElapsedMilliseconds", Elapsed.TotalMilliseconds),
                    1 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Response.StatusCode), _context.HttpContext.Response.StatusCode),
                    2 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Response.ContentType), _context.HttpContext.Response.ContentType),
                    3 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Response.ContentLength), _context.HttpContext.Response.ContentLength),
                    4 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Request.Protocol), _context.HttpContext.Request.Protocol),
                    5 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Request.Method), _context.HttpContext.Request.Method),
                    6 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Request.Scheme), _context.HttpContext.Request.Scheme),
                    7 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Request.Host), _context.HttpContext.Request.Host.Value),
                    8 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Request.PathBase), _context.HttpContext.Request.PathBase.Value),
                    9 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Request.Path), _context.HttpContext.Request.Path.Value),
                    10 => new KeyValuePair<string, object?>(nameof(_context.HttpContext.Request.QueryString), _context.HttpContext.Request.QueryString.Value),
                    _ => throw new IndexOutOfRangeException(nameof(index)),
                };
            }
        }

        public HostingRequestFinishedLog(HostingApplication.Context context, TimeSpan elapsed)
        {
            _context = context;
            Elapsed = elapsed;
            _cachedToString = null;
        }

        public override string ToString()
        {
            if (_cachedToString == null)
            {
                Debug.Assert(_context.HttpContext != null && _context.StartLog != null);

                var response = _context.HttpContext.Response;
                _cachedToString = $"Request finished {_context.StartLog.Value.ToStringWithoutPreamble()} - {response.StatusCode.ToString(CultureInfo.InvariantCulture)} {ValueOrEmptyMarker(response.ContentLength)} {EscapedValueOrEmptyMarker(response.ContentType)} {Elapsed.TotalMilliseconds.ToString("0.0000", CultureInfo.InvariantCulture)}ms";
            }

            return _cachedToString;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
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
