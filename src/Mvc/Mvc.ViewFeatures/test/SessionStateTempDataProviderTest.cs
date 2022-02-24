// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class SessionStateTempDataProviderTest
{
    private static readonly byte[] Bytes = Encoding.UTF8.GetBytes("test value");
    private static readonly IDictionary<string, object> Dictionary = new Dictionary<string, object>
        {
            { "key", "value" },
        };

    [Fact]
    public void Load_ThrowsException_WhenSessionIsNotEnabled()
    {
        // Arrange
        var testProvider = CreateTempDataProvider();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            testProvider.LoadTempData(GetHttpContext(sessionEnabled: false));
        });
    }

    [Fact]
    public void Save_ThrowsException_WhenSessionIsNotEnabled()
    {
        // Arrange
        var testProvider = CreateTempDataProvider();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            testProvider.SaveTempData(GetHttpContext(sessionEnabled: false), Dictionary);
        });
    }

    [Fact]
    public void Load_ReturnsEmptyDictionary_WhenNoSessionDataIsAvailable()
    {
        // Arrange
        var testProvider = CreateTempDataProvider();

        // Act
        var tempDataDictionary = testProvider.LoadTempData(GetHttpContext());

        // Assert
        Assert.Empty(tempDataDictionary);
    }

    [Fact]
    public void SaveAndLoad_Works()
    {
        // Arrange
        var testProvider = CreateTempDataProvider();
        var context = GetHttpContext();

        // Act
        testProvider.SaveTempData(context, Dictionary);
        var result = testProvider.LoadTempData(context);

        // Assert
        Assert.Same(Dictionary, result);
    }

    private class TestItem
    {
        public int DummyInt { get; set; }
    }

    private HttpContext GetHttpContext(bool sessionEnabled = true)
    {
        var httpContext = new DefaultHttpContext();
        if (sessionEnabled)
        {
            httpContext.Features.Set<ISessionFeature>(new SessionFeature() { Session = new TestSession() });
        }
        return httpContext;
    }

    private static SessionStateTempDataProvider CreateTempDataProvider()
    {
        var tempDataSerializer = new TestTempDataSerializer();
        return new SessionStateTempDataProvider(tempDataSerializer);
    }

    private class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; }
    }

    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _innerDictionary = new Dictionary<string, byte[]>();

        public IEnumerable<string> Keys { get { return _innerDictionary.Keys; } }

        public string Id => "TestId";

        public bool IsAvailable { get; } = true;

        public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public void Clear()
        {
            _innerDictionary.Clear();
        }

        public void Remove(string key)
        {
            _innerDictionary.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _innerDictionary[key] = value.ToArray();
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _innerDictionary.TryGetValue(key, out value);
        }
    }

    private class TestTempDataSerializer : TempDataSerializer
    {
        public override IDictionary<string, object> Deserialize(byte[] unprotectedData)
        {
            return Dictionary;
        }

        public override byte[] Serialize(IDictionary<string, object> values)
        {
            return Bytes;
        }
    }
}
