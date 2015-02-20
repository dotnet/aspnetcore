// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Core;
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
                    new ViewLocationExpanderContext(GetActionContext(), "test", isPartial: false),
                    "test:0:mycontroller"
                };

                yield return new object[]
                {
                    new ViewLocationExpanderContext(GetActionContext(), "test", isPartial: true),
                    "test:1:mycontroller"
                };

                var areaActionContext = GetActionContext("controller2", "myarea");
                yield return new object[]
                {
                    new ViewLocationExpanderContext(areaActionContext, "test2", isPartial: false),
                    "test2:0:controller2:myarea"
                };
                yield return new object[]
                {
                    new ViewLocationExpanderContext(areaActionContext, "test2", isPartial: true),
                    "test2:1:controller2:myarea"
                };

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

                yield return new object[]
                {
                    expanderContext,
                    "test3:0:controller3:area3:culture:fr:theme:sleek"
                };

                expanderContext = new ViewLocationExpanderContext(actionContext, "test3", isPartial: true)
                {
                    Values = values
                };
                yield return new object[]
                {
                    expanderContext,
                    "test3:1:controller3:area3:culture:fr:theme:sleek"
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
            var routeData = new RouteData();
            routeData.Values["controller"] = controller;
            if (area != null)
            {
                routeData.Values["area"] = area;
            }

            return new ActionContext(new DefaultHttpContext(), routeData, new ActionDescriptor());
        }
    }
}