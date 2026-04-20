// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class SessionCascadingValueSupplierTest
{
    private readonly SessionCascadingValueSupplier _supplier;

    public SessionCascadingValueSupplierTest()
    {
        _supplier = new SessionCascadingValueSupplier(NullLogger<SessionCascadingValueSupplier>.Instance);
    }

    [Fact]
    public async Task RegisterValueCallback_AddsCallback()
    {
        var callbackInvoked = false;
        _supplier.RegisterValueCallback("key", () =>
        {
            callbackInvoked = true;
            return "value";
        });

        var httpContext = CreateHttpContextWithSession();
        _supplier.SetRequestContext(httpContext);
        await _supplier.PersistAllValues();

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void RegisterValueCallback_ThrowsForDuplicateKey()
    {
        _supplier.RegisterValueCallback("key", () => "value1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _supplier.RegisterValueCallback("key", () => "value2"));

        Assert.Contains("key", ex.Message);
    }

    [Fact]
    public async Task PersistAllValues_SetsValueInSession()
    {
        _supplier.RegisterValueCallback("key", () => "persisted value");

        var httpContext = CreateHttpContextWithSession();
        _supplier.SetRequestContext(httpContext);
        await _supplier.PersistAllValues();

        Assert.Equal("\"persisted value\"", httpContext.Session.GetString("key"));
    }

    [Fact]
    public async Task PersistAllValues_RemovesKey_WhenCallbackReturnsNull()
    {
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString("key", "\"existing\"");

        _supplier.RegisterValueCallback("key", () => null);
        _supplier.SetRequestContext(httpContext);
        await _supplier.PersistAllValues();

        Assert.Null(httpContext.Session.GetString("key"));
    }

    [Fact]
    public async Task PersistAllValues_HandlesMultipleKeys()
    {
        _supplier.RegisterValueCallback("key1", () => "value1");
        _supplier.RegisterValueCallback("key2", () => "value2");
        _supplier.RegisterValueCallback("key3", () => "value3");

        var httpContext = CreateHttpContextWithSession();
        _supplier.SetRequestContext(httpContext);
        await _supplier.PersistAllValues();

        Assert.Equal("\"value1\"", httpContext.Session.GetString("key1"));
        Assert.Equal("\"value2\"", httpContext.Session.GetString("key2"));
        Assert.Equal("\"value3\"", httpContext.Session.GetString("key3"));
    }

    [Fact]
    public async Task PersistAllValues_ContinuesOnCallbackException()
    {
        _supplier.RegisterValueCallback("key1", () => throw new InvalidOperationException("Test exception"));
        _supplier.RegisterValueCallback("key2", () => "value2");

        var httpContext = CreateHttpContextWithSession();
        _supplier.SetRequestContext(httpContext);
        await _supplier.PersistAllValues();

        Assert.Null(httpContext.Session.GetString("key1"));
        Assert.Equal("\"value2\"", httpContext.Session.GetString("key2"));
    }

    [Fact]
    public async Task PersistAllValues_ContinuesOnSerializationException()
    {
        _supplier.RegisterValueCallback("key1", () => new IntPtr(42));
        _supplier.RegisterValueCallback("key2", () => "value2");

        var httpContext = CreateHttpContextWithSession();
        _supplier.SetRequestContext(httpContext);
        await _supplier.PersistAllValues();

        Assert.Null(httpContext.Session.GetString("key1"));
        Assert.Equal("\"value2\"", httpContext.Session.GetString("key2"));
    }

    [Fact]
    public async Task PersistAllValues_LowercasesSessionKey()
    {
        _supplier.RegisterValueCallback("MyKey", () => "value");

        var httpContext = CreateHttpContextWithSession();
        _supplier.SetRequestContext(httpContext);
        await _supplier.PersistAllValues();

        Assert.Equal("\"value\"", httpContext.Session.GetString("mykey"));
    }

    [Fact]
    public async Task PersistAllValues_NoOp_WhenSessionUnavailable()
    {
        _supplier.RegisterValueCallback("key", () => "value");

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseFeature>(new TestHttpResponseFeature());
        _supplier.SetRequestContext(httpContext);

        await _supplier.PersistAllValues();
    }

    [Fact]
    public async Task DeleteCallbacks_RemovesCallbacksForKey()
    {
        var callbackInvoked = false;
        _supplier.RegisterValueCallback("key", () =>
        {
            callbackInvoked = true;
            return "value";
        });

        _supplier.DeleteValueCallback("key");

        var httpContext = CreateHttpContextWithSession();
        _supplier.SetRequestContext(httpContext);
        await _supplier.PersistAllValues();

        Assert.False(callbackInvoked);
        Assert.Null(httpContext.Session.GetString("key"));
    }

    [Fact]
    public async Task SetRequestContext_RegistersOnStartingCallback()
    {
        _supplier.RegisterValueCallback("key", () => "value");

        var httpContext = CreateHttpContextWithSession(out var responseFeature);
        _supplier.SetRequestContext(httpContext);

        await responseFeature.FireOnStartingAsync();

        Assert.Equal("\"value\"", httpContext.Session.GetString("key"));
    }

    internal static DefaultHttpContext CreateHttpContextWithSession()
    {
        return CreateHttpContextWithSession(out _);
    }

    internal static DefaultHttpContext CreateHttpContextWithSession(out TestHttpResponseFeature responseFeature)
    {
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        httpContext.Features.Set<ISessionFeature>(new TestSessionFeature(session));

        responseFeature = new TestHttpResponseFeature();
        httpContext.Features.Set<IHttpResponseFeature>(responseFeature);

        return httpContext;
    }

    internal class TestSession : ISession
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

    internal class TestSessionFeature : ISessionFeature
    {
        public TestSessionFeature(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; set; }
    }

    internal class TestHttpResponseFeature : IHttpResponseFeature
    {
        private readonly Stack<(Func<object, Task> Callback, object State)> _onStarting = new();

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted { get; private set; }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            _onStarting.Push((callback, state));
        }

        public async Task FireOnStartingAsync()
        {
            foreach (var (callback, state) in _onStarting)
            {
                await callback(state);
            }
            HasStarted = true;
        }
    }
}
