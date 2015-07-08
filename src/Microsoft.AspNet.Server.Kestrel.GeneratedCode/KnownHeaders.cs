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
        string Each<T>(IEnumerable<T> values, Func<T, string> formatter)
        {
            return values.Select(formatter).Aggregate((a, b) => a + b + "\r\n");
        }

        class KnownHeader
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public string Identifier => Name.Replace("-", "");
            public string TestBit() => $"((_bits & ({1L << Index}L)) != 0)";
            public string SetBit() => $"_bits |= {1L << Index}L";
            public string ClearBit() => $"_bits &= ~{(1L << Index)}L";
        }

        public virtual void BeforeCompile(BeforeCompileContext context)
        {
            Console.WriteLine("I like pie");

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
                "Translage",
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

            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText($@"
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Server.Kestrel.Http 
{{{Each(loops, loop => $@"
    public partial class {loop.ClassName} 
    {{
        long _bits = 0;
        {Each(loop.Headers, header => "string[] _" + header.Identifier + ";")}

        protected override int GetCountFast()
        {{
            var count = Unknown.Count;
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
                        if (0 == StringComparer.OrdinalIgnoreCase.Compare(key, ""{header.Name}"")) 
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
            return Unknown[key];
        }}

        protected override bool TryGetValueFast(string key, out string[] value)
        {{
            switch(key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (0 == StringComparer.OrdinalIgnoreCase.Compare(key, ""{header.Name}"")) 
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
            return Unknown.TryGetValue(key, out value);
        }}

        protected override void SetValueFast(string key, string[] value)
        {{
            switch(key.Length)
            {{{Each(loop.HeadersByLength, byLength => $@"
                case {byLength.Key}:
                    {{{Each(byLength, header => $@"
                        if (0 == StringComparer.OrdinalIgnoreCase.Compare(key, ""{header.Name}"")) 
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
                        if (0 == StringComparer.OrdinalIgnoreCase.Compare(key, ""{header.Name}"")) 
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
                        if (0 == StringComparer.OrdinalIgnoreCase.Compare(key, ""{header.Name}"")) 
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
            return Unknown.Remove(key);
        }}

        protected override void ClearFast()
        {{
            _bits = 0;
            Unknown.Clear();
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
            ((ICollection<KeyValuePair<string, string[]>>)Unknown).CopyTo(array, arrayIndex);
        }}

        protected override IEnumerable<KeyValuePair<string, string[]>> EnumerateFast()
        {{
            {Each(loop.Headers, header => $@"
                if ({header.TestBit()}) 
                {{
                    yield return new KeyValuePair<string, string[]>(""{header.Name}"", _{header.Identifier});
                }}
            ")}
            foreach(var kv in Unknown)
            {{
                yield return kv;
            }}
        }}
    }}
")}}}
");

            context.Compilation = context.Compilation.AddSyntaxTrees(syntaxTree);
        }

        public virtual void AfterCompile(AfterCompileContext context)
        {
        }
    }
}
