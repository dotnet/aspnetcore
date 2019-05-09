// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    using static HostingRequestStartingLog;

    internal class HostingRequestFinishedLog : IReadOnlyList<KeyValuePair<string, object>>
    {
        internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestFinishedLog)state).ToString();

        private readonly HostingRequestStartingLog _startLog;
        public string ContentType { get; }
        public long? ContentLength { get; }
        public int StatusCode { get; }
        public TimeSpan Elapsed { get; }

        private string _cachedToString;

        public int Count => 11;

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return new KeyValuePair<string, object>("ElapsedMilliseconds", Elapsed.TotalMilliseconds);
                    case 1:
                        return new KeyValuePair<string, object>("StatusCode", StatusCode);
                    case 2:
                        return new KeyValuePair<string, object>("ContentType", ContentType);
                    case 3:
                        return new KeyValuePair<string, object>("ContentLength", ContentLength);
                    case 4:
                        return new KeyValuePair<string, object>("Protocol", _startLog.Protocol);
                    case 5:
                        return new KeyValuePair<string, object>("Method", _startLog.Method);
                    case 6:
                        return new KeyValuePair<string, object>("Scheme", _startLog.Scheme);
                    case 7:
                        return new KeyValuePair<string, object>("Host", _startLog.Host);
                    case 8:
                        return new KeyValuePair<string, object>("PathBase", _startLog.PathBase);
                    case 9:
                        return new KeyValuePair<string, object>("Path", _startLog.Path);
                    case 10:
                        return new KeyValuePair<string, object>("QueryString", _startLog.QueryString);
                    default:
                        throw new IndexOutOfRangeException(nameof(index));
                }
            }
        }

        public HostingRequestFinishedLog(HostingRequestStartingLog startLog, int statusCode, long? contentLength, string contentType, TimeSpan elapsed)
        {
            _startLog = startLog;

            ContentType = !string.IsNullOrEmpty(contentType) ? Uri.EscapeUriString(contentType) : string.Empty;
            ContentLength = contentLength;
            StatusCode = statusCode;
            Elapsed = elapsed;
        }

        public override string ToString()
        {
            if (_cachedToString == null)
            {
                _cachedToString = $"Request finished {_startLog.ToStringWithoutPreamble()} - {StatusCode.ToString(CultureInfo.InvariantCulture)} {ValueOrEmptyMarker(ContentLength)} {ValueOrEmptyMarker(ContentType)} {Elapsed.TotalMilliseconds.ToString("0.0000", CultureInfo.InvariantCulture)}ms";
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
