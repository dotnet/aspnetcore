// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ResponseCacheAttributeTest
    {
        [Fact]
        public void OnActionExecuting_DoesNotThrow_WhenNoStoreIsTrue()
        {
            // Arrange
            var cache = new ResponseCacheAttribute() { NoStore = true };
            var context = GetActionExecutingContext(new List<IFilter> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal("no-store", context.HttpContext.Response.Headers.Get("Cache-control"));
        }

        [Fact]
        public void OnActionExecuting_ThrowsIfDurationIsNotSet_WhenNoStoreIsFalse()
        {
            // Arrange
            var cache = new ResponseCacheAttribute() { NoStore = false };
            var context = GetActionExecutingContext(new List<IFilter> { cache });

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => { cache.OnActionExecuting(context); });
            Assert.Equal("If the 'NoStore' property is not set to true, 'Duration' property must be specified.",
                exception.Message);
        }

        public static IEnumerable<object[]> CacheControlData
        {
            get
            {
                yield return new object[] { new ResponseCacheAttribute { NoStore = true, Duration = 0 }, "no-store" };
                // If no-store is set, then location is ignored.
                yield return new object[] {
                    new ResponseCacheAttribute
                        { NoStore = true, Duration = 0, Location = ResponseCacheLocation.Client },
                    "no-store"
                };
                yield return new object[] {
                    new ResponseCacheAttribute { NoStore = true, Duration = 0, Location = ResponseCacheLocation.Any },
                    "no-store"
                };
                // If no-store is set, then duration is ignored.
                yield return new object[] {
                    new ResponseCacheAttribute { NoStore = true, Duration = 100 }, "no-store"
                };

                yield return new object[] {
                    new ResponseCacheAttribute { Location = ResponseCacheLocation.Client, Duration = 10 },
                    "private,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheAttribute { Location = ResponseCacheLocation.Any, Duration = 10 },
                    "public,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheAttribute { Location = ResponseCacheLocation.None, Duration = 10 },
                    "no-cache,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheAttribute { Location = ResponseCacheLocation.Client, Duration = 10 },
                    "private,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheAttribute { Location = ResponseCacheLocation.Any, Duration = 31536000 },
                    "public,max-age=31536000"
                };
                yield return new object[] {
                    new ResponseCacheAttribute { Duration = 20 },
                    "public,max-age=20"
                };
            }
        }

        [Theory]
        [MemberData(nameof(CacheControlData))]
        public void OnActionExecuting_CanSetCacheControlHeaders(ResponseCacheAttribute cache, string output)
        {
            // Arrange
            var context = GetActionExecutingContext(new List<IFilter> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal(output, context.HttpContext.Response.Headers.Get("Cache-control"));
        }

        public static IEnumerable<object[]> NoStoreData
        {
            get
            {
                // If no-store is set, then location is ignored.
                yield return new object[] {
                    new ResponseCacheAttribute
                    { NoStore = true, Location = ResponseCacheLocation.Client, Duration = 0 },
                    "no-store"
                };
                yield return new object[] {
                    new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.Any, Duration = 0 },
                    "no-store"
                };
                // If no-store is set, then duration is ignored.
                yield return new object[] {
                    new ResponseCacheAttribute { NoStore = true, Duration = 100 }, "no-store"
                };
            }
        }

        [Theory]
        [MemberData(nameof(NoStoreData))]
        public void OnActionExecuting_DoesNotSetLocationOrDuration_IfNoStoreIsSet(
            ResponseCacheAttribute cache, string output)
        {
            // Arrange
            var context = GetActionExecutingContext(new List<IFilter> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal(output, context.HttpContext.Response.Headers.Get("Cache-control"));
        }

        public static IEnumerable<object[]> VaryData
        {
            get
            {
                yield return new object[] {
                    new ResponseCacheAttribute { VaryByHeader = "Accept", Duration = 10 },
                    "Accept",
                    "public,max-age=10" };
                yield return new object[] {
                    new ResponseCacheAttribute { VaryByHeader = "Accept", NoStore = true, Duration = 0 },
                    "Accept",
                    "no-store"
                };
                yield return new object[] {
                    new ResponseCacheAttribute {
                        Location = ResponseCacheLocation.Client, Duration = 10, VaryByHeader = "Accept"
                    },
                    "Accept",
                    "private,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheAttribute {
                        Location = ResponseCacheLocation.Any, Duration = 10, VaryByHeader = "Test"
                    },
                    "Test",
                    "public,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheAttribute {
                        Location = ResponseCacheLocation.Client, Duration = 10, VaryByHeader = "Test"
                    },
                    "Test",
                    "private,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheAttribute {
                        Location = ResponseCacheLocation.Any,
                        Duration = 31536000,
                        VaryByHeader = "Test"
                    },
                    "Test",
                    "public,max-age=31536000"
                };
            }
        }

        [Theory]
        [MemberData(nameof(VaryData))]
        public void ResponseCacheCanSetVary(ResponseCacheAttribute cache, string varyOutput, string cacheControlOutput)
        {
            // Arrange
            var context = GetActionExecutingContext(new List<IFilter> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal(varyOutput, context.HttpContext.Response.Headers.Get("Vary"));
            Assert.Equal(cacheControlOutput, context.HttpContext.Response.Headers.Get("Cache-control"));
        }

        [Fact]
        public void SetsPragmaOnNoCache()
        {
            // Arrange
            var cache = new ResponseCacheAttribute()
                            {
                                NoStore = true,
                                Location = ResponseCacheLocation.None,
                                Duration = 0
                            };
            var context = GetActionExecutingContext(new List<IFilter> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal("no-store,no-cache", context.HttpContext.Response.Headers.Get("Cache-control"));
            Assert.Equal("no-cache", context.HttpContext.Response.Headers.Get("Pragma"));
        }

        [Fact]
        public void IsOverridden_ReturnsTrueForAllButLastFilter()
        {
            // Arrange
            var caches = new List<IFilter>();
            caches.Add(new ResponseCacheAttribute());
            caches.Add(new ResponseCacheAttribute());

            var context = GetActionExecutingContext(caches);

            // Act & Assert
            Assert.True((caches[0] as ResponseCacheAttribute).IsOverridden(context));
            Assert.False((caches[1] as ResponseCacheAttribute).IsOverridden(context));
        }

        private ActionExecutingContext GetActionExecutingContext(List<IFilter> filters = null)
        {
            return new ActionExecutingContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                filters ?? new List<IFilter>(),
                new Dictionary<string, object>(),
                new object());
        }
    }
}