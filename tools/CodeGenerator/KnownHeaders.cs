// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CodeGenerator
{
    public class KnownHeaders
    {
        static string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
        {
            return values.Any() ? values.Select(formatter).Aggregate((a, b) => a + b) : "";
        }

        static string AppendSwitch(IEnumerable<IGrouping<int, KnownHeader>> values, string className) =>
             $@"var pUL = (ulong*)pUB;
                var pUI = (uint*)pUB;
                var pUS = (ushort*)pUB;
                var stringValue = new StringValues(value);
                switch (keyLength)
                {{{Each(values, byLength => $@"
                    case {byLength.Key}:
                        {{{Each(byLength, header => $@"
                            if ({header.EqualIgnoreCaseBytes()})
                            {{{(header.Identifier == "ContentLength" ? $@"
                                if (_contentLength.HasValue)
                                {{
                                    BadHttpRequestException.Throw(RequestRejectionReason.MultipleContentLengths);
                                }}
                                else
                                {{
                                    _contentLength = ParseContentLength(value);
                                }}
                                return;" : $@"
                                if ({header.TestBit()})
                                {{
                                    _headers._{header.Identifier} = AppendValue(_headers._{header.Identifier}, value);
                                }}
                                else
                                {{
                                    {header.SetBit()};
                                    _headers._{header.Identifier} = stringValue;{(header.EnhancedSetter == false ? "" : $@"
                                    _headers._raw{header.Identifier} = null;")}
                                }}
                                return;")}
                            }}
                        ")}}}
                        break;
                ")}}}";

        class KnownHeader
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public string Identifier => Name.Replace("-", "");

            public byte[] Bytes => Encoding.ASCII.GetBytes($"\r\n{Name}: ");
            public int BytesOffset { get; set; }
            public int BytesCount { get; set; }
            public bool ExistenceCheck { get; set; }
            public bool FastCount { get; set; }
            public bool EnhancedSetter { get; set; }
            public bool PrimaryHeader { get; set; }
            public string TestBit() => $"(_bits & {1L << Index}L) != 0";
            public string TestTempBit() => $"(tempBits & {1L << Index}L) != 0";
            public string TestNotTempBit() => $"(tempBits & ~{1L << Index}L) == 0";
            public string TestNotBit() => $"(_bits & {1L << Index}L) == 0";
            public string SetBit() => $"_bits |= {1L << Index}L";
            public string ClearBit() => $"_bits &= ~{1L << Index}L";

            public string EqualIgnoreCaseBytes()
            {
                var result = "";
                var delim = "";
                var index = 0;
                while (index != Name.Length)
                {
                    if (Name.Length - index >= 8)
                    {
                        result += delim + Term(Name, index, 8, "pUL", "uL");
                        index += 8;
                    }
                    else if (Name.Length - index >= 4)
                    {
                        result += delim + Term(Name, index, 4, "pUI", "u");
                        index += 4;
                    }
                    else if (Name.Length - index >= 2)
                    {
                        result += delim + Term(Name, index, 2, "pUS", "u");
                        index += 2;
                    }
                    else
                    {
                        result += delim + Term(Name, index, 1, "pUB", "u");
                        index += 1;
                    }
                    delim = " && ";
                }
                return $"({result})";
            }
            protected string Term(string name, int offset, int count, string array, string suffix)
            {
                ulong mask = 0;
                ulong comp = 0;
                for (var scan = 0; scan < count; scan++)
                {
                    var ch = (byte)name[offset + count - scan - 1];
                    var isAlpha = (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
                    comp = (comp << 8) + (ch & (isAlpha ? 0xdfu : 0xffu));
                    mask = (mask << 8) + (isAlpha ? 0xdfu : 0xffu);
                }
                return $"(({array}[{offset / count}] & {mask}{suffix}) == {comp}{suffix})";
            }
        }

        public static string GeneratedFile()
        {
            var requestPrimaryHeaders = new[]
            {
                "Accept",
                "Connection",
                "Host",
                "User-Agent"
            };
            var responsePrimaryHeaders = new[]
            {
                "Connection",
                "Date",
                "Content-Type",
                "Server",
            };
            var commonHeaders = new[]
            {
                "Cache-Control",
                "Connection",
                "Date",
                "Keep-Alive",
                "Pragma",
                "Trailer",
                "Transfer-Encoding",
                "Upgrade",
                "Via",
                "Warning",
                "Allow",
                "Content-Type",
                "Content-Encoding",
                "Content-Language",
                "Content-Location",
                "Content-MD5",
                "Content-Range",
                "Expires",
                "Last-Modified"
            };
            // http://www.w3.org/TR/cors/#syntax
            var corsRequestHeaders = new[]
            {
                "Origin",
                "Access-Control-Request-Method",
                "Access-Control-Request-Headers",
            };
            var requestHeadersExistence = new[]
            {
                "Connection",
                "Transfer-Encoding",
            };
            var requestHeadersCount = new[]
            {
                "Host"
            };
            var requestHeaders = commonHeaders.Concat(new[]
            {
                "Accept",
                "Accept-Charset",
                "Accept-Encoding",
                "Accept-Language",
                "Authorization",
                "Cookie",
                "Expect",
                "From",
                "Host",
                "If-Match",
                "If-Modified-Since",
                "If-None-Match",
                "If-Range",
                "If-Unmodified-Since",
                "Max-Forwards",
                "Proxy-Authorization",
                "Referer",
                "Range",
                "TE",
                "Translate",
                "User-Agent",
            })
            .Concat(corsRequestHeaders)
            .Select((header, index) => new KnownHeader
            {
                Name = header,
                Index = index,
                PrimaryHeader = requestPrimaryHeaders.Contains(header),
                ExistenceCheck = requestHeadersExistence.Contains(header),
                FastCount = requestHeadersCount.Contains(header)
            })
            .Concat(new[] { new KnownHeader
            {
                Name = "Content-Length",
                Index = -1,
                PrimaryHeader = requestPrimaryHeaders.Contains("Content-Length")
            }})
            .ToArray();
            Debug.Assert(requestHeaders.Length <= 64);
            Debug.Assert(requestHeaders.Max(x => x.Index) <= 62);

            var responseHeadersExistence = new[]
            {
                "Connection",
                "Server",
                "Date",
                "Transfer-Encoding"
            };
            var enhancedHeaders = new[]
            {
                "Connection",
                "Server",
                "Date",
                "Transfer-Encoding"
            };
            // http://www.w3.org/TR/cors/#syntax
            var corsResponseHeaders = new[]
            {
                "Access-Control-Allow-Credentials",
                "Access-Control-Allow-Headers",
                "Access-Control-Allow-Methods",
                "Access-Control-Allow-Origin",
                "Access-Control-Expose-Headers",
                "Access-Control-Max-Age",
            };
            var responseHeaders = commonHeaders.Concat(new[]
            {
                "Accept-Ranges",
                "Age",
                "ETag",
                "Location",
                "Proxy-Authenticate",
                "Retry-After",
                "Server",
                "Set-Cookie",
                "Vary",
                "WWW-Authenticate",
            })
            .Concat(corsResponseHeaders)
            .Select((header, index) => new KnownHeader
            {
                Name = header,
                Index = index,
                EnhancedSetter = enhancedHeaders.Contains(header),
                ExistenceCheck = responseHeadersExistence.Contains(header),
                PrimaryHeader = responsePrimaryHeaders.Contains(header)
            })
            .Concat(new[] { new KnownHeader
            {
                Name = "Content-Length",
                Index = -1,
                EnhancedSetter = enhancedHeaders.Contains("Content-Length"),
                PrimaryHeader = responsePrimaryHeaders.Contains("Content-Length")
            }})
            .ToArray();
            // 63 for reponseHeaders as it steals one bit for Content-Length in CopyTo(ref MemoryPoolIterator output)
            Debug.Assert(responseHeaders.Length <= 63);
            Debug.Assert(responseHeaders.Max(x => x.Index) <= 62);

            var loops = new[]
            {
                new
                {
                    Headers = requestHeaders,
                    HeadersByLength = requestHeaders.GroupBy(x => x.Name.Length),
                    ClassName = "HttpRequestHeaders",
                    Bytes = default(byte[])
                },
                new
                {
                    Headers = responseHeaders,
                    HeadersByLength = responseHeaders.GroupBy(x => x.Name.Length),
                    ClassName = "HttpResponseHeaders",
                    Bytes = responseHeaders.SelectMany(header => header.Bytes).ToArray()
                }
            };
            foreach (var loop in loops.Where(l => l.Bytes != null))
            {
                var offset = 0;
                foreach (var header in loop.Headers)
                {
                    header.BytesOffset = offset;
                    header.BytesCount += header.Bytes.Length;
                    offset += header.BytesCount;
                }
            }
            return $@"// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using System.Buffers;
using System.IO.Pipelines;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{{
{Each(loops, loop => $@"
    public partial class {loop.ClassName}
    {{{(loop.Bytes != null ?
        $@"
        private static byte[] _headerBytes = new byte[]
        {{
            {Each(loop.Bytes, b => $"{b},")}
        }};"
        : "")}

        private long _bits = 0;
        private HeaderReferences _headers;
{Each(loop.Headers.Where(header => header.ExistenceCheck), header => $@"
        public bool Has{header.Identifier} => {header.TestBit()};")}
{Each(loop.Headers.Where(header => header.FastCount), header => $@"
        public int {header.Identifier}Count => _headers._{header.Identifier}.Count;")}
        {Each(loop.Headers, header => $@"
        public StringValues Header{header.Identifier}
        {{{(header.Identifier == "ContentLength" ? $@"
            get
            {{
                StringValues value;
                if (_contentLength.HasValue)
                {{
                    value = new StringValues(HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                }}
                return value;
            }}
            set
            {{
                _contentLength = ParseContentLength(value);
            }}" : $@"
            get
            {{
                StringValues value;
                if ({header.TestBit()})
                {{
                    value = _headers._{header.Identifier};
                }}
                return value;
            }}
            set
            {{
                {header.SetBit()};
                _headers._{header.Identifier} = value; {(header.EnhancedSetter == false ? "" : $@"
                _headers._raw{header.Identifier} = null;")}
            }}")}
        }}")}
{Each(loop.Headers.Where(header => header.EnhancedSetter), header => $@"
        public void SetRaw{header.Identifier}(in StringValues value, byte[] raw)
        {{
            {header.SetBit()};
            _headers._{header.Identifier} = value;
            _headers._raw{header.Identifier} = raw;
        }}")}
        protected override int GetCountFast()
        {{
            return (_contentLength.HasValue ? 1 : 0 ) + BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }}

        protected override bool TryGetValueFast(string key, out StringValues value)
        {{
            switch (key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {{{(header.Identifier == "ContentLength" ? @"
                            if (_contentLength.HasValue)
                            {
                                value = HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value);
                                return true;
                            }
                            return false;" : $@"
                            if ({header.TestBit()})
                            {{
                                value = _headers._{header.Identifier};
                                return true;
                            }}
                            return false;")}
                        }}")}
                    }}
                    break;")}
            }}

            return MaybeUnknown?.TryGetValue(key, out value) ?? false;
        }}

        protected override void SetValueFast(string key, in StringValues value)
        {{{(loop.ClassName == "HttpResponseHeaders" ? @"
            ValidateHeaderValueCharacters(value);" : "")}
            switch (key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {{{(header.Identifier == "ContentLength" ? $@"
                            _contentLength = ParseContentLength(value.ToString());" : $@"
                            {header.SetBit()};
                            _headers._{header.Identifier} = value;{(header.EnhancedSetter == false ? "" : $@"
                            _headers._raw{header.Identifier} = null;")}")}
                            return;
                        }}")}
                    }}
                    break;")}
            }}

            SetValueUnknown(key, value);
        }}

        protected override bool AddValueFast(string key, in StringValues value)
        {{{(loop.ClassName == "HttpResponseHeaders" ? @"
            ValidateHeaderValueCharacters(value);" : "")}
            switch (key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {{{(header.Identifier == "ContentLength" ? $@"
                            if (!_contentLength.HasValue)
                            {{
                                _contentLength = ParseContentLength(value);
                                return true;
                            }}
                            return false;" : $@"
                            if ({header.TestNotBit()})
                            {{
                                {header.SetBit()};
                                _headers._{header.Identifier} = value;{(header.EnhancedSetter == false ? "" : $@"
                                _headers._raw{header.Identifier} = null;")}
                                return true;
                            }}
                            return false;")}
                        }}")}
                    }}
                    break;")}
            }}
{(loop.ClassName == "HttpResponseHeaders" ? @"
            ValidateHeaderNameCharacters(key);" : "")}
            Unknown.Add(key, value);
            // Return true, above will throw and exit for false
            return true;
        }}

        protected override bool RemoveFast(string key)
        {{
            switch (key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {{{(header.Identifier == "ContentLength" ? @"
                            if (_contentLength.HasValue)
                            {
                                _contentLength = null;
                                return true;
                            }
                            return false;" : $@"
                            if ({header.TestBit()})
                            {{
                                {header.ClearBit()};
                                _headers._{header.Identifier} = default(StringValues);{(header.EnhancedSetter == false ? "" : $@"
                                _headers._raw{header.Identifier} = null;")}
                                return true;
                            }}
                            return false;")}
                        }}")}
                    }}
                    break;")}
            }}

            return MaybeUnknown?.Remove(key) ?? false;
        }}

        protected override void ClearFast()
        {{
            MaybeUnknown?.Clear();
            _contentLength = null;
            var tempBits = _bits;
            _bits = 0;
            if(HttpHeaders.BitCount(tempBits) > 12)
            {{
                _headers = default(HeaderReferences);
                return;
            }}
            {Each(loop.Headers.Where(header => header.Identifier != "ContentLength").OrderBy(h => !h.PrimaryHeader), header => $@"
            if ({header.TestTempBit()})
            {{
                _headers._{header.Identifier} = default(StringValues);
                if({header.TestNotTempBit()})
                {{
                    return;
                }}
                tempBits &= ~{1L << header.Index}L;
            }}
            ")}
        }}

        protected override bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {{
            if (arrayIndex < 0)
            {{
                return false;
            }}
            {Each(loop.Headers.Where(header => header.Identifier != "ContentLength"), header => $@"
                if ({header.TestBit()})
                {{
                    if (arrayIndex == array.Length)
                    {{
                        return false;
                    }}
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(""{header.Name}"", _headers._{header.Identifier});
                    ++arrayIndex;
                }}")}
                if (_contentLength.HasValue)
                {{
                    if (arrayIndex == array.Length)
                    {{
                        return false;
                    }}
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(""Content-Length"", HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                    ++arrayIndex;
                }}
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);

            return true;
        }}
        {(loop.ClassName == "HttpResponseHeaders" ? $@"
        internal void CopyToFast(ref CountingBufferWriter<PipeWriter> output)
        {{
            var tempBits = _bits | (_contentLength.HasValue ? {1L << 63}L : 0);
            {Each(loop.Headers.Where(header => header.Identifier != "ContentLength").OrderBy(h => !h.PrimaryHeader), header => $@"
                if ({header.TestTempBit()})
                {{ {(header.EnhancedSetter == false ? "" : $@"
                    if (_headers._raw{header.Identifier} != null)
                    {{
                        output.Write(_headers._raw{header.Identifier});
                    }}
                    else ")}
                    {{
                        var valueCount = _headers._{header.Identifier}.Count;
                        for (var i = 0; i < valueCount; i++)
                        {{
                            var value = _headers._{header.Identifier}[i];
                            if (value != null)
                            {{
                                output.Write(new ReadOnlySpan<byte>(_headerBytes, {header.BytesOffset}, {header.BytesCount}));
                                PipelineExtensions.WriteAsciiNoValidation(ref output, value);
                            }}
                        }}
                    }}

                    if({header.TestNotTempBit()})
                    {{
                        return;
                    }}
                    tempBits &= ~{1L << header.Index}L;
                }}{(header.Identifier == "Server" ? $@"
                if ((tempBits & {1L << 63}L) != 0)
                {{
                    output.Write(new ReadOnlySpan<byte>(_headerBytes, {loop.Headers.First(x => x.Identifier == "ContentLength").BytesOffset}, {loop.Headers.First(x => x.Identifier == "ContentLength").BytesCount}));
                    PipelineExtensions.WriteNumeric(ref output, (ulong)ContentLength.Value);

                    if((tempBits & ~{1L << 63}L) == 0)
                    {{
                        return;
                    }}
                    tempBits &= ~{1L << 63}L;
                }}" : "")}")}
        }}" : "")}
        {(loop.ClassName == "HttpRequestHeaders" ? $@"
        public unsafe void Append(byte* pKeyBytes, int keyLength, string value)
        {{
            var pUB = pKeyBytes;
            {AppendSwitch(loop.Headers.Where(h => h.PrimaryHeader).GroupBy(x => x.Name.Length), loop.ClassName)}

            AppendNonPrimaryHeaders(pKeyBytes, keyLength, value);
        }}

        private unsafe void AppendNonPrimaryHeaders(byte* pKeyBytes, int keyLength, string value)
        {{
                var pUB = pKeyBytes;
                {AppendSwitch(loop.Headers.Where(h => !h.PrimaryHeader).GroupBy(x => x.Name.Length), loop.ClassName)}

                AppendUnknownHeaders(pKeyBytes, keyLength, value);
        }}" : "")}

        private struct HeaderReferences
        {{{Each(loop.Headers.Where(header => header.Identifier != "ContentLength"), header => @"
            public StringValues _" + header.Identifier + ";")}
            {Each(loop.Headers.Where(header => header.EnhancedSetter), header => @"
            public byte[] _raw" + header.Identifier + ";")}
        }}

        public partial struct Enumerator
        {{
            public bool MoveNext()
            {{
                switch (_state)
                {{
                    {Each(loop.Headers.Where(header => header.Identifier != "ContentLength"), header => $@"
                    case {header.Index}:
                        goto state{header.Index};
                    ")}
                    case {loop.Headers.Count()}:
                        goto state{loop.Headers.Count()};
                    default:
                        goto state_default;
                }}
                {Each(loop.Headers.Where(header => header.Identifier != "ContentLength"), header => $@"
                state{header.Index}:
                    if ({header.TestBit()})
                    {{
                        _current = new KeyValuePair<string, StringValues>(""{header.Name}"", _collection._headers._{header.Identifier});
                        _state = {header.Index + 1};
                        return true;
                    }}
                ")}
                state{loop.Headers.Count()}:
                    if (_collection._contentLength.HasValue)
                    {{
                        _current = new KeyValuePair<string, StringValues>(""Content-Length"", HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.Value));
                        _state = {loop.Headers.Count() + 1};
                        return true;
                    }}
                state_default:
                    if (!_hasUnknown || !_unknownEnumerator.MoveNext())
                    {{
                        _current = default(KeyValuePair<string, StringValues>);
                        return false;
                    }}
                    _current = _unknownEnumerator.Current;
                    return true;
            }}
        }}
    }}
")}}}";
        }
    }
}
