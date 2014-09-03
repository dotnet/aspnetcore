// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.PipelineCore;
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
                yield return new[] { new ViewLocationExpanderContext(GetActionContext(), "test") };

                var areaActionContext = GetActionContext("controller2", "myarea");
                yield return new[] { new ViewLocationExpanderContext(areaActionContext, "test2") };

                var actionContext = GetActionContext("controller3", "area3");
                var values = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "culture", "fr" },
                    { "theme", "sleek" }
                };
                var expanderContext = new ViewLocationExpanderContext(actionContext, "test3")
                {
                    Values = values
                };

                yield return new [] { expanderContext };
            }
        }

        [Theory]
        [MemberData(nameof(CacheEntryData))]
        public void Get_GeneratesCacheKeyIfItemDoesNotExist(ViewLocationExpanderContext context)
        {
            // Arrange
            var cache = new DefaultViewLocationCache();

            // Act
            var result = cache.Get(context);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [MemberData(nameof(CacheEntryData))]
        public void InvokingGetAfterSet_ReturnsCachedItem(ViewLocationExpanderContext context)
        {
            // Arrange
            var cache = new DefaultViewLocationCache();
            var value = Guid.NewGuid().ToString();

            // Act
            cache.Set(context, value);
            var result = cache.Get(context);

            // Assert
            Assert.Equal(value, result);
        }

        public static IEnumerable<object[]> CacheKeyData
        {
            get
            {
                yield return new object[]
                {
                    new ViewLocationExpanderContext(GetActionContext(), "test"),
                    "test:mycontroller"
                };

                var areaActionContext = GetActionContext("controller2", "myarea");
                yield return new object[]
                {
                    new ViewLocationExpanderContext(areaActionContext, "test2"),
                    "test2:controller2:myarea"
                };

                var actionContext = GetActionContext("controller3", "area3");
                var values = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "culture", "fr" },
                    { "theme", "sleek" }
                };
                var expanderContext = new ViewLocationExpanderContext(actionContext, "test3")
                {
                    Values = values
                };

                yield return new object[]
                {
                    expanderContext,
                    "test3:controller3:area3:culture:fr:theme:sleek"
                };
            }
        }

        [Theory]
        [MemberData(nameof(CacheKeyData))]
        public void CacheKeyIsComputedBasedOnValuesInExpander(ViewLocationExpanderContext context, string expected)
        {
            // Act
            var result = DefaultViewLocationCache.GenerateKey(context);

            // Assert
            Assert.Equal(expected, result);
        }

        public static ActionContext GetActionContext(string controller = "mycontroller",
                                                     string area = null)
        {
            var routeData = new RouteData
            {
                Values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            };
            routeData.Values["controller"] = controller;
            if (area != null)
            {
                routeData.Values["area"] = area;
            }

            return new ActionContext(new DefaultHttpContext(), routeData, new ActionDescriptor());
        }
    }
}