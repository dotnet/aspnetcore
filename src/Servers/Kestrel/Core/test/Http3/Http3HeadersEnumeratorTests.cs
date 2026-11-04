// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.HPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.Extensions.Primitives;

using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http3HeadersEnumeratorTests
{
    [Fact]
    public void CanIterateOverResponseHeaders()
    {
        var responseHeaders = (IHeaderDictionary)new HttpResponseHeaders();

        responseHeaders.ContentLength = 9;
        responseHeaders.AcceptRanges = "AcceptRanges!";
        responseHeaders.Age = new StringValues(new[] { "1", "2" });
        responseHeaders.Date = "Date!";
        responseHeaders.GrpcEncoding = "Identity!";

        responseHeaders.Append("Name1", "Value1");
        responseHeaders.Append("Name2", "Value2-1");
        responseHeaders.Append("Name2", "Value2-2");
        responseHeaders.Append("Name3", "Value3");

        var e = new Http3HeadersEnumerator();
        e.Initialize(responseHeaders);

        var headers = GetNormalizedHeaders(e);

        Assert.Equal(new[]
        {
            CreateHeaderResult(6, "Date", "Date!"),
            CreateHeaderResult(32, "Accept-Ranges", "AcceptRanges!"),
            CreateHeaderResult(2, "Age", "1"),
            CreateHeaderResult(2, "Age", "2"),
            CreateHeaderResult(-1, "Grpc-Encoding", "Identity!"),
            CreateHeaderResult(4, "Content-Length", "9"),
            CreateHeaderResult(-1, "Name1", "Value1"),
            CreateHeaderResult(-1, "Name2", "Value2-1"),
            CreateHeaderResult(-1, "Name2", "Value2-2"),
            CreateHeaderResult(-1, "Name3", "Value3"),
        }, headers);
    }

    [Fact]
    public void CanIterateOverResponseTrailers()
    {
        var responseTrailers = (IHeaderDictionary)new HttpResponseTrailers();

        responseTrailers.ContentLength = 9;
        responseTrailers.ETag = "ETag!";

        responseTrailers.Append("Name1", "Value1");
        responseTrailers.Append("Name2", "Value2-1");
        responseTrailers.Append("Name2", "Value2-2");
        responseTrailers.Append("Name3", "Value3");

        var e = new Http3HeadersEnumerator();
        e.Initialize(responseTrailers);

        var headers = GetNormalizedHeaders(e);

        Assert.Equal(new[]
        {
            CreateHeaderResult(7, "ETag", "ETag!"),
            CreateHeaderResult(-1, "Name1", "Value1"),
            CreateHeaderResult(-1, "Name2", "Value2-1"),
            CreateHeaderResult(-1, "Name2", "Value2-2"),
            CreateHeaderResult(-1, "Name3", "Value3"),
        }, headers);
    }

    [Fact]
    public void Initialize_ChangeHeadersSource_EnumeratorUsesNewSource()
    {
        var responseHeaders = new HttpResponseHeaders();
        responseHeaders.Append("Name1", "Value1");
        responseHeaders.Append("Name2", "Value2-1");
        responseHeaders.Append("Name2", "Value2-2");

        var e = new Http3HeadersEnumerator();
        e.Initialize(responseHeaders);

        Assert.True(e.MoveNext());
        Assert.Equal("Name1", e.Current.Key);
        Assert.Equal("Value1", e.Current.Value);
        var (index, matchedValue) = e.GetQPackStaticTableId();
        Assert.Equal(-1, index);

        Assert.True(e.MoveNext());
        Assert.Equal("Name2", e.Current.Key);
        Assert.Equal("Value2-1", e.Current.Value);
        (index, matchedValue) = e.GetQPackStaticTableId();
        Assert.Equal(-1, index);

        Assert.True(e.MoveNext());
        Assert.Equal("Name2", e.Current.Key);
        Assert.Equal("Value2-2", e.Current.Value);
        (index, matchedValue) = e.GetQPackStaticTableId();
        Assert.Equal(-1, index);

        var responseTrailers = (IHeaderDictionary)new HttpResponseTrailers();

        responseTrailers.GrpcStatus = "1";

        responseTrailers.Append("Name1", "Value1");
        responseTrailers.Append("Name2", "Value2-1");
        responseTrailers.Append("Name2", "Value2-2");

        e.Initialize(responseTrailers);

        Assert.True(e.MoveNext());
        Assert.Equal("Grpc-Status", e.Current.Key);
        Assert.Equal("1", e.Current.Value);
        (index, matchedValue) = e.GetQPackStaticTableId();
        Assert.Equal(-1, index);

        Assert.True(e.MoveNext());
        Assert.Equal("Name1", e.Current.Key);
        Assert.Equal("Value1", e.Current.Value);
        (index, matchedValue) = e.GetQPackStaticTableId();
        Assert.Equal(-1, index);

        Assert.True(e.MoveNext());
        Assert.Equal("Name2", e.Current.Key);
        Assert.Equal("Value2-1", e.Current.Value);
        (index, matchedValue) = e.GetQPackStaticTableId();
        Assert.Equal(-1, index);

        Assert.True(e.MoveNext());
        Assert.Equal("Name2", e.Current.Key);
        Assert.Equal("Value2-2", e.Current.Value);
        (index, matchedValue) = e.GetQPackStaticTableId();
        Assert.Equal(-1, index);

        Assert.False(e.MoveNext());
    }

    private (int QPackStaticTableId, string Name, string Value)[] GetNormalizedHeaders(Http3HeadersEnumerator enumerator)
    {
        var headers = new List<(int HPackStaticTableId, string Name, string Value)>();
        while (enumerator.MoveNext())
        {
            headers.Add(CreateHeaderResult(enumerator.GetQPackStaticTableId().index, enumerator.Current.Key, enumerator.Current.Value));
        }
        return headers.ToArray();
    }

    private static (int QPackStaticTableId, string Key, string Value) CreateHeaderResult(int hPackStaticTableId, string key, string value)
    {
        return (hPackStaticTableId, key, value);
    }
}
