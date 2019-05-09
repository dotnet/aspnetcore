// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal class HostingRequestStartingLog : IReadOnlyList<KeyValuePair<string, object>>
    {
        private const string LogPreamble = "Request starting ";
        private const string EmptyEntry = "-";

        internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestStartingLog)state).ToString();

        public string Protocol { get; }
        public string Method { get; }
        public string ContentType { get; }
        public long? ContentLength { get; }
        public string Scheme { get; }
        public string Host { get; }
        public string PathBase { get; }
        public string Path { get; }
        public string QueryString { get; }

        private string _cachedToString;

        public int Count => 9;

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return new KeyValuePair<string, object>("Protocol", Protocol);
                    case 1:
                        return new KeyValuePair<string, object>("Method", Method);
                    case 2:
                        return new KeyValuePair<string, object>("ContentType", ContentType);
                    case 3:
                        return new KeyValuePair<string, object>("ContentLength", ContentLength);
                    case 4:
                        return new KeyValuePair<string, object>("Scheme", Scheme);
                    case 5:
                        return new KeyValuePair<string, object>("Host", Host);
                    case 6:
                        return new KeyValuePair<string, object>("PathBase", PathBase);
                    case 7:
                        return new KeyValuePair<string, object>("Path", Path);
                    case 8:
                        return new KeyValuePair<string, object>("QueryString", QueryString);
                    default:
                        throw new IndexOutOfRangeException(nameof(index));
                }
            }
        }

        public HostingRequestStartingLog(HttpContext httpContext)
        {
            var request = httpContext.Request;

            Protocol = request.Protocol;
            Method = request.Method;
            ContentType = !string.IsNullOrEmpty(request.ContentType) ? Uri.EscapeUriString(request.ContentType) : string.Empty;
            ContentLength = request.ContentLength;
            Scheme = request.Scheme;
            Host = request.Host.Value;
            PathBase = request.PathBase.Value;
            Path = request.Path.Value;
            QueryString = request.QueryString.Value;
        }

        public override string ToString()
        {
            if (_cachedToString == null)
            {
                _cachedToString = $"{LogPreamble}{Protocol} {Method} {Scheme}://{Host}{PathBase}{Path}{QueryString} {ValueOrEmptyMarker(ContentType)} {ValueOrEmptyMarker(ContentLength)}";
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

        internal string ToStringWithoutPreamble()
            => ToString().Substring(LogPreamble.Length);

        internal static string ValueOrEmptyMarker(string potentialValue)
            => potentialValue?.Length > 0 ? potentialValue : EmptyEntry;

        internal static string ValueOrEmptyMarker<T>(T? potentialValue) where T : struct, IFormattable
            => potentialValue?.ToString(null, CultureInfo.InvariantCulture) ?? EmptyEntry;
    }
}
