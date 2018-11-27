// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class SessionStateTempDataProviderTest
    {
        [Fact]
        public void Load_ThrowsException_WhenSessionIsNotEnabled()
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();

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
            var testProvider = new SessionStateTempDataProvider();
            var values = new Dictionary<string, object>();
            values.Add("key1", "value1");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                testProvider.SaveTempData(GetHttpContext(sessionEnabled: false), values);
            });
        }

        [Fact]
        public void Load_ReturnsEmptyDictionary_WhenNoSessionDataIsAvailable()
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();

            // Act
            var tempDataDictionary = testProvider.LoadTempData(GetHttpContext());

            // Assert
            Assert.Empty(tempDataDictionary);
        }

        [Fact]
        public void SaveAndLoad_StringCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();
            var input = new Dictionary<string, object>
            {
                { "string", "value" }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var stringVal = Assert.IsType<string>(TempData["string"]);
            Assert.Equal("value", stringVal);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void SaveAndLoad_IntCanBeStoredAndLoaded(int expected)
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();
            var input = new Dictionary<string, object>
            {
                { "int", expected }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var intVal = Assert.IsType<int>(TempData["int"]);
            Assert.Equal(expected, intVal);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SaveAndLoad_BoolCanBeStoredAndLoaded(bool value)
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();
            var input = new Dictionary<string, object>
            {
                { "bool", value }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var boolVal = Assert.IsType<bool>(TempData["bool"]);
            Assert.Equal(value, boolVal);
        }

        [Fact]
        public void SaveAndLoad_DateTimeCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();
            var inputDatetime = new DateTime(2010, 12, 12, 1, 2, 3, DateTimeKind.Local);
            var input = new Dictionary<string, object>
            {
                { "DateTime", inputDatetime }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var datetime = Assert.IsType<DateTime>(TempData["DateTime"]);
            Assert.Equal(inputDatetime, datetime);
        }

        [Fact]
        public void SaveAndLoad_GuidCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();
            var inputGuid = Guid.NewGuid();
            var input = new Dictionary<string, object>
            {
                { "Guid", inputGuid }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var guidVal = Assert.IsType<Guid>(TempData["Guid"]);
            Assert.Equal(inputGuid, guidVal);
        }

        [Fact]
        public void SaveAndLoad_EnumCanBeSavedAndLoaded()
        {
            // Arrange
            var key = "EnumValue";
            var testProvider = new SessionStateTempDataProvider();
            var expected = DayOfWeek.Friday;
            var input = new Dictionary<string, object>
            {
                { key, expected }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);
            var result = TempData[key];

            // Assert
            var actual = (DayOfWeek)result;
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(3100000000L)]
        [InlineData(-3100000000L)]
        public void SaveAndLoad_LongCanBeSavedAndLoaded(long expected)
        {
            // Arrange
            var key = "LongValue";
            var testProvider = new SessionStateTempDataProvider();
            var input = new Dictionary<string, object>
            {
                { key, expected }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);
            var result = TempData[key];

            // Assert
            var actual = Assert.IsType<long>(result);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SaveAndLoad_ListCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();
            var input = new Dictionary<string, object>
            {
                { "List`string", new List<string> { "one", "two" } }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var list = (IList<string>)TempData["List`string"];
            Assert.Equal(2, list.Count);
            Assert.Equal("one", list[0]);
            Assert.Equal("two", list[1]);
        }

        [Fact]
        public void SaveAndLoad_DictionaryCanBeStoredAndLoaded()
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();
            var inputDictionary = new Dictionary<string, string>
            {
                { "Hello", "World" },
            };
            var input = new Dictionary<string, object>
            {
                { "Dictionary", inputDictionary }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var dictionary = Assert.IsType<Dictionary<string, string>>(TempData["Dictionary"]);
            Assert.Equal("World", dictionary["Hello"]);
        }

        [Fact]
        public void SaveAndLoad_EmptyDictionary_RoundTripsAsNull()
        {
            // Arrange
            var testProvider = new SessionStateTempDataProvider();
            var input = new Dictionary<string, object>
            {
                { "EmptyDictionary", new Dictionary<string, int>() }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            var TempData = testProvider.LoadTempData(context);

            // Assert
            var emptyDictionary = (IDictionary<string, int>)TempData["EmptyDictionary"];
            Assert.Null(emptyDictionary);
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

        private class SessionFeature : ISessionFeature
        {
            public ISession Session { get; set; }
        }

        private class TestSession : ISession
        {
            private Dictionary<string, byte[]> _innerDictionary = new Dictionary<string, byte[]>();

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
    }
}