// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class TempDataDictionaryTest
    {
        [Fact]
        public void TempData_Load_CreatesEmptyDictionaryIfProviderReturnsNull()
        {
            // Arrange
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), new NullTempDataProvider());

            // Act
            tempData.Load();

            // Assert
            Assert.Empty(tempData);
        }

        [Fact]
        public void TempData_Save_RemovesKeysThatWereRead()
        {
            // Arrange
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), new NullTempDataProvider());
            tempData["Foo"] = "Foo";
            tempData["Bar"] = "Bar";

            // Act
            var value = tempData["Foo"];
            tempData.Save();

            // Assert
            Assert.False(tempData.ContainsKey("Foo"));
            Assert.True(tempData.ContainsKey("Bar"));
        }

        [Fact]
        public void TempData_EnumeratingDictionary_MarksKeysForDeletion()
        {
            // Arrange
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), new NullTempDataProvider());
            tempData["Foo"] = "Foo";
            tempData["Bar"] = "Bar";

            // Act
            var enumerator = tempData.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
            }
            tempData.Save();

            // Assert
            Assert.False(tempData.ContainsKey("Foo"));
            Assert.False(tempData.ContainsKey("Bar"));
        }

        [Fact]
        public void TempData_TryGetValue_MarksKeyForDeletion()
        {
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), new NullTempDataProvider());
            object value;
            tempData["Foo"] = "Foo";

            // Act
            tempData.TryGetValue("Foo", out value);
            tempData.Save();

            // Assert
            Assert.False(tempData.ContainsKey("Foo"));
        }

        [Fact]
        public void TempData_Keep_RetainsAllKeysWhenSavingDictionary()
        {
            // Arrange
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), new NullTempDataProvider());
            tempData["Foo"] = "Foo";
            tempData["Bar"] = "Bar";

            // Act
            tempData.Keep();
            tempData.Save();

            // Assert
            Assert.True(tempData.ContainsKey("Foo"));
            Assert.True(tempData.ContainsKey("Bar"));
        }

        [Fact]
        public void TempData_Keep_RetainsSpecificKeysWhenSavingDictionary()
        {
            // Arrange
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), new NullTempDataProvider());
            tempData["Foo"] = "Foo";
            tempData["Bar"] = "Bar";

            // Act
            var foo = tempData["Foo"];
            var bar = tempData["Bar"];
            tempData.Keep("Foo");
            tempData.Save();

            // Assert
            Assert.True(tempData.ContainsKey("Foo"));
            Assert.False(tempData.ContainsKey("Bar"));
        }

        [Fact]
        public void TempData_Peek_DoesNotMarkKeyForDeletion()
        {
            // Arrange
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), new NullTempDataProvider());
            tempData["Bar"] = "barValue";

            // Act
            var value = tempData.Peek("bar");
            tempData.Save();

            // Assert
            Assert.Equal("barValue", value);
            Assert.True(tempData.ContainsKey("Bar"));
        }

        [Fact]
        public void TempData_CompareIsOrdinalIgnoreCase()
        {
            // Arrange
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), new NullTempDataProvider());
            var item = new object();

            // Act
            tempData["Foo"] = item;
            var value = tempData["FOO"];

            // Assert
            Assert.Same(item, value);
        }

        [Fact]
        public void TempData_LoadAndSaveAreCaseInsensitive()
        {
            // Arrange
            var data = new Dictionary<string, object>();
            data["Foo"] = "Foo";
            data["Bar"] = "Bar";
            var provider = new TestTempDataProvider(data);
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), provider);

            // Act
            tempData.Load();
            var value = tempData["FOO"];
            tempData.Save();

            // Assert
            Assert.False(tempData.ContainsKey("foo"));
            Assert.True(tempData.ContainsKey("bar"));
        }

        [Fact]
        public void TempData_RemovalOfKeysAreCaseInsensitive()
        {
            var tempData = new TempDataDictionary(GetHttpContextAccessor(), new NullTempDataProvider());
            object fooValue;
            tempData["Foo"] = "Foo";
            tempData["Bar"] = "Bar";

            // Act
            tempData.TryGetValue("foo", out fooValue);
            var barValue = tempData["bar"];
            tempData.Save();

            // Assert
            Assert.False(tempData.ContainsKey("Foo"));
            Assert.False(tempData.ContainsKey("Boo"));
        }

        private class NullTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData([NotNull]HttpContext context)
            {
                return null;
            }

            public void SaveTempData([NotNull]HttpContext context, IDictionary<string, object> values)
            {
            }
        }

        private class TestTempDataProvider : ITempDataProvider
        {
            private IDictionary<string, object> _data;

            public TestTempDataProvider(IDictionary<string, object> data)
            {
                _data = data;
            }

            public IDictionary<string, object> LoadTempData([NotNull]HttpContext context)
            {
                return _data;
            }

            public void SaveTempData([NotNull]HttpContext context, IDictionary<string, object> values)
            {
            }
        }

        private static IHttpContextAccessor GetHttpContextAccessor()
        {
            var httpContext = new Mock<HttpContext>();
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext.Object);
            return httpContextAccessor.Object;
        }
    }
}