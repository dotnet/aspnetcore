using Microsoft.Framework.Runtime.Roslyn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.GeneratedCode
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class KnownHeaders : ICompileModule
    {
        static string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
        {
            return values.Select(formatter).Aggregate((a, b) => a + b);
        }

        class KnownHeader
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public string Identifier => Name.Replace("-", "");
            public string TestBit() => $"((_bits & {1L << Index}L) != 0)";
            public string SetBit() => $"_bits |= {1L << Index}L";
            public string ClearBit() => $"_bits &= ~{1L << Index}L";
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
            });

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
                Index = index
            });

            var loops = new[]
            {
                new
                {
                    Headers = requestHeaders,
                    HeadersByLength = requestHeaders.GroupBy(x => x.Name.Length),
                    ClassName = "FrameRequestHeaders"
                },
                new
                {
                    Headers = responseHeaders,
                    HeadersByLength = responseHeaders.GroupBy(x => x.Name.Length),
                    ClassName = "FrameResponseHeaders"
                }
            };

            return $@"
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Server.Kestrel.Http 
{{{Each(loops, loop => $@"
    public partial class {loop.ClassName} 
    {{
        long _bits = 0;
        {Each(loop.Headers, header => @"
        string[] _" + header.Identifier + ";")}

        protected override int GetCountFast()
        {{
            var count = MaybeUnknown?.Count ?? 0;
            {Each(loop.Headers, header => $@"
                if ({header.TestBit()}) 
                {{
                    ++count;
                }}
            ")}
            return count;
        }}

        protected override string[] GetValueFast(string key)
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

        protected override bool TryGetValueFast(string key, out string[] value)
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
                                value = null;
                                return false;
                            }}
                        }}
                    ")}}}
                    break;
            ")}}}
            value = null;
            return MaybeUnknown?.TryGetValue(key, out value) ?? false;
        }}

        protected override void SetValueFast(string key, string[] value)
        {{
            switch(key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {{
                            {header.SetBit()};
                            _{header.Identifier} = value;
                            return;
                        }}
                    ")}}}
                    break;
            ")}}}
            Unknown[key] = value;
        }}

        protected override void AddValueFast(string key, string[] value)
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
                            _{header.Identifier} = value;
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
            MaybeUnknown?.Clear();
        }}
        
        protected override void CopyToFast(KeyValuePair<string, string[]>[] array, int arrayIndex)
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

                    array[arrayIndex] = new KeyValuePair<string, string[]>(""{header.Name}"", _{header.Identifier});
                    ++arrayIndex;
                }}
            ")}
            ((ICollection<KeyValuePair<string, string[]>>)MaybeUnknown)?.CopyTo(array, arrayIndex);
        }}

        public void Append(string key, string value)
        {{
            switch(key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (""{header.Name}"".Equals(key, StringComparison.OrdinalIgnoreCase)) 
                        {{
                            if ({header.TestBit()})
                            {{
                                _{header.Identifier} = AppendValue(_{header.Identifier}, value);
                            }}
                            else
                            {{
                                {header.SetBit()};
                                _{header.Identifier} = new[] {{value}};
                            }}
                            return;
                        }}
                    ")}}}
                    break;
            ")}}}
            string[] existing;
            Unknown[key] = Unknown.TryGetValue(key, out existing) ? AppendValue(existing, value) : new[] {{value}};
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
                        _current = new KeyValuePair<string, string[]>(""{header.Name}"", _collection._{header.Identifier});
                        _state = {header.Index + 1};
                        return true;
                    }}
                ")}
                state_default:
                    if (!_hasUnknown || !_unknownEnumerator.MoveNext())
                    {{
                        _current = default(KeyValuePair<string, string[]>);
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
