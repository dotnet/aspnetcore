// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

[MemoryDiagnoser]
public class LazyRequestHeadersBenchmark
{
    private HttpRequestHeaders _headers;
    private int _iteration;

    [Params(false, true)]
    public bool Lazy { get; set; }

    [Params(false, true)]
    public bool Changing { get; set; }

    [Params("Ignore", "ReadOne", "ReadAll", "Enumerate", "DuplicateKnown", "DuplicateUnknown")]
    public string Access { get; set; }

    [Params(1)]
    public int Protocol { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyRequestHeaders", Lazy);
        _headers = new HttpRequestHeaders();
        Request();
    }

    [Benchmark]
    public int Request()
    {
        _headers.Reset();
        var value = Changing && (_iteration++ & 1) != 0 ? "changing-value-B"u8 : "changing-value-A"u8;
        Append("Accept"u8, value);
        Append("Accept-Language"u8, value);
        Append("User-Agent"u8, value);
        Append("X-Example"u8, value);
        Append("X-Request-Nonce"u8, value);
        if (Access == "DuplicateKnown")
        {
            Append("Accept"u8, "second"u8);
        }
        else if (Access == "DuplicateUnknown")
        {
            Append("X-Example"u8, "second"u8);
        }
        _headers.OnHeadersComplete();

        IHeaderDictionary dictionary = _headers;
        if (Access == "ReadOne")
        {
            return dictionary.UserAgent.ToString().Length;
        }
        if (Access is "ReadAll" or "DuplicateKnown" or "DuplicateUnknown")
        {
            return dictionary.Accept.ToString().Length + dictionary.AcceptLanguage.ToString().Length +
                dictionary.UserAgent.ToString().Length + dictionary["X-Example"].ToString().Length +
                dictionary["X-Request-Nonce"].ToString().Length;
        }
        if (Access == "Enumerate")
        {
            var length = 0;
            foreach (var header in _headers)
            {
                length += header.Value.ToString().Length;
            }
            return length;
        }
        return _headers.Count;
    }

    private void Append(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
#if LAZY_HEADERS_BASELINE
        _headers.Append(name, value, checkForNewlineChars: true);
#else
        _headers.Append(name, value, checkForNewlineChars: true, allowLazyValues: true);
#endif
    }
}
