// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class SessionValueMapperTest
{
    private SessionValueMapper GetSessionValueMapper()
    {
        return new SessionValueMapper(new Microsoft.Extensions.Logging.Abstractions.NullLogger<SessionValueMapper>());
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenKeyNotFound()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession();
        mapper.SetRequestContext(httpContext);

        // Act
        var result = mapper.GetValue("nonexistent", typeof(string));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ReturnsDeserializedValue_WhenKeyExists()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString("email", "\"test@example.com\"");
        mapper.SetRequestContext(httpContext);

        // Act
        var result = mapper.GetValue("email", typeof(string));

        // Assert
        Assert.Equal("test@example.com", result);
    }

    [Fact]
    public void GetValue_DeserializesComplexType()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString("user", "{\"name\":\"John\",\"age\":30}");
        mapper.SetRequestContext(httpContext);

        // Act
        var result = mapper.GetValue("user", typeof(TestUser));

        // Assert
        var user = Assert.IsType<TestUser>(result);
        Assert.Equal("John", user.Name);
        Assert.Equal(30, user.Age);
    }

    [Fact]
    public void RegisterValueCallback_AddsCallbackToList()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var callCount = 0;

        // Act
        mapper.RegisterValueCallback("key", () => { callCount++; return "value"; });

        // Assert
        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task RegisterValueCallback_AllowsMultipleCallbacksForSameKey()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession(out var responseFeature);
        mapper.SetRequestContext(httpContext);

        var callOrder = new List<int>();
        mapper.RegisterValueCallback("key", () => { callOrder.Add(1); return null; });
        mapper.RegisterValueCallback("key", () => { callOrder.Add(2); return "value2"; });
        mapper.RegisterValueCallback("key", () => { callOrder.Add(3); return "value3"; });

        // Act
        await responseFeature.FireOnStartingAsync();

        // Assert
        Assert.Equal(new[] { 1, 2 }, callOrder);
        Assert.Equal("\"value2\"", httpContext.Session.GetString("key"));
    }

    [Fact]
    public async Task PersistAllValues_PersistsFirstNonNullValue()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession(out var responseFeature);
        mapper.SetRequestContext(httpContext);

        mapper.RegisterValueCallback("email", () => null);
        mapper.RegisterValueCallback("email", () => "test@example.com");
        mapper.RegisterValueCallback("email", () => "other@example.com");

        // Act
        await responseFeature.FireOnStartingAsync();

        // Assert
        Assert.Equal("\"test@example.com\"", httpContext.Session.GetString("email"));
    }

    [Fact]
    public async Task PersistAllValues_RemovesKey_WhenAllCallbacksReturnNull()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession(out var responseFeature);
        httpContext.Session.SetString("email", "\"existing@example.com\"");
        mapper.SetRequestContext(httpContext);

        mapper.RegisterValueCallback("email", () => null);
        mapper.RegisterValueCallback("email", () => null);

        // Act
        await responseFeature.FireOnStartingAsync();

        // Assert
        Assert.Null(httpContext.Session.GetString("email"));
    }

    [Fact]
    public async Task PersistAllValues_HandlesMultipleKeys()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession(out var responseFeature);
        mapper.SetRequestContext(httpContext);

        mapper.RegisterValueCallback("key1", () => "value1");
        mapper.RegisterValueCallback("key2", () => "value2");

        // Act
        await responseFeature.FireOnStartingAsync();

        // Assert
        Assert.Equal("\"value1\"", httpContext.Session.GetString("key1"));
        Assert.Equal("\"value2\"", httpContext.Session.GetString("key2"));
    }

    [Fact]
    public async Task PersistAllValues_SerializesComplexTypes()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession(out var responseFeature);
        mapper.SetRequestContext(httpContext);

        mapper.RegisterValueCallback("user", () => new TestUser { Name = "Jane", Age = 25 });

        // Act
        await responseFeature.FireOnStartingAsync();

        // Assert
        var json = httpContext.Session.GetString("user");
        Assert.NotNull(json);
        Assert.Contains("\"name\":\"Jane\"", json);
        Assert.Contains("\"age\":25", json);
    }

    [Fact]
    public async Task HandlesIncorrectValuesInSession()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession(out var responseFeature);
        mapper.SetRequestContext(httpContext);

        mapper.RegisterValueCallback("number", () => "not-a-number");

        // Act
        await responseFeature.FireOnStartingAsync();
        var result = mapper.GetValue("number", typeof(int));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HandlesCallbackThrowing()
    {
        // Arrange
        var mapper = GetSessionValueMapper();
        var httpContext = CreateHttpContextWithSession(out var responseFeature);
        mapper.SetRequestContext(httpContext);

        mapper.RegisterValueCallback("number", () => throw new Exception("Callback exception"));

        // Act
        await responseFeature.FireOnStartingAsync();
        var result = mapper.GetValue("number", typeof(int));

        // Assert
        Assert.Null(result);
    }

    private static DefaultHttpContext CreateHttpContextWithSession()
    {
        return CreateHttpContextWithSession(out _);
    }

    private static DefaultHttpContext CreateHttpContextWithSession(out TestHttpResponseFeature responseFeature)
    {
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        httpContext.Features.Set<ISessionFeature>(new TestSessionFeature(session));

        responseFeature = new TestHttpResponseFeature();
        httpContext.Features.Set<IHttpResponseFeature>(responseFeature);

        return httpContext;
    }

    private class TestUser
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private class TestSession : ISession
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

    private class TestSessionFeature : ISessionFeature
    {
        public TestSessionFeature(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; set; }
    }

    private class TestHttpResponseFeature : IHttpResponseFeature
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

