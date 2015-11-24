// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ResponseCacheAttributeTest
    {
        [Theory]
        [InlineData("Cache20Sec")]
        // To verify case-insensitive lookup.
        [InlineData("cache20sec")]
        public void CreateInstance_SelectsTheAppropriateCacheProfile(string profileName)
        {
            // Arrange
            var responseCache = new ResponseCacheAttribute()
            {
                CacheProfileName = profileName
            };
            var cacheProfiles = new Dictionary<string, CacheProfile>();
            cacheProfiles.Add("Cache20Sec", new CacheProfile { NoStore = true });
            cacheProfiles.Add("Test", new CacheProfile { Duration = 20 });

            // Act
            var createdFilter = responseCache.CreateInstance(GetServiceProvider(cacheProfiles));

            // Assert
            var responseCacheFilter = Assert.IsType<ResponseCacheFilter>(createdFilter);
            Assert.True(responseCacheFilter.NoStore);
        }

        [Fact]
        public void CreateInstance_ThrowsIfThereAreNoMatchingCacheProfiles()
        {
            // Arrange
            var responseCache = new ResponseCacheAttribute()
            {
                CacheProfileName = "HelloWorld"
            };
            var cacheProfiles = new Dictionary<string, CacheProfile>();
            cacheProfiles.Add("Cache20Sec", new CacheProfile { NoStore = true });
            cacheProfiles.Add("Test", new CacheProfile { Duration = 20 });

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => responseCache.CreateInstance(GetServiceProvider(cacheProfiles)));
            Assert.Equal("The 'HelloWorld' cache profile is not defined.", ex.Message);
        }

        public static IEnumerable<object[]> OverrideData
        {
            get
            {
                // When there are no cache profiles then the passed in data is returned unchanged
                yield return new object[] {
                    new ResponseCacheAttribute()
                    { Duration = 20, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = "Accept" },
                    null,
                    new CacheProfile
                    { Duration = 20, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = "Accept" }
                };

                yield return new object[] {
                    new ResponseCacheAttribute()
                    { Duration = 0, Location = ResponseCacheLocation.None, NoStore = true, VaryByHeader = null },
                    null,
                    new CacheProfile
                    { Duration = 0, Location = ResponseCacheLocation.None, NoStore = true, VaryByHeader = null }
                };

                // Everything gets overriden if attribute parameters are present,
                // when a particular cache profile is chosen.
                yield return new object[] {
                    new ResponseCacheAttribute()
                    {
                        Duration = 20,
                        Location = ResponseCacheLocation.Any,
                        NoStore = false,
                        VaryByHeader = "Accept",
                        CacheProfileName = "TestCacheProfile"
                    },
                    new Dictionary<string, CacheProfile> { { "TestCacheProfile", new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Client,
                            NoStore = true,
                            VaryByHeader = "Test"
                        } } },
                    new CacheProfile
                    { Duration = 20, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = "Accept" }
                };

                // Select parameters override the selected profile.
                yield return new object[] {
                    new ResponseCacheAttribute()
                    {
                        Duration = 534,
                        CacheProfileName = "TestCacheProfile"
                    },
                    new Dictionary<string, CacheProfile>() { { "TestCacheProfile", new CacheProfile
                        {
                            Duration = 10,
                            Location = ResponseCacheLocation.Client,
                            NoStore = false,
                            VaryByHeader = "Test"
                        } } },
                    new CacheProfile
                    { Duration = 534, Location = ResponseCacheLocation.Client, NoStore = false, VaryByHeader = "Test" }
                };

                // Duration parameter gets added to the selected profile.
                yield return new object[] {
                    new ResponseCacheAttribute()
                    {
                        Duration = 534,
                        CacheProfileName = "TestCacheProfile"
                    },
                    new Dictionary<string, CacheProfile>() { { "TestCacheProfile", new CacheProfile
                        {
                            Location = ResponseCacheLocation.Client,
                            NoStore = false,
                            VaryByHeader = "Test"
                        } } },
                    new CacheProfile
                    { Duration = 534, Location = ResponseCacheLocation.Client, NoStore = false, VaryByHeader = "Test" }
                };

                // Default values gets added for parameters which are absent
                yield return new object[] {
                    new ResponseCacheAttribute()
                    {
                        Duration = 5234,
                        CacheProfileName = "TestCacheProfile"
                    },
                    new Dictionary<string, CacheProfile>() { { "TestCacheProfile", new CacheProfile() } },
                    new CacheProfile
                    { Duration = 5234, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = null }
                };
            }
        }

        [Theory]
        [MemberData(nameof(OverrideData))]
        public void CreateInstance_HonorsOverrides(
            ResponseCacheAttribute responseCache,
            Dictionary<string, CacheProfile> cacheProfiles,
            CacheProfile expectedProfile)
        {
            // Arrange & Act
            var createdFilter = responseCache.CreateInstance(GetServiceProvider(cacheProfiles));

            // Assert
            var responseCacheFilter = Assert.IsType<ResponseCacheFilter>(createdFilter);
            Assert.Equal(expectedProfile.Duration, responseCacheFilter.Duration);
            Assert.Equal(expectedProfile.Location, responseCacheFilter.Location);
            Assert.Equal(expectedProfile.NoStore, responseCacheFilter.NoStore);
            Assert.Equal(expectedProfile.VaryByHeader, responseCacheFilter.VaryByHeader);
        }

        [Fact]
        public void CreateInstance_DoesNotThrowWhenTheDurationIsNotSet_WithNoStoreFalse()
        {
            // Arrange
            var responseCache = new ResponseCacheAttribute()
            {
                CacheProfileName = "Test"
            };
            var cacheProfiles = new Dictionary<string, CacheProfile>();
            cacheProfiles.Add("Test", new CacheProfile { NoStore = false });

            // Act
            var filter = responseCache.CreateInstance(GetServiceProvider(cacheProfiles));

            // Assert
            Assert.NotNull(filter);
        }

        [Fact]
        public void ResponseCache_SetsAllHeaders()
        {
            // Arrange
            var responseCache = new ResponseCacheAttribute()
            {
                Duration = 100,
                Location = ResponseCacheLocation.Any,
                VaryByHeader = "Accept"
            };
            var filter = (ResponseCacheFilter)responseCache.CreateInstance(GetServiceProvider(cacheProfiles: null));
            var context = GetActionExecutingContext(filter);

            // Act
            filter.OnActionExecuting(context);

            // Assert
            var response = context.HttpContext.Response;
            StringValues values;
            Assert.True(response.Headers.TryGetValue("Cache-Control", out values));
            var data = Assert.Single(values);
            AssertHeaderEquals("public, max-age=100", data);
            Assert.True(response.Headers.TryGetValue("Vary", out values));
            data = Assert.Single(values);
            Assert.Equal("Accept", data);
        }

        public static TheoryData<ResponseCacheAttribute, string> CacheControlData
        {
            get
            {
                return new TheoryData<ResponseCacheAttribute, string>
                {
                    {
                        new ResponseCacheAttribute() { Duration = 100, Location = ResponseCacheLocation.Any },
                        "public, max-age=100"
                    },
                    {
                         new ResponseCacheAttribute() { Duration = 100, Location = ResponseCacheLocation.Client },
                        "max-age=100, private"
                    },
                    {
                        new ResponseCacheAttribute() { NoStore = true, Duration = 0 },
                        "no-store"
                    },
                    {
                        new ResponseCacheAttribute()
                    {
                        NoStore = true,
                        Duration = 0,
                        Location = ResponseCacheLocation.None
                    },
                    "no-store, no-cache"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(CacheControlData))]
        public void ResponseCache_SetsDifferentCacheControlHeaders(
            ResponseCacheAttribute responseCacheAttribute,
            string expected)
        {
            // Arrange
            var filter = (ResponseCacheFilter)responseCacheAttribute.CreateInstance(
                GetServiceProvider(cacheProfiles: null));
            var context = GetActionExecutingContext(filter);

            // Act
            filter.OnActionExecuting(context);

            // Assert
            StringValues values;
            Assert.True(context.HttpContext.Response.Headers.TryGetValue("Cache-Control", out values));
            var data = Assert.Single(values);
            AssertHeaderEquals(expected, data);
        }

        [Fact]
        public void SetsCacheControlPublicByDefault()
        {
            // Arrange
            var responseCacheAttribute = new ResponseCacheAttribute() { Duration = 40 };
            var filter = (ResponseCacheFilter)responseCacheAttribute.CreateInstance(
                GetServiceProvider(cacheProfiles: null));
            var context = GetActionExecutingContext(filter);

            // Act
            filter.OnActionExecuting(context);

            // Assert
            StringValues values;
            Assert.True(context.HttpContext.Response.Headers.TryGetValue("Cache-Control", out values));
            var data = Assert.Single(values);
            AssertHeaderEquals("public, max-age=40", data);
        }

        [Fact]
        public void ThrowsWhenDurationIsNotSet()
        {
            // Arrange
            var responseCacheAttribute = new ResponseCacheAttribute()
            {
                VaryByHeader = "Accept"
            };
            var filter = (ResponseCacheFilter)responseCacheAttribute.CreateInstance(
                GetServiceProvider(cacheProfiles: null));
            var context = GetActionExecutingContext(filter);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                filter.OnActionExecuting(context);
            });
            Assert.Equal(
                "If the 'NoStore' property is not set to true, 'Duration' property must be specified.",
                exception.Message);
        }

        private IServiceProvider GetServiceProvider(Dictionary<string, CacheProfile> cacheProfiles)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var optionsAccessor = new TestOptionsManager<MvcOptions>();
            if (cacheProfiles != null)
            {
                foreach (var p in cacheProfiles)
                {
                    optionsAccessor.Value.CacheProfiles.Add(p.Key, p.Value);
                }
            }

            serviceProvider
                .Setup(s => s.GetService(typeof(IOptions<MvcOptions>)))
                .Returns(optionsAccessor);

            return serviceProvider.Object;
        }

        private ActionExecutingContext GetActionExecutingContext(params IFilterMetadata[] filters)
        {
            return new ActionExecutingContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                filters?.ToList() ?? new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new object());
        }

        private void AssertHeaderEquals(string expected, string actual)
        {
            // OrderBy is used because the order of the results may vary depending on the platform / client.
            Assert.Equal(
                expected.Split(',').Select(p => p.Trim()).OrderBy(item => item, StringComparer.Ordinal),
                actual.Split(',').Select(p => p.Trim()).OrderBy(item => item, StringComparer.Ordinal));
        }
    }
}