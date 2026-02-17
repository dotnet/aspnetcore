// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class SessionStorageTempDataProviderTest
{
    private readonly SessionStorageTempDataProvider _sessionStateTempDataProvider;

    internal TempData CreateTempData()
    {
        return new TempData(() => new Dictionary<string, object>());
    }

    public SessionStorageTempDataProviderTest()
    {
        _sessionStateTempDataProvider = new SessionStorageTempDataProvider(
            new JsonTempDataSerializer(),
            NullLogger<SessionStorageTempDataProvider>.Instance);
    }

    [Fact]
    public void Load_ReturnsEmptyTempData_WhenNoSessionDataExists()
    {
        var httpContext = CreateHttpContext();
        var tempData = _sessionStateTempDataProvider.LoadTempData(httpContext);

        Assert.NotNull(tempData);
        Assert.Empty(tempData);
    }

    [Fact]
    public void Save_RemovesSessionKey_WhenNoDataToSave()
    {
        var httpContext = CreateHttpContext();
        var session = (TestSession)httpContext.Session;
        session.Set(SessionStorageTempDataProvider.TempDataSessionStateKey, new byte[] { 1, 2, 3 });

        var tempData = CreateTempData();
        _sessionStateTempDataProvider.SaveTempData(httpContext, tempData.Save());

        Assert.DoesNotContain(SessionStorageTempDataProvider.TempDataSessionStateKey, session.Keys);
    }

    [Fact]
    public void Save_SetsSessionData_WhenDataExists()
    {
        var httpContext = CreateHttpContext();
        var tempData = CreateTempData();
        tempData["Key1"] = "Value1";

        _sessionStateTempDataProvider.SaveTempData(httpContext, tempData.Save());

        var session = (TestSession)httpContext.Session;
        Assert.Contains(SessionStorageTempDataProvider.TempDataSessionStateKey, session.Keys);
    }

    [Fact]
    public void Save_ThrowsForUnsupportedType()
    {
        var httpContext = CreateHttpContext();
        var tempData = CreateTempData();
        tempData["Key"] = new object();

        Assert.Throws<InvalidOperationException>(() => _sessionStateTempDataProvider.SaveTempData(httpContext, tempData.Save()));
    }

    [Fact]
    public void Load_ReturnsEmptyTempData_WhenSessionThrows()
    {
        var httpContext = CreateHttpContext(throwOnSessionAccess: true);
        var tempData = _sessionStateTempDataProvider.LoadTempData(httpContext);

        Assert.NotNull(tempData);
        Assert.Empty(tempData);
    }

    [Fact]
    public void Load_ReturnsEmptyTempData_WhenSessionContainsInvalidJson()
    {
        var httpContext = CreateHttpContext();
        var session = (TestSession)httpContext.Session;
        session.Set(SessionStorageTempDataProvider.TempDataSessionStateKey, "not valid json"u8.ToArray());

        var tempData = _sessionStateTempDataProvider.LoadTempData(httpContext);

        Assert.NotNull(tempData);
        Assert.Empty(tempData);
    }

    [Fact]
    public void RoundTrip_SaveAndLoad_WorksCorrectly()
    {
        var httpContext = CreateHttpContext();
        var tempData = CreateTempData();
        tempData["StringKey"] = "StringValue";
        tempData["IntKey"] = 42;

        _sessionStateTempDataProvider.SaveTempData(httpContext, tempData.Save());
        var loadedTempData = _sessionStateTempDataProvider.LoadTempData(httpContext);

        Assert.Equal("StringValue", loadedTempData["StringKey"]);
        Assert.Equal(42, loadedTempData["IntKey"]);
    }

    private static DefaultHttpContext CreateHttpContext(bool throwOnSessionAccess = false)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");

        var sessionFeature = new TestSessionFeature(throwOnSessionAccess);
        httpContext.Features.Set<ISessionFeature>(sessionFeature);

        return httpContext;
    }

    private class TestSessionFeature : ISessionFeature
    {
        private readonly bool _throwOnAccess;

        public TestSessionFeature(bool throwOnAccess = false)
        {
            _throwOnAccess = throwOnAccess;
            Session = new TestSession();
        }

        public ISession Session
        {
            get => _throwOnAccess ? throw new InvalidOperationException("Session not configured") : field;
            set;
        }
    }

    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public string Id => "test-session-id";
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}
