using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dnx.Compilation.CSharp;
using System.Text;

namespace Microsoft.AspNet.Server.Kestrel.GeneratedCode
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class KnownHeaders : ICompileModule
    {
        static string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
        {
            return values.Any() ? values.Select(formatter).Aggregate((a, b) => a + b) : "";
        }

        class KnownHeader
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public string Identifier => Name.Replace("-", "");

            public byte[] Bytes => Encoding.ASCII.GetBytes($"\r\n{Name}: ");
            public int BytesOffset { get; set; }
            public int BytesCount { get; set; }
            public bool EnhancedSetter { get; set; }
            public string TestBit() => $"((_bits & {1L << Index}L) != 0)";
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
        public virtual void BeforeCompile(BeforeCompileContext context)
        {
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(GeneratedFile());
            context.Compilation = context.Compilation.AddSyntaxTrees(syntaxTree);
        }
        public static string GeneratedFile()
        {
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
                "Content-Length",
                "Content-Type",
                "Content-Encoding",
                "Content-Language",
                "Content-Location",
                "Content-MD5",
                "Content-Range",
                "Expires",
                "Last-Modified"
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
            }).Select((header, index) => new KnownHeader
            {
                Name = header,
                Index = index
            }).ToArray();
            var enhancedHeaders = new[]
            {
                "Connection",
                "Server",
                "Date",
                "Transfer-Encoding",
                "Content-Length",
            };
            var responseHeaders = commonHeaders.Concat(new[]
            {
                "Accept-Ranges",
                "Age",
                "ETag",
                "Location",
                "Proxy-Autheticate",
                "Retry-After",
                "Server",
                "Set-Cookie",
                "Vary",
                "WWW-Authenticate",
            }).Select((header, index) => new KnownHeader
            {
                Name = header,
                Index = index,
                EnhancedSetter = enhancedHeaders.Contains(header)
            }).ToArray();
            var loops = new[]
            {
                new
                {
                    Headers = requestHeaders,
                    HeadersByLength = requestHeaders.GroupBy(x => x.Name.Length),
                    ClassName = "FrameRequestHeaders",
                    Bytes = default(byte[])
                },
                new
                {
                    Headers = responseHeaders,
                    HeadersByLength = responseHeaders.GroupBy(x => x.Name.Length),
                    ClassName = "FrameResponseHeaders",
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
            return $@"
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Server.Kestrel.Http 
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
        {Each(loop.Headers, header => @"
        private StringValues _" + header.Identifier + ";")}
        {Each(loop.Headers.Where(header => header.EnhancedSetter), header => @"
        private byte[] _raw" + header.Identifier + ";")}
        {Each(loop.Headers, header => $@"
        public StringValues Header{header.Identifier}
        {{
            get
            {{
                return _{header.Identifier};
            }}
            set
            {{
                {header.SetBit()};
                _{header.Identifier} = value; {(header.EnhancedSetter == false ? "" : $@"
                _raw{header.Identifier} = null;")}
            }}
        }}")}
        {Each(loop.Headers.Where(header => header.EnhancedSetter), header => $@"
        public void SetRaw{header.Identifier}(StringValues value, byte[] raw)
        {{
            {header.SetBit()};
            _{header.Identifier} = value; 
            _raw{header.Identifier} = raw;
        }}")}
        protected override int GetCountFast()
        {{
            return BitCount(_bits) + (MaybeUnknown?.Count ?? 0);
        }}
        protected override StringValues GetValueFast(string key)
        {{
            switch(key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {{
                            if ({header.TestBit()})
                            {{
                                return _{header.Identifier};
                            }}
                            else
                            {{
                                throw new System.Collections.Generic.KeyNotFoundException();
                            }}
                        }}
                    ")}}}
                    break;
")}}}
            if (MaybeUnknown == null) 
            {{
                throw new System.Collections.Generic.KeyNotFoundException();
            }}
            return MaybeUnknown[key];
        }}
        protected override bool TryGetValueFast(string key, out StringValues value)
        {{
            switch(key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {{
                            if ({header.TestBit()})
                            {{
                                value = _{header.Identifier};
                                return true;
                            }}
                            else
                            {{
                                value = StringValues.Empty;
                                return false;
                            }}
                        }}
                    ")}}}
                    break;
")}}}
            value = StringValues.Empty;
            return MaybeUnknown?.TryGetValue(key, out value) ?? false;
        }}
        protected override void SetValueFast(string key, StringValues value)
        {{
            switch(key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {{
                            {header.SetBit()};
                            _{header.Identifier} = value;{(header.EnhancedSetter == false ? "" : $@"
                            _raw{header.Identifier} = null;")}
                            return;
                        }}
                    ")}}}
                    break;
")}}}
            Unknown[key] = value;
        }}
        protected override void AddValueFast(string key, StringValues value)
        {{
            switch(key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase))
                        {{
                            if ({header.TestBit()})
                            {{
                                throw new ArgumentException(""An item with the same key has already been added."");
                            }}
                            {header.SetBit()};
                            _{header.Identifier} = value;{(header.EnhancedSetter == false ? "" : $@"
                            _raw{header.Identifier} = null;")}
                            return;
                        }}
                    ")}}}
                    break;
            ")}}}
            Unknown.Add(key, value);
        }}
        protected override bool RemoveFast(string key)
        {{
            switch(key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {{
                            if ({header.TestBit()})
                            {{
                                {header.ClearBit()};
                                _{header.Identifier} = StringValues.Empty;{(header.EnhancedSetter == false ? "" : $@"
                                _raw{header.Identifier} = null;")}
                                return true;
                            }}
                            else
                            {{
                                return false;
                            }}
                        }}
                    ")}}}
                    break;
            ")}}}
            return MaybeUnknown?.Remove(key) ?? false;
        }}
        protected override void ClearFast()
        {{
            _bits = 0;
            {Each(loop.Headers, header => $@"
            _{header.Identifier} = StringValues.Empty;")}
            {Each(loop.Headers.Where(header => header.EnhancedSetter), header => $@"
            _raw{header.Identifier} = null;")}
            MaybeUnknown?.Clear();
        }}
        
        protected override void CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {{
            if (arrayIndex < 0)
            {{
                throw new ArgumentException();
            }}
            {Each(loop.Headers, header => $@"
                if ({header.TestBit()}) 
                {{
                    if (arrayIndex == array.Length)
                    {{
                        throw new ArgumentException();
                    }}

                    array[arrayIndex] = new KeyValuePair<string, StringValues>(""{header.Name}"", _{header.Identifier});
                    ++arrayIndex;
                }}
            ")}
            ((ICollection<KeyValuePair<string, StringValues>>)MaybeUnknown)?.CopyTo(array, arrayIndex);
        }}
        {(loop.ClassName == "FrameResponseHeaders" ? $@"
        protected void CopyToFast(ref MemoryPoolIterator2 output)
        {{
            {Each(loop.Headers, header => $@"
                if ({header.TestBit()}) 
                {{ {(header.EnhancedSetter == false ? "" : $@"
                    if (_raw{header.Identifier} != null) 
                    {{
                        output.CopyFrom(_raw{header.Identifier}, 0, _raw{header.Identifier}.Length);
                    }} else ")}
                    foreach(var value in _{header.Identifier})
                    {{
                        output.CopyFrom(_headerBytes, {header.BytesOffset}, {header.BytesCount});
                        output.CopyFromAscii(value);
                    }}
                }}
            ")}
        }}" : "")}
        public unsafe void Append(byte[] keyBytes, int keyOffset, int keyLength, string value)
        {{
            fixed(byte* ptr = keyBytes) {{ var pUB = ptr + keyOffset; var pUL = (ulong*)pUB; var pUI = (uint*)pUB; var pUS = (ushort*)pUB;
            switch(keyLength)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if ({header.EqualIgnoreCaseBytes()}) 
                        {{
                            if ({header.TestBit()})
                            {{
                                _{header.Identifier} = AppendValue(_{header.Identifier}, value);
                            }}
                            else
                            {{
                                {header.SetBit()};
                                _{header.Identifier} = new StringValues(value);{(header.EnhancedSetter == false ? "" : $@"
                                _raw{header.Identifier} = null;")}
                            }}
                            return;
                        }}
                    ")}}}
                    break;
            ")}}}}}
            var key = System.Text.Encoding.ASCII.GetString(keyBytes, keyOffset, keyLength);
            StringValues existing;
            Unknown.TryGetValue(key, out existing);
            Unknown[key] = AppendValue(existing, value);
        }}
        public partial struct Enumerator
        {{
            public bool MoveNext()
            {{
                switch (_state)
                {{
                    {Each(loop.Headers, header => $@"
                        case {header.Index}:
                            goto state{header.Index};
                    ")}
                    default:
                        goto state_default;
                }}
                {Each(loop.Headers, header => $@"
                state{header.Index}:
                    if ({header.TestBit()})
                    {{
                        _current = new KeyValuePair<string, StringValues>(""{header.Name}"", _collection._{header.Identifier});
                        _state = {header.Index + 1};
                        return true;
                    }}
                ")}
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
")}}}
";
        }
        public virtual void AfterCompile(AfterCompileContext context)
        {
        }
    }
}