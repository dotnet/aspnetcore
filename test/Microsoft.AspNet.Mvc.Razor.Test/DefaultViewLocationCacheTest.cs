// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class DefaultViewLocationCacheTest
    {
        public static IEnumerable<object[]> CacheEntryData
        {
            get
            {
                yield return new[] { new ViewLocationExpanderContext(GetActionContext(), "test", isPartial: false) };
                yield return new[] { new ViewLocationExpanderContext(GetActionContext(), "test", isPartial: true) };

                var areaActionContext = GetActionContext("controller2", "myarea");
                yield return new[] { new ViewLocationExpanderContext(areaActionContext, "test2", isPartial: false) };
                yield return new[] { new ViewLocationExpanderContext(areaActionContext, "test2", isPartial: true) };

                var actionContext = GetActionContext("controller3", "area3");
                var values = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "culture", "fr" },
                    { "theme", "sleek" }
                };
                var expanderContext = new ViewLocationExpanderContext(actionContext, "test3", isPartial: false)
                {
                    Values = values
                };
                yield return new[] { expanderContext };

                expanderContext = new ViewLocationExpanderContext(actionContext, "test3", isPartial: true)
                {
                    Values = values
                };
                yield return new[] { expanderContext };
            }
        }

        private static DefaultViewLocationCache.ViewLocationCacheKeyComparer CacheKeyComparer =>
            DefaultViewLocationCache.ViewLocationCacheKeyComparer.Instance;

        [Theory]
        [MemberData(nameof(CacheEntryData))]
        public void Get_ReturnsNoneResultIfItemDoesNotExist(ViewLocationExpanderContext context)
        {
            // Arrange
            var cache = new DefaultViewLocationCache();

            // Act
            var result = cache.Get(context);

            // Assert
            Assert.Equal(result, ViewLocationCacheResult.None);
        }

        [Theory]
        [MemberData(nameof(CacheEntryData))]
        public void InvokingGetAfterSet_ReturnsCachedItem(ViewLocationExpanderContext context)
        {
            // Arrange
            var cache = new DefaultViewLocationCache();
            var value = new ViewLocationCacheResult(
                Guid.NewGuid().ToString(),
                new[]
                {
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString()
                });

            // Act - 1
            cache.Set(context, value);
            var result = cache.Get(context);

            // Assert - 1
            Assert.Equal(value, result);

            // Act - 2
            result = cache.Get(context);

            // Assert - 2
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData("View1", "View2")]
        [InlineData("View1", "view1")]
        public void ViewLocationCacheKeyComparer_EqualsReturnsFalseIfViewNamesAreDifferent(
            string viewName1,
            string viewName2)
        {
            // Arrange
            var actionContext = GetActionContext();
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                actionContext,
                viewName1,
                isPartial: true);
            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               actionContext,
               viewName2,
               isPartial: true);

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.False(result);
            Assert.NotEqual(hash1, hash2);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void ViewLocationCacheKeyComparer_EqualsReturnsFalseIfIsPartialAreDifferent(
            bool isPartial1,
            bool isPartial2)
        {
            // Arrange
            var actionContext = GetActionContext();
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                actionContext,
                "View1",
                isPartial1);
            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               actionContext,
               "View1",
               isPartial2);

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.False(result);
            Assert.NotEqual(hash1, hash2);
        }

        [Theory]
        [InlineData("Controller1", "Controller2")]
        [InlineData("controller1", "Controller1")]
        public void ViewLocationCacheKeyComparer_EqualsReturnsFalseIfIsControllerNamesAreDifferent(
            string controller1,
            string controller2)
        {
            // Arrange
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                GetActionContext(controller1),
                "View1",
                isPartial: false);
            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               GetActionContext(controller2),
               "View1",
               isPartial: false);

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.False(result);
            Assert.NotEqual(hash1, hash2);
        }

        [Theory]
        [InlineData("area1", null)]
        [InlineData("Area1", "Area2")]
        [InlineData("area1", "aRea1")]
        public void ViewLocationCacheKeyComparer_EqualsReturnsFalseIfIsAreaNamesAreDifferent(
            string area1,
            string area2)
        {
            // Arrange
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                GetActionContext("Controller1", area1),
                "View1",
                isPartial: false);
            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               GetActionContext("Controller1", area2),
               "View1",
               isPartial: false);

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.False(result);
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void ViewLocationCacheKeyComparer_EqualsReturnsTrueIfControllerAreaAndViewNamesAreIdentical()
        {
            // Arrange
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                GetActionContext("Controller1", "Area1"),
                "View1",
                isPartial: false);
            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               GetActionContext("Controller1", "Area1"),
               "View1",
               isPartial: false);

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.True(result);
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void ViewLocationCacheKeyComparer_EqualsReturnsFalseIfViewLocationExpanderIsNullForEitherKey()
        {
            // Arrange
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                GetActionContext("Controller1", "Area1"),
                "View1",
                isPartial: false);
            viewLocationExpanderContext1.Values = new Dictionary<string, string>
            {
                { "somekey", "somevalue" }
            };

            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               GetActionContext("Controller1", "Area1"),
               "View1",
               isPartial: false);

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.False(result);
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void ViewLocationCacheKeyComparer_EqualsReturnsFalseIfExpanderValueCountIsDifferent()
        {
            // Arrange
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                GetActionContext("Controller1", "Area1"),
                "View1",
                isPartial: false);
            viewLocationExpanderContext1.Values = new Dictionary<string, string>
            {
                { "somekey", "somevalue" }
            };

            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               GetActionContext("Controller1", "Area1"),
               "View1",
               isPartial: false);
            viewLocationExpanderContext2.Values = new Dictionary<string, string>
            {
                { "somekey", "somevalue" },
                { "somekey2", "somevalue2" },
            };

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.False(result);
            Assert.NotEqual(hash1, hash2);
        }

        [Theory]
        [InlineData("key1", "key2")]
        [InlineData("Key1", "key1")]
        public void ViewLocationCacheKeyComparer_EqualsReturnsFalseIfKeysAreDifferent(
            string keyName1,
            string keyName2)
        {
            // Arrange
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                GetActionContext("Controller1", "Area1"),
                "View1",
                isPartial: false);
            viewLocationExpanderContext1.Values = new Dictionary<string, string>
            {
                { keyName1, "somevalue" }
            };

            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               GetActionContext("Controller1", "Area1"),
               "View1",
               isPartial: false);
            viewLocationExpanderContext2.Values = new Dictionary<string, string>
            {
                { keyName2, "somevalue" },
            };

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.False(result);
            Assert.NotEqual(hash1, hash2);
        }

        [Theory]
        [InlineData("value1", null)]
        [InlineData("value1", "value2")]
        [InlineData("value1", "Value1")]
        public void ViewLocationCacheKeyComparer_EqualsReturnsFalseIfValuesAreDifferent(
            string value1,
            string value2)
        {
            // Arrange
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                GetActionContext("Controller1", "Area1"),
                "View1",
                isPartial: false);
            viewLocationExpanderContext1.Values = new Dictionary<string, string>
            {
                { "somekey", value1 }
            };

            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               GetActionContext("Controller1", "Area1"),
               "View1",
               isPartial: false);
            viewLocationExpanderContext2.Values = new Dictionary<string, string>
            {
                { "somekey", value2 },
            };

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.False(result);
            Assert.NotEqual(hash1, hash2);
        }

        public void ViewLocationCacheKeyComparer_EqualsReturnsTrueIfValuesAreSame()
        {
            // Arrange
            var viewLocationExpanderContext1 = new ViewLocationExpanderContext(
                GetActionContext("Controller1", "Area1"),
                "View1",
                isPartial: false);
            viewLocationExpanderContext1.Values = new Dictionary<string, string>
            {
                { "somekey1", "value1" },
                { "somekey2", "value2" },
            };

            var viewLocationExpanderContext2 = new ViewLocationExpanderContext(
               GetActionContext("Controller1", "Area1"),
               "View1",
               isPartial: false);
            viewLocationExpanderContext2.Values = new Dictionary<string, string>
            {
                { "somekey2", "value2" },
                { "somekey1", "value1" },
            };

            // Act
            var key1 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext1,
                copyViewExpanderValues: false);

            var key2 = DefaultViewLocationCache.GenerateKey(
                viewLocationExpanderContext2,
                copyViewExpanderValues: false);

            var result = CacheKeyComparer.Equals(key1, key2);
            var hash1 = CacheKeyComparer.GetHashCode(key1);
            var hash2 = CacheKeyComparer.GetHashCode(key2);

            // Assert
            Assert.True(result);
            Assert.Equal(hash1, hash2);
        }

        public static ActionContext GetActionContext(
            string controller = "mycontroller",
            string area = null)
        {
            var routeData = new RouteData();
            routeData.Values["controller"] = controller;
            if (area != null)
            {
                routeData.Values["area"] = area;
            }

            var actionDesciptor = new ActionDescriptor();
            actionDesciptor.RouteConstraints = new List<RouteDataActionConstraint>();
            return new ActionContext(new DefaultHttpContext(), routeData, actionDesciptor);
        }

        private static ActionContext GetActionContextWithActionDescriptor(
            IDictionary<string, object> routeValues,
            IDictionary<string, string> routesInActionDescriptor,
            bool isAttributeRouted)
        {
            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            foreach (var kvp in routeValues)
            {
                routeData.Values.Add(kvp.Key, kvp.Value);
            }

            var actionDescriptor = new ActionDescriptor();
            if (isAttributeRouted)
            {
                actionDescriptor.AttributeRouteInfo = new Routing.AttributeRouteInfo();
                foreach (var kvp in routesInActionDescriptor)
                {
                    actionDescriptor.RouteValueDefaults.Add(kvp.Key, kvp.Value);
                }
            }
            else
            {
                actionDescriptor.RouteConstraints = new List<RouteDataActionConstraint>();
                foreach (var kvp in routesInActionDescriptor)
                {
                    actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(kvp.Key, kvp.Value));
                }
            }

            return new ActionContext(httpContext, routeData, actionDescriptor);
        }
    }
}