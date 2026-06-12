// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class SessionEstablishmentHelperTest
{
    public SessionEstablishmentHelperTest()
    {
        // The helper guards each warning with a static "log once per process" flag.
        // Reset that state so each test observes logging deterministically.
        ResetLogGuards();
    }

    [Fact]
    public void TryRegisterSessionEstablishment_LogsSessionDoesNotExist_WhenNoSessionFeature()
    {
        var sink = new TestSink();
        var httpContext = CreateHttpContext(sink, session: null, responseHasStarted: false);

        SessionEstablishmentHelper.TryRegisterSessionEstablishment(httpContext);

        var write = Assert.Single(sink.Writes);
        Assert.Equal(LogLevel.Warning, write.LogLevel);
        Assert.Equal("SessionDoesNotExist", write.EventId.Name);
    }

    [Fact]
    public void TryRegisterSessionEstablishment_LogsResponseHasStarted_WhenSessionAvailableButResponseStarted()
    {
        var sink = new TestSink();
        var httpContext = CreateHttpContext(sink, session: new TestSession(), responseHasStarted: true);

        SessionEstablishmentHelper.TryRegisterSessionEstablishment(httpContext);

        var write = Assert.Single(sink.Writes);
        Assert.Equal(LogLevel.Warning, write.LogLevel);
        Assert.Equal("SessionStateNotPersistedAfterResponseStarted", write.EventId.Name);
    }

    [Fact]
    public void TryRegisterSessionEstablishment_DoesNotLog_WhenSessionAvailableAndResponseNotStarted()
    {
        var sink = new TestSink();
        var httpContext = CreateHttpContext(sink, session: new TestSession(), responseHasStarted: false);

        SessionEstablishmentHelper.TryRegisterSessionEstablishment(httpContext);

        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void TryRegisterSessionEstablishment_LogsSessionDoesNotExistOnlyOnce_AcrossRequests()
    {
        var sink = new TestSink();
        var first = CreateHttpContext(sink, session: null, responseHasStarted: false);
        var second = CreateHttpContext(sink, session: null, responseHasStarted: false);

        SessionEstablishmentHelper.TryRegisterSessionEstablishment(first);
        SessionEstablishmentHelper.TryRegisterSessionEstablishment(second);

        var write = Assert.Single(sink.Writes);
        Assert.Equal("SessionDoesNotExist", write.EventId.Name);
    }

    [Fact]
    public void TryRegisterSessionEstablishment_LogsResponseHasStartedOnlyOnce_AcrossRequests()
    {
        var sink = new TestSink();
        var first = CreateHttpContext(sink, session: new TestSession(), responseHasStarted: true);
        var second = CreateHttpContext(sink, session: new TestSession(), responseHasStarted: true);

        SessionEstablishmentHelper.TryRegisterSessionEstablishment(first);
        SessionEstablishmentHelper.TryRegisterSessionEstablishment(second);

        var write = Assert.Single(sink.Writes);
        Assert.Equal("SessionStateNotPersistedAfterResponseStarted", write.EventId.Name);
    }

    private static DefaultHttpContext CreateHttpContext(TestSink sink, ISession? session, bool responseHasStarted)
    {
        var responseFeature = new TestHttpResponseFeature { HasStarted = responseHasStarted };
        return CreateHttpContext(sink, session, responseFeature);
    }

    private static DefaultHttpContext CreateHttpContext(TestSink sink, ISession? session, TestHttpResponseFeature responseFeature)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };

        if (session is not null)
        {
            httpContext.Features.Set<ISessionFeature>(new TestSessionFeature(session));
        }

        httpContext.Features.Set<IHttpResponseFeature>(responseFeature);
        return httpContext;
    }

    private static void ResetLogGuards()
    {
        SessionEstablishmentHelper.HasLoggedResponseHasStarted = false;
        SessionEstablishmentHelper.HasLoggedSessionDoesNotExist = false;
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
    }

    private sealed class TestSessionFeature : ISessionFeature
    {
        public TestSessionFeature(ISession session) => Session = session;

        public ISession Session { get; set; }
    }

    private sealed class TestHttpResponseFeature : IHttpResponseFeature
    {
        public int OnStartingCount { get; private set; }

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted { get; set; }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state) => OnStartingCount++;
    }
}
