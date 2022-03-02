// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.Extensions.Primitives;
using Http2HeadersEnumerator = Microsoft.AspNetCore.Server.Kestrel.Core.Tests.Http2HeadersEnumerator;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class Http2HeadersEnumeratorBenchmark
{
    private Http2HeadersEnumerator _enumerator;
    private IHeaderDictionary _knownSingleValueResponseHeaders;
    private IHeaderDictionary _knownMultipleValueResponseHeaders;
    private IHeaderDictionary _unknownSingleValueResponseHeaders;
    private IHeaderDictionary _unknownMultipleValueResponseHeaders;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _knownSingleValueResponseHeaders = new HttpResponseHeaders();

        _knownSingleValueResponseHeaders.Server = "Value";
        _knownSingleValueResponseHeaders.Date = "Value";
        _knownSingleValueResponseHeaders.ContentType = "Value";
        _knownSingleValueResponseHeaders.SetCookie = "Value";

        _knownMultipleValueResponseHeaders = new HttpResponseHeaders();

        _knownMultipleValueResponseHeaders.Server = new StringValues(new[] { "One", "Two" });
        _knownMultipleValueResponseHeaders.Date = new StringValues(new[] { "One", "Two" });
        _knownMultipleValueResponseHeaders.ContentType = new StringValues(new[] { "One", "Two" });
        _knownMultipleValueResponseHeaders.SetCookie = new StringValues(new[] { "One", "Two" });

        _unknownSingleValueResponseHeaders = new HttpResponseHeaders();
        _unknownSingleValueResponseHeaders.Append("One", "Value");
        _unknownSingleValueResponseHeaders.Append("Two", "Value");
        _unknownSingleValueResponseHeaders.Append("Three", "Value");
        _unknownSingleValueResponseHeaders.Append("Four", "Value");

        _unknownMultipleValueResponseHeaders = new HttpResponseHeaders();
        _unknownMultipleValueResponseHeaders.Append("One", new StringValues(new[] { "One", "Two" }));
        _unknownMultipleValueResponseHeaders.Append("Two", new StringValues(new[] { "One", "Two" }));
        _unknownMultipleValueResponseHeaders.Append("Three", new StringValues(new[] { "One", "Two" }));
        _unknownMultipleValueResponseHeaders.Append("Four", new StringValues(new[] { "One", "Two" }));

        _enumerator = new Http2HeadersEnumerator();
    }

    [Benchmark]
    public void KnownSingleValueResponseHeaders()
    {
        _enumerator.Initialize(_knownSingleValueResponseHeaders);

        if (_enumerator.MoveNext())
        {
        }
    }

    [Benchmark]
    public void KnownMultipleValueResponseHeaders()
    {
        _enumerator.Initialize(_knownMultipleValueResponseHeaders);

        if (_enumerator.MoveNext())
        {
        }
    }

    [Benchmark]
    public void UnknownSingleValueResponseHeaders()
    {
        _enumerator.Initialize(_unknownSingleValueResponseHeaders);

        if (_enumerator.MoveNext())
        {
        }
    }

    [Benchmark]
    public void UnknownMultipleValueResponseHeaders()
    {
        _enumerator.Initialize(_unknownMultipleValueResponseHeaders);

        if (_enumerator.MoveNext())
        {
        }
    }
}
