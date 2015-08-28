// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ResponseCacheFilterTest
    {
        [Fact]
        public void ResponseCacheFilter_DoesNotThrow_WhenNoStoreIsTrue()
        {
            // Arrange
            var cache = new ResponseCacheFilter(
                new CacheProfile
                {
                    NoStore = true,
                    Duration = null
                });
            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal("no-store", context.HttpContext.Response.Headers["Cache-control"]);
        }

        [Fact]
        public void ResponseCacheFilter_DoesNotThrowIfDurationIsNotSet_WhenNoStoreIsFalse()
        {
            // Arrange, Act
            var cache = new ResponseCacheFilter(
                new CacheProfile
                {
                    Duration = null
                });

            // Assert
            Assert.NotNull(cache);
        }

        [Fact]
        public void OnActionExecuting_ThrowsIfDurationIsNotSet_WhenNoStoreIsFalse()
        {
            // Arrange
            var cache = new ResponseCacheFilter(
                new CacheProfile()
                {
                    Duration = null
                });

            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => cache.OnActionExecuting(context));
            Assert.Equal("If the 'NoStore' property is not set to true, 'Duration' property must be specified.",
                ex.Message);
        }

        public static IEnumerable<object[]> CacheControlData
        {
            get
            {
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 0, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                        }),
                    "no-store"
                };
                // If no-store is set, then location is ignored.
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 0, Location = ResponseCacheLocation.Client, NoStore = true, VaryByHeader = null
                        }),
                    "no-store"
                };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 0, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                        }),
                    "no-store"
                };
                // If no-store is set, then duration is ignored.
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 100, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                        }),
                    "no-store"
                };

                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 10, Location = ResponseCacheLocation.Client,
                            NoStore = false, VaryByHeader = null
                        }),
                    "private,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 10, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = null
                        }),
                    "public,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 10, Location = ResponseCacheLocation.None, NoStore = false, VaryByHeader = null
                        }),
                    "no-cache,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 31536000, Location = ResponseCacheLocation.Any,
                            NoStore = false, VaryByHeader = null
                        }),
                    "public,max-age=31536000"
                };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 20, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = null
                        }),
                    "public,max-age=20"
                };
            }
        }

        [Theory]
        [MemberData(nameof(CacheControlData))]
        public void OnActionExecuting_CanSetCacheControlHeaders(ResponseCacheFilter cache, string output)
        {
            // Arrange
            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal(output, context.HttpContext.Response.Headers["Cache-control"]);
        }

        public static IEnumerable<object[]> NoStoreData
        {
            get
            {
                // If no-store is set, then location is ignored.
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 0, Location = ResponseCacheLocation.Client, NoStore = true, VaryByHeader = null
                        }),
                    "no-store"
                };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 0, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                        }),
                    "no-store"
                };
                // If no-store is set, then duration is ignored.
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 100, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                        }),
                    "no-store"
                };
            }
        }

        [Theory]
        [MemberData(nameof(NoStoreData))]
        public void OnActionExecuting_DoesNotSetLocationOrDuration_IfNoStoreIsSet(
            ResponseCacheFilter cache, string output)
        {
            // Arrange
            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal(output, context.HttpContext.Response.Headers["Cache-control"]);
        }

        public static IEnumerable<object[]> VaryData
        {
            get
            {
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 10, Location = ResponseCacheLocation.Any,
                            NoStore = false, VaryByHeader = "Accept"
                        }),
                    "Accept",
                    "public,max-age=10" };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 0, Location= ResponseCacheLocation.Any,
                            NoStore = true, VaryByHeader = "Accept"
                        }),
                    "Accept",
                    "no-store"
                };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 10, Location = ResponseCacheLocation.Client,
                            NoStore = false, VaryByHeader = "Accept"
                        }),
                    "Accept",
                    "private,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 10, Location = ResponseCacheLocation.Client,
                            NoStore = false, VaryByHeader = "Test"
                        }),
                    "Test",
                    "private,max-age=10"
                };
                yield return new object[] {
                    new ResponseCacheFilter(
                        new CacheProfile
                        {
                            Duration = 31536000, Location = ResponseCacheLocation.Any,
                            NoStore = false, VaryByHeader = "Test"
                        }),
                    "Test",
                    "public,max-age=31536000"
                };
            }
        }

        [Theory]
        [MemberData(nameof(VaryData))]
        public void ResponseCacheCanSetVary(ResponseCacheFilter cache, string varyOutput, string cacheControlOutput)
        {
            // Arrange
            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal(varyOutput, context.HttpContext.Response.Headers["Vary"]);
            Assert.Equal(cacheControlOutput, context.HttpContext.Response.Headers["Cache-control"]);
        }

        [Fact]
        public void SetsPragmaOnNoCache()
        {
            // Arrange
            var cache = new ResponseCacheFilter(
                new CacheProfile
                {
                    Duration = 0, Location = ResponseCacheLocation.None, NoStore = true, VaryByHeader = null
                });
            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal("no-store,no-cache", context.HttpContext.Response.Headers["Cache-control"]);
            Assert.Equal("no-cache", context.HttpContext.Response.Headers["Pragma"]);
        }

        [Fact]
        public void IsOverridden_ReturnsTrueForAllButLastFilter()
        {
            // Arrange
            var caches = new List<IFilterMetadata>();
            caches.Add(new ResponseCacheFilter(
                new CacheProfile
                {
                    Duration = 0, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = null
                }));
            caches.Add(new ResponseCacheFilter(
                new CacheProfile
                {
                    Duration = 0, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = null
                }));

            var context = GetActionExecutingContext(caches);

            // Act & Assert
            var cache = Assert.IsType<ResponseCacheFilter>(caches[0]);
            Assert.True(cache.IsOverridden(context));
            cache = Assert.IsType<ResponseCacheFilter>(caches[1]);
            Assert.False(cache.IsOverridden(context));
        }

        [Fact]
        public void FilterDurationProperty_OverridesCachePolicySetting()
        {
            // Arrange
            var cache = new ResponseCacheFilter(
                new CacheProfile
                {
                    Duration = 10
                });
            cache.Duration = 20;
            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal("public,max-age=20", context.HttpContext.Response.Headers["Cache-control"]);
        }

        [Fact]
        public void FilterLocationProperty_OverridesCachePolicySetting()
        {
            // Arrange
            var cache = new ResponseCacheFilter(
                new CacheProfile
                {
                    Duration = 10,
                    Location = ResponseCacheLocation.None
                });
            cache.Location = ResponseCacheLocation.Client;
            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal("private,max-age=10", context.HttpContext.Response.Headers["Cache-control"]);
        }

        [Fact]
        public void FilterNoStoreProperty_OverridesCachePolicySetting()
        {
            // Arrange
            var cache = new ResponseCacheFilter(
                new CacheProfile
                {
                    NoStore = true
                });
            cache.NoStore = false;
            cache.Duration = 10;
            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal("public,max-age=10", context.HttpContext.Response.Headers["Cache-control"]);
        }

        [Fact]
        public void FilterVaryByProperty_OverridesCachePolicySetting()
        {
            // Arrange
            var cache = new ResponseCacheFilter(
                new CacheProfile
                {
                    NoStore = true,
                    VaryByHeader = "Accept"
                });
            cache.VaryByHeader = "Test";
            var context = GetActionExecutingContext(new List<IFilterMetadata> { cache });

            // Act
            cache.OnActionExecuting(context);

            // Assert
            Assert.Equal("Test", context.HttpContext.Response.Headers["Vary"]);
        }

        private ActionExecutingContext GetActionExecutingContext(List<IFilterMetadata> filters = null)
        {
            return new ActionExecutingContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                filters ?? new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object());
        }
    }
}