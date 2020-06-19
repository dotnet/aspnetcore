// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Net.Http.Headers;

namespace CodeGenerator
{
    public class KnownHeaders
    {
        public readonly static KnownHeader[] RequestHeaders;
        public readonly static KnownHeader[] ResponseHeaders;
        public readonly static KnownHeader[] ResponseTrailers;

        static KnownHeaders()
        {
            var requestPrimaryHeaders = new[]
            {
                ":authority",
                ":method",
                ":path",
                ":scheme",
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
                "Content-Length",
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
            RequestHeaders = commonHeaders.Concat(new[]
            {
                ":authority",
                ":method",
                ":path",
                ":scheme",
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
                "DNT",
                "Upgrade-Insecure-Requests",
                "Request-Id",
                "Correlation-Context",
                "TraceParent",
                "TraceState"
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
            ResponseHeaders = commonHeaders.Concat(new[]
            {
                "Accept-Ranges",
                "Age",
                "Alt-Svc",
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
                Index = 63,
                EnhancedSetter = enhancedHeaders.Contains("Content-Length"),
                PrimaryHeader = responsePrimaryHeaders.Contains("Content-Length")
            }})
            .ToArray();

            ResponseTrailers = new[]
            {
                "ETag",
            }
            .Select((header, index) => new KnownHeader
            {
                Name = header,
                Index = index,
                EnhancedSetter = enhancedHeaders.Contains(header),
                ExistenceCheck = responseHeadersExistence.Contains(header),
                PrimaryHeader = responsePrimaryHeaders.Contains(header)
            })
            .ToArray();
        }

        static string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
        {
            return values.Any() ? values.Select(formatter).Aggregate((a, b) => a + b) : "";
        }

        static string Each<T>(IEnumerable<T> values, Func<T, int, string> formatter)
        {
            return values.Any() ? values.Select(formatter).Aggregate((a, b) => a + b) : "";
        }

        static string AppendSwitch(IEnumerable<IGrouping<int, KnownHeader>> values) =>
             $@"switch (name.Length)
            {{{Each(values, byLength => $@"
                case {byLength.Key}:{AppendSwitchSection(byLength.Key, byLength.OrderBy(h => (h.PrimaryHeader ? "_" : "") + h.Name))}
                    break;")}
            }}";

        static string AppendSwitchSection(int length, IOrderedEnumerable<KnownHeader> values)
        {
            var useVarForFirstTerm = values.Count() > 1 && values.Select(h => h.FirstNameIgnoreCaseSegment()).Distinct().Count() == 1;
            var firstTermVarExpression = values.Select(h => h.FirstNameIgnoreCaseSegment()).FirstOrDefault();
            var firstTermVar = $"firstTerm{length}";

            var start = "";
            if (useVarForFirstTerm)
            {
                start = $@"
                    var {firstTermVar} = {firstTermVarExpression};";
            }
            else
            {
                firstTermVar = "";
            }

            string GenerateIfBody(KnownHeader header, string extraIndent = "")
            {
                if (header.Identifier == "ContentLength")
                {
                    return $@"
                        {extraIndent}if (ReferenceEquals(EncodingSelector, KestrelServerOptions.DefaultRequestHeaderEncodingSelector)
                        {extraIndent}    || ReferenceEquals(EncodingSelector, KestrelServerOptions.DefaultLatin1RequestHeaderEncodingSelector))
                        {extraIndent}{{
                        {extraIndent}    AppendContentLength(value);
                        {extraIndent}}}
                        {extraIndent}else
                        {extraIndent}{{
                        {extraIndent}    AppendContentLengthCustomEncoding(value, EncodingSelector(HeaderNames.ContentLength));
                        {extraIndent}}}
                        {extraIndent}return;";
                }
                else
                {
                    return $@"
                        {extraIndent}flag = {header.FlagBit()};
                        {extraIndent}values = ref _headers._{header.Identifier};
                        {extraIndent}nameStr = HeaderNames.{header.Identifier};";
                }
            }

            var groups = values.GroupBy(header => header.EqualIgnoreCaseBytesFirstTerm());
            return start + $@"{Each(groups,  (byFirstTerm, i) => $@"{(byFirstTerm.Count() == 1 ? $@"{Each(byFirstTerm, header => $@"
                    {(i > 0 ? "else " : "")}if ({header.EqualIgnoreCaseBytes(firstTermVar)})
                    {{{GenerateIfBody(header)}
                    }}")}" : $@"
                    if ({byFirstTerm.Key.Replace(firstTermVarExpression, firstTermVar)})
                    {{{Each(byFirstTerm, (header, i) => $@"
                        {(i > 0 ? "else " : "")}if ({header.EqualIgnoreCaseBytesSecondTermOnwards()})
                        {{{GenerateIfBody(header, extraIndent: "    ")}
                        }}")}
                    }}")}")}";
        }

        public class KnownHeader
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public string Identifier => ResolveIdentifier(Name);

            public byte[] Bytes => Encoding.ASCII.GetBytes($"\r\n{Name}: ");
            public int BytesOffset { get; set; }
            public int BytesCount { get; set; }
            public bool ExistenceCheck { get; set; }
            public bool FastCount { get; set; }
            public bool EnhancedSetter { get; set; }
            public bool PrimaryHeader { get; set; }
            public string FlagBit() => $"{"0x" + (1L << Index).ToString("x")}L";
            public string TestBit() => $"(_bits & {"0x" + (1L << Index).ToString("x")}L) != 0";
            public string TestTempBit() => $"(tempBits & {"0x" + (1L << Index).ToString("x")}L) != 0";
            public string TestNotTempBit() => $"(tempBits & ~{"0x" + (1L << Index).ToString("x")}L) == 0";
            public string TestNotBit() => $"(_bits & {"0x" + (1L << Index).ToString("x")}L) == 0";
            public string SetBit() => $"_bits |= {"0x" + (1L << Index).ToString("x")}L";
            public string ClearBit() => $"_bits &= ~{"0x" + (1L << Index).ToString("x")}L";

            private string ResolveIdentifier(string name)
            {
                var identifer = name.Replace("-", "");

                // Pseudo headers start with a colon. A colon isn't valid in C# names so
                // remove it and pascal case the header name. e.g. :path -> Path, :scheme -> Scheme.
                // This identifier will match the names in HeadersNames.cs
                if (identifer.StartsWith(':'))
                {
                    identifer = char.ToUpper(identifer[1]) + identifer.Substring(2);
                }

                return identifer;
            }

            private void GetMaskAndComp(string name, int offset, int count, out ulong mask, out ulong comp)
            {
                mask = 0;
                comp = 0;
                for (var scan = 0; scan < count; scan++)
                {
                    var ch = (byte)name[offset + count - scan - 1];
                    var isAlpha = (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
                    comp = (comp << 8) + (ch & (isAlpha ? 0xdfu : 0xffu));
                    mask = (mask << 8) + (isAlpha ? 0xdfu : 0xffu);
                }
            }

            private string NameTerm(string name, int offset, int count, string type, string suffix)
            {
                GetMaskAndComp(name, offset, count, out var mask, out var comp);

                if (offset == 0)
                {
                    if (type == "byte")
                    {
                        return $"(nameStart & 0x{mask:x}{suffix})";
                    }
                    else
                    {
                        return $"(Unsafe.ReadUnaligned<{type}>(ref nameStart) & 0x{mask:x}{suffix})";
                    }
                }
                else
                {
                    if (type == "byte")
                    {
                        return $"(Unsafe.AddByteOffset(ref nameStart, (IntPtr){offset / count}) & 0x{mask:x}{suffix})";
                    }
                    else if ((offset / count) == 1)
                    {
                        return $"(Unsafe.ReadUnaligned<{type}>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)sizeof({type}))) & 0x{mask:x}{suffix})";
                    }
                    else
                    {
                        return $"(Unsafe.ReadUnaligned<{type}>(ref Unsafe.AddByteOffset(ref nameStart, (IntPtr)({offset / count} * sizeof({type})))) & 0x{mask:x}{suffix})";
                    }
                }

            }

            private string EqualityTerm(string name, int offset, int count, string type, string suffix)
            {
                GetMaskAndComp(name, offset, count, out var mask, out var comp);

                return $"0x{comp:x}{suffix}";
            }

            private string Term(string name, int offset, int count, string type, string suffix)
            {
                GetMaskAndComp(name, offset, count, out var mask, out var comp);

                return $"({NameTerm(name, offset, count, type, suffix)} == {EqualityTerm(name, offset, count, type, suffix)})";
            }

            public string FirstNameIgnoreCaseSegment()
            {
                var result = "";
                if (Name.Length >= 8)
                {
                    result = NameTerm(Name, 0, 8, "ulong", "uL");
                }
                else if (Name.Length >= 4)
                {
                    result = NameTerm(Name, 0, 4, "uint", "u");
                }
                else if (Name.Length >= 2)
                {
                    result = NameTerm(Name, 0, 2, "ushort", "u");
                }
                else
                {
                    result = NameTerm(Name, 0, 1, "byte", "u");
                }

                return result;
            }

            public string EqualIgnoreCaseBytes(string firstTermVar = "")
            {
                if (!string.IsNullOrEmpty(firstTermVar))
                {
                    return EqualIgnoreCaseBytesWithVar(firstTermVar);
                }

                var result = "";
                var delim = "";
                var index = 0;
                while (index != Name.Length)
                {
                    if (Name.Length - index >= 8)
                    {
                        result += delim + Term(Name, index, 8, "ulong", "uL");
                        index += 8;
                    }
                    else if (Name.Length - index >= 4)
                    {
                        result += delim + Term(Name, index, 4, "uint", "u");
                        index += 4;
                    }
                    else if (Name.Length - index >= 2)
                    {
                        result += delim + Term(Name, index, 2, "ushort", "u");
                        index += 2;
                    }
                    else
                    {
                        result += delim + Term(Name, index, 1, "byte", "u");
                        index += 1;
                    }
                    delim = " && ";
                }
                return result;

                string EqualIgnoreCaseBytesWithVar(string firstTermVar)
                {
                    var result = "";
                    var delim = " && ";
                    var index = 0;
                    var isFirst = true;
                    while (index != Name.Length)
                    {
                        if (Name.Length - index >= 8)
                        {
                            if (isFirst)
                            {
                                result = $"({firstTermVar} == {EqualityTerm(Name, index, 8, "ulong", "uL")})";
                            }
                            else
                            {
                                result += delim + Term(Name, index, 8, "ulong", "uL");
                            }

                            index += 8;
                        }
                        else if (Name.Length - index >= 4)
                        {
                            if (isFirst)
                            {
                                result = $"({firstTermVar} == {EqualityTerm(Name, index, 4, "uint", "u")})";
                            }
                            else
                            {
                                result += delim + Term(Name, index, 4, "uint", "u");
                            }
                            index += 4;
                        }
                        else if (Name.Length - index >= 2)
                        {
                            if (isFirst)
                            {
                                result = $"({firstTermVar} == {EqualityTerm(Name, index, 2, "ushort", "u")})";
                            }
                            else
                            {
                                result += delim + Term(Name, index, 2, "ushort", "u");
                            }
                            index += 2;
                        }
                        else
                        {
                            if (isFirst)
                            {
                                result = $"({firstTermVar} == {EqualityTerm(Name, index, 1, "byte", "u")})";
                            }
                            else
                            {
                                result += delim + Term(Name, index, 1, "byte", "u");
                            }
                            index += 1;
                        }

                        isFirst = false;
                    }
                    return result;
                }
            }

            public string EqualIgnoreCaseBytesFirstTerm()
            {
                var result = "";
                if (Name.Length >= 8)
                {
                    result = Term(Name, 0, 8, "ulong", "uL");
                }
                else if (Name.Length >= 4)
                {
                    result = Term(Name, 0, 4, "uint", "u");
                }
                else if (Name.Length >= 2)
                {
                    result = Term(Name, 0, 2, "ushort", "u");
                }
                else
                {
                    result = Term(Name, 0, 1, "byte", "u");
                }

                return result;
            }

            public string EqualIgnoreCaseBytesSecondTermOnwards()
            {
                var result = "";
                var delim = "";
                var index = 0;
                var isFirst = true;
                while (index != Name.Length)
                {
                    if (Name.Length - index >= 8)
                    {
                        if (!isFirst)
                        {
                            result += delim + Term(Name, index, 8, "ulong", "uL");
                        }

                        index += 8;
                    }
                    else if (Name.Length - index >= 4)
                    {
                        if (!isFirst)
                        {
                            result += delim + Term(Name, index, 4, "uint", "u");
                        }
                        index += 4;
                    }
                    else if (Name.Length - index >= 2)
                    {
                        if (!isFirst)
                        {
                            result += delim + Term(Name, index, 2, "ushort", "u");
                        }
                        index += 2;
                    }
                    else
                    {
                        if (!isFirst)
                        {
                            result += delim + Term(Name, index, 1, "byte", "u");
                        }
                        index += 1;
                    }

                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        delim = " && ";
                    }
                }
                return result;
            }
        }

        public static string GeneratedFile()
        {

            var requestHeaders = RequestHeaders;
            Debug.Assert(requestHeaders.Length <= 64);
            Debug.Assert(requestHeaders.Max(x => x.Index) <= 62);

            // 63 for responseHeaders as it steals one bit for Content-Length in CopyTo(ref MemoryPoolIterator output)
            var responseHeaders = ResponseHeaders;
            Debug.Assert(responseHeaders.Length <= 63);
            Debug.Assert(responseHeaders.Count(x => x.Index == 63) == 1);

            var responseTrailers = ResponseTrailers;

            var allHeaderNames = RequestHeaders.Concat(ResponseHeaders).Concat(ResponseTrailers)
                .Select(h => h.Identifier).Distinct().OrderBy(n => n).ToArray();

            var loops = new[]
            {
                new
                {
                    Headers = requestHeaders,
                    HeadersByLength = requestHeaders.OrderBy(x => x.Name.Length).GroupBy(x => x.Name.Length),
                    ClassName = "HttpRequestHeaders",
                    Bytes = default(byte[])
                },
                new
                {
                    Headers = responseHeaders,
                    HeadersByLength = responseHeaders.OrderBy(x => x.Name.Length).GroupBy(x => x.Name.Length),
                    ClassName = "HttpResponseHeaders",
                    Bytes = responseHeaders.SelectMany(header => header.Bytes).ToArray()
                },
                new
                {
                    Headers = responseTrailers,
                    HeadersByLength = responseTrailers.OrderBy(x => x.Name.Length).GroupBy(x => x.Name.Length),
                    ClassName = "HttpResponseTrailers",
                    Bytes = responseTrailers.SelectMany(header => header.Bytes).ToArray()
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
using System.Buffers;
using System.IO.Pipelines;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{{
    internal enum KnownHeaderType
    {{
        Unknown,{Each(allHeaderNames, n => @"
        " + n + ",")}
    }}
{Each(loops, loop => $@"
    internal partial class {loop.ClassName}
    {{{(loop.Bytes != null ?
        $@"
        private static ReadOnlySpan<byte> HeaderBytes => new byte[]
        {{
            {Each(loop.Bytes, b => $"{b},")}
        }};"
        : "")}
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
                StringValues value = default;
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
                StringValues value = default;
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
        public void SetRaw{header.Identifier}(StringValues value, byte[] raw)
        {{
            {header.SetBit()};
            _headers._{header.Identifier} = value;
            _headers._raw{header.Identifier} = raw;
        }}")}
        protected override int GetCountFast()
        {{
            return (_contentLength.HasValue ? 1 : 0 ) + BitOperations.PopCount((ulong)_bits) + (MaybeUnknown?.Count ?? 0);
        }}

        protected override bool TryGetValueFast(string key, out StringValues value)
        {{
            value = default;
            switch (key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                {{{Each(byLength.OrderBy(h => !h.PrimaryHeader), header => $@"
                    if (ReferenceEquals(HeaderNames.{header.Identifier}, key))
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
{Each(byLength.OrderBy(h => !h.PrimaryHeader), header => $@"
                    if (HeaderNames.{header.Identifier}.Equals(key, StringComparison.OrdinalIgnoreCase))
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
                    break;
                }}")}
            }}

            return TryGetUnknown(key, ref value);
        }}

        protected override void SetValueFast(string key, StringValues value)
        {{{(loop.ClassName != "HttpRequestHeaders" ? @"
            ValidateHeaderValueCharacters(value);" : "")}
            switch (key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                {{{Each(byLength.OrderBy(h => !h.PrimaryHeader), header => $@"
                    if (ReferenceEquals(HeaderNames.{header.Identifier}, key))
                    {{{(header.Identifier == "ContentLength" ? $@"
                        _contentLength = ParseContentLength(value.ToString());" : $@"
                        {header.SetBit()};
                        _headers._{header.Identifier} = value;{(header.EnhancedSetter == false ? "" : $@"
                        _headers._raw{header.Identifier} = null;")}")}
                        return;
                    }}")}
{Each(byLength.OrderBy(h => !h.PrimaryHeader), header => $@"
                    if (HeaderNames.{header.Identifier}.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {{{(header.Identifier == "ContentLength" ? $@"
                        _contentLength = ParseContentLength(value.ToString());" : $@"
                        {header.SetBit()};
                        _headers._{header.Identifier} = value;{(header.EnhancedSetter == false ? "" : $@"
                        _headers._raw{header.Identifier} = null;")}")}
                        return;
                    }}")}
                    break;
                }}")}
            }}

            SetValueUnknown(key, value);
        }}

        protected override bool AddValueFast(string key, StringValues value)
        {{{(loop.ClassName != "HttpRequestHeaders" ? @"
            ValidateHeaderValueCharacters(value);" : "")}
            switch (key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                {{{Each(byLength.OrderBy(h => !h.PrimaryHeader), header => $@"
                    if (ReferenceEquals(HeaderNames.{header.Identifier}, key))
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
    {Each(byLength.OrderBy(h => !h.PrimaryHeader), header => $@"
                    if (HeaderNames.{header.Identifier}.Equals(key, StringComparison.OrdinalIgnoreCase))
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
                    break;
                }}")}
            }}

            return AddValueUnknown(key, value);
        }}

        protected override bool RemoveFast(string key)
        {{
            switch (key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                {{{Each(byLength.OrderBy(h => !h.PrimaryHeader), header => $@"
                    if (ReferenceEquals(HeaderNames.{header.Identifier}, key))
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
    {Each(byLength.OrderBy(h => !h.PrimaryHeader), header => $@"
                    if (HeaderNames.{header.Identifier}.Equals(key, StringComparison.OrdinalIgnoreCase))
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
                    break;
                }}")}
            }}

            return RemoveUnknown(key);
        }}
{(loop.ClassName != "HttpRequestHeaders" ?
 $@"        protected override void ClearFast()
        {{
            MaybeUnknown?.Clear();
            _contentLength = null;
            var tempBits = _bits;
            _bits = 0;
            if(BitOperations.PopCount((ulong)tempBits) > 12)
            {{
                _headers = default(HeaderReferences);
                return;
            }}
            {Each(loop.Headers.Where(header => header.Identifier != "ContentLength").OrderBy(h => !h.PrimaryHeader), header => $@"
            if ({header.TestTempBit()})
            {{
                _headers._{header.Identifier} = default;
                if({header.TestNotTempBit()})
                {{
                    return;
                }}
                tempBits &= ~{"0x" + (1L << header.Index).ToString("x")}L;
            }}
            ")}
        }}
" :
$@"        private void Clear(long bitsToClear)
        {{
            var tempBits = bitsToClear;
            {Each(loop.Headers.Where(header => header.Identifier != "ContentLength").OrderBy(h => !h.PrimaryHeader), header => $@"
            if ({header.TestTempBit()})
            {{
                _headers._{header.Identifier} = default;
                if({header.TestNotTempBit()})
                {{
                    return;
                }}
                tempBits &= ~{"0x" + (1L << header.Index).ToString("x")}L;
            }}
            ")}
        }}
")}
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
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.{header.Identifier}, _headers._{header.Identifier});
                    ++arrayIndex;
                }}")}
                if (_contentLength.HasValue)
                {{
                    if (arrayIndex == array.Length)
                    {{
                        return false;
                    }}
                    array[arrayIndex] = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_contentLength.Value));
                    ++arrayIndex;
                }}
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);

            return true;
        }}
        {(loop.ClassName == "HttpResponseHeaders" ? $@"
        internal unsafe void CopyToFast(ref BufferWriter<PipeWriter> output)
        {{
            var tempBits = (ulong)_bits | (_contentLength.HasValue ? {"0x" + (1L << 63).ToString("x")}L : 0);
            var next = 0;
            var keyStart = 0;
            var keyLength = 0;
            ref readonly StringValues values = ref Unsafe.AsRef<StringValues>(null);

            do
            {{
                switch (next)
                {{{Each(loop.Headers.OrderBy(h => !h.PrimaryHeader).Select((h, i) => (Header: h, Index: i)), hi => $@"
                    case {hi.Index}: // Header: ""{hi.Header.Name}""
                        if ({hi.Header.TestTempBit()})
                        {{
                            tempBits ^= {"0x" + (1L << hi.Header.Index).ToString("x")}L;{(hi.Header.Identifier != "ContentLength" ? $@"{(hi.Header.EnhancedSetter == false ? $@"
                            values = ref _headers._{hi.Header.Identifier};
                            keyStart = {hi.Header.BytesOffset};
                            keyLength = {hi.Header.BytesCount};
                            next = {hi.Index + 1};
                            break; // OutputHeader" : $@"
                            if (_headers._raw{hi.Header.Identifier} != null)
                            {{
                                output.Write(_headers._raw{hi.Header.Identifier});
                            }}
                            else
                            {{
                                values = ref _headers._{hi.Header.Identifier};
                                keyStart = {hi.Header.BytesOffset};
                                keyLength = {hi.Header.BytesCount};
                                next = {hi.Index + 1};
                                break; // OutputHeader
                            }}")}" : $@"
                            output.Write(HeaderBytes.Slice({hi.Header.BytesOffset}, {hi.Header.BytesCount}));
                            output.WriteNumeric((ulong)ContentLength.Value);
                            if (tempBits == 0)
                            {{
                                return;
                            }}")}
                        }}
                        {(hi.Index + 1 < loop.Headers.Count() ? $"goto case {hi.Index + 1};" : "return;")}")}
                    default:
                        return;
                }}

                // OutputHeader
                {{
                    var valueCount = values.Count;
                    var headerKey = HeaderBytes.Slice(keyStart, keyLength);
                    for (var i = 0; i < valueCount; i++)
                    {{
                        var value = values[i];
                        if (value != null)
                        {{
                            output.Write(headerKey);
                            output.WriteAscii(value);
                        }}
                    }}
                }}
            }} while (tempBits != 0);
        }}" : "")}{(loop.ClassName == "HttpRequestHeaders" ? $@"
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void Append(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {{
            ref byte nameStart = ref MemoryMarshal.GetReference(name);
            var nameStr = string.Empty;
            ref StringValues values = ref Unsafe.AsRef<StringValues>(null);
            var flag = 0L;

            // Does the name matched any ""known"" headers
            {AppendSwitch(loop.Headers.GroupBy(x => x.Name.Length).OrderBy(x => x.Key))}

            if (flag != 0)
            {{
                // Matched a known header
                if ((_previousBits & flag) != 0)
                {{
                    // Had a previous string for this header, mark it as used so we don't clear it OnHeadersComplete or consider it if we get a second header
                    _previousBits ^= flag;

                    // We will only reuse this header if there was only one previous header
                    if (values.Count == 1)
                    {{
                        var previousValue = values.ToString();
                        // Check lengths are the same, then if the bytes were converted to an ascii string if they would be the same.
                        // We do not consider Utf8 headers for reuse.
                        if (previousValue.Length == value.Length &&
                            StringUtilities.BytesOrdinalEqualsStringAndAscii(previousValue, value))
                        {{
                            // The previous string matches what the bytes would convert to, so we will just use that one.
                            _bits |= flag;
                            return;
                        }}
                    }}
                }}

                // We didn't have a previous matching header value, or have already added a header, so get the string for this value.
                var valueStr = value.GetRequestHeaderString(nameStr, EncodingSelector);
                if ((_bits & flag) == 0)
                {{
                    // We didn't already have a header set, so add a new one.
                    _bits |= flag;
                    values = new StringValues(valueStr);
                }}
                else
                {{
                    // We already had a header set, so concatenate the new one.
                    values = AppendValue(values, valueStr);
                }}
            }}
            else
            {{
                // The header was not one of the ""known"" headers.
                // Convert value to string first, because passing two spans causes 8 bytes stack zeroing in 
                // this method with rep stosd, which is slower than necessary.
                nameStr = name.GetHeaderName();
                var valueStr = value.GetRequestHeaderString(nameStr, EncodingSelector);
                AppendUnknownHeaders(nameStr, valueStr);
            }}
        }}" : "")}

        private struct HeaderReferences
        {{{Each(loop.Headers.Where(header => header.Identifier != "ContentLength"), header => @"
            public StringValues _" + header.Identifier + ";")}
            {Each(loop.Headers.Where(header => header.EnhancedSetter), header => @"
            public byte[] _raw" + header.Identifier + ";")}
        }}

        public partial struct Enumerator
        {{
            // Compiled to Jump table
            public bool MoveNext()
            {{
                switch (_next)
                {{{Each(loop.Headers.Where(header => header.Identifier != "ContentLength"), header => $@"
                    case {header.Index}:
                        goto Header{header.Identifier};")}
                    {(!loop.ClassName.Contains("Trailers") ? $@"case {loop.Headers.Count() - 1}:
                        goto HeaderContentLength;" : "")}
                    default:
                        goto ExtraHeaders;
                }}
                {Each(loop.Headers.Where(header => header.Identifier != "ContentLength"), header => $@"
                Header{header.Identifier}: // case {header.Index}
                    if ({header.TestBit()})
                    {{
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.{header.Identifier}, _collection._headers._{header.Identifier});
                        _currentKnownType = KnownHeaderType.{header.Identifier};
                        _next = {header.Index + 1};
                        return true;
                    }}")}
                {(!loop.ClassName.Contains("Trailers") ? $@"HeaderContentLength: // case {loop.Headers.Count() - 1}
                    if (_collection._contentLength.HasValue)
                    {{
                        _current = new KeyValuePair<string, StringValues>(HeaderNames.ContentLength, HeaderUtilities.FormatNonNegativeInt64(_collection._contentLength.Value));
                        _currentKnownType = KnownHeaderType.ContentLength;
                        _next = {loop.Headers.Count()};
                        return true;
                    }}" : "")}
                ExtraHeaders:
                    if (!_hasUnknown || !_unknownEnumerator.MoveNext())
                    {{
                        _current = default(KeyValuePair<string, StringValues>);
                        _currentKnownType = default;
                        return false;
                    }}
                    _current = _unknownEnumerator.Current;
                    _currentKnownType = KnownHeaderType.Unknown;
                    return true;
            }}
        }}
    }}
")}}}";
        }
    }
}
