// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests;

public class ServerSentEventsRequestDetectionTests
{
    public static TheoryData<string[], bool> AcceptHeaders => new()
    {
        { null, false },
        { new[] { "" }, false },
        { new[] { "*/*" }, false },
        { new[] { "application/json, text/plain, */*" }, false },
        { new[] { "text/*" }, false },
        { new[] { "text/event-stream" }, true },
        { new[] { "TEXT/Event-Stream" }, true },
        { new[] { "text / event-stream" }, true },
        { new[] { "  text/event-stream  " }, true },
        { new[] { "application/json, text/event-stream" }, true },
        { new[] { "application/json", "text/event-stream" }, true },
        { new[] { null, "text/event-stream" }, true },
        { new[] { "text/event-stream; charset=utf-8" }, false },
        { new[] { "text/event-stream;q=0.5" }, false },
        { new[] { "text/event-streams" }, false },
        { new[] { "application/event-stream" }, false },
        { new[] { "event-stream" }, false },
        { new[] { "text/event-stream, @@@" }, true },
        { new[] { "application/json", "@@@", "text/event-stream" }, true },
    };

    [Theory]
    [MemberData(nameof(AcceptHeaders))]
    public void MatchesParsedAcceptHeader(string[] accept, bool expected)
    {
        var context = new DefaultHttpContext();
        if (accept is not null)
        {
            context.Request.Headers.Accept = new StringValues(accept);
        }

        // The pre-check must agree with parsing the full Accept header.
        var parsed = context.Request.GetTypedHeaders().Accept?.Contains(new MediaTypeHeaderValue("text/event-stream")) == true;
        Assert.Equal(expected, parsed);
        Assert.Equal(expected, HttpConnectionDispatcher.IsServerSentEventsRequest(context.Request));
    }
}
