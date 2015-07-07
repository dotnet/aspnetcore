// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.OptionsModel;
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
            var responseCache = new ResponseCacheAttribute() {
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

        private IServiceProvider GetServiceProvider(Dictionary<string, CacheProfile> cacheProfiles)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var optionsAccessor = new Mock<IOptions<MvcCacheOptions>>();
            var options = new MvcCacheOptions();
            if (cacheProfiles != null)
            {
                foreach (var p in cacheProfiles)
                {
                    options.CacheProfiles.Add(p.Key, p.Value);
                }
            }

            optionsAccessor.SetupGet(o => o.Options).Returns(options);
            serviceProvider
                .Setup(s => s.GetService(typeof(IOptions<MvcCacheOptions>)))
                .Returns(optionsAccessor.Object);

            return serviceProvider.Object;
        }
    }
}