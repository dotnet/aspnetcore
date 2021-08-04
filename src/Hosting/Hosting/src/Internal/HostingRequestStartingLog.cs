// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting
{
    internal readonly struct HostingRequestStartingLog : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private const string LogPreamble = "Request starting ";
        private const string EmptyEntry = "-";

        internal static readonly Func<HostingRequestStartingLog, Exception?, string> Callback = (state, exception) => state.ToString();

        private readonly HttpRequest _request;

        private readonly string _cachedToString;

        public int Count => 9;

        public KeyValuePair<string, object?> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object?>(nameof(_request.Protocol), _request.Protocol),
            1 => new KeyValuePair<string, object?>(nameof(_request.Method), _request.Method),
            2 => new KeyValuePair<string, object?>(nameof(_request.ContentType), _request.ContentType),
            3 => new KeyValuePair<string, object?>(nameof(_request.ContentLength), _request.ContentLength),
            4 => new KeyValuePair<string, object?>(nameof(_request.Scheme), _request.Scheme),
            5 => new KeyValuePair<string, object?>(nameof(_request.Host), _request.Host.Value),
            6 => new KeyValuePair<string, object?>(nameof(_request.PathBase), _request.PathBase.Value),
            7 => new KeyValuePair<string, object?>(nameof(_request.Path), _request.Path.Value),
            8 => new KeyValuePair<string, object?>(nameof(_request.QueryString), _request.QueryString.Value),
            _ => throw new IndexOutOfRangeException(nameof(index)),
        };

        public HostingRequestStartingLog(HttpContext httpContext)
        {
            _request = httpContext.Request;
            _cachedToString = $"{LogPreamble}{_request.Protocol} {_request.Method} {_request.Scheme}://{_request.Host.Value}{_request.PathBase.Value}{_request.Path.Value}{_request.QueryString.Value} {EscapedValueOrEmptyMarker(_request.ContentType)} {ValueOrEmptyMarker(_request.ContentLength)}";
        }

        public override string ToString()
        {
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

        internal string ToStringWithoutPreamble()
            => ToString().Substring(LogPreamble.Length);

        internal static string EscapedValueOrEmptyMarker(string? potentialValue)
            // Encode space as +
            => potentialValue?.Length > 0 ? potentialValue.Replace(' ', '+') : EmptyEntry;

        internal static string ValueOrEmptyMarker<T>(T? potentialValue) where T : struct, IFormattable
            => potentialValue?.ToString(null, CultureInfo.InvariantCulture) ?? EmptyEntry;
    }
}
