// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace Microsoft.Extensions.Http.Logging
{
    internal class HttpHeadersLogValue : IReadOnlyList<KeyValuePair<string, object>>
    {
        private readonly Kind _kind;

        private string _formatted;
        private List<KeyValuePair<string, object>> _values;

        public HttpHeadersLogValue(Kind kind, HttpHeaders headers, HttpHeaders contentHeaders)
        {
            _kind = kind;

            Headers = headers;
            ContentHeaders = contentHeaders;
        }

        public HttpHeaders Headers { get; }

        public HttpHeaders ContentHeaders { get; }
        
        private List<KeyValuePair<string, object>> Values
        {
            get
            {
                if (_values == null)
                {
                    var values = new List<KeyValuePair<string, object>>();
                    
                    foreach (var kvp in Headers)
                    {
                        values.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
                    }

                    if (ContentHeaders != null)
                    {
                        foreach (var kvp in ContentHeaders)
                        {
                            values.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
                        }
                    }

                    _values = values;
                }

                return _values;
            }
        }

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                return Values[index];
            }
        }

        public int Count => Values.Count;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        public override string ToString()
        {
            if (_formatted == null)
            {
                var builder = new StringBuilder();
                builder.AppendLine(_kind == Kind.Request ? "Request Headers:" : "Response Headers:");

                for (var i = 0; i < Values.Count; i++)
                {
                    var kvp = Values[i];
                    builder.Append(kvp.Key);
                    builder.Append(": ");

                    foreach (var value in (IEnumerable<object>)kvp.Value)
                    {
                        builder.Append(value);
                        builder.Append(", ");
                    }

                    // Remove the extra ', '
                    builder.Remove(builder.Length - 2, 2);
                    builder.AppendLine();
                }

                _formatted = builder.ToString();
            }

            return _formatted;
        }

        public enum Kind
        {
            Request,
            Response,
        }
    }
}
