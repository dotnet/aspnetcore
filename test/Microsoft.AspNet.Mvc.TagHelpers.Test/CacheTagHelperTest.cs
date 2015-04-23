// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Caching.Memory.Infrastructure;
using Microsoft.Framework.Expiration.Interfaces;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class CacheTagHelperTest
    {
        [Fact]
        public void GenerateKey_ReturnsKeyBasedOnTagHelperUniqueId()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var tagHelperContext = GetTagHelperContext(id);
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext()
            };
            var expected = GetHashedBytes("CacheTagHelper||" + id);

            // Act
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Assert
            Assert.Equal(expected, key);
        }

        [Theory]
        [InlineData("Vary-By-Value")]
        [InlineData("Vary  with spaces")]
        [InlineData("  Vary  with more spaces   ")]
        public void GenerateKey_UsesVaryByPropertyToGenerateKey(string varyBy)
        {
            // Arrange
            var tagHelperContext = GetTagHelperContext();
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                VaryBy = varyBy
            };
            var expected = GetHashedBytes("CacheTagHelper||testid||VaryBy||" + varyBy);

            // Act
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Assert
            Assert.Equal(expected, key);
        }

        [Theory]
        [InlineData("Cookie0", "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value)")]
        [InlineData("Cookie0,Cookie1",
                    "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value||Cookie1||Cookie1Value)")]
        [InlineData("Cookie0, Cookie1",
                    "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value||Cookie1||Cookie1Value)")]
        [InlineData("   Cookie0,   ,   Cookie1   ",
                    "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value||Cookie1||Cookie1Value)")]
        [InlineData(",Cookie0,,Cookie1,",
                    "CacheTagHelper||testid||VaryByCookie(Cookie0||Cookie0Value||Cookie1||Cookie1Value)")]
        public void GenerateKey_UsesVaryByCookieName(string varyByCookie, string expected)
        {
            // Arrange
            var tagHelperContext = GetTagHelperContext();
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                VaryByCookie = varyByCookie
            };
            cacheTagHelper.ViewContext.HttpContext.Request.Headers["Cookie"] =
                "Cookie0=Cookie0Value;Cookie1=Cookie1Value";

            // Act
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Assert
            Assert.Equal(GetHashedBytes(expected), key);
        }

        [Theory]
        [InlineData("Accept-Language", "CacheTagHelper||testid||VaryByHeader(Accept-Language||en-us;charset=utf8)")]
        [InlineData("X-CustomHeader,Accept-Encoding, NotAvailable",
         "CacheTagHelper||testid||VaryByHeader(X-CustomHeader||Header-Value||Accept-Encoding||utf8||NotAvailable||)")]
        [InlineData("X-CustomHeader,  , Accept-Encoding, NotAvailable",
         "CacheTagHelper||testid||VaryByHeader(X-CustomHeader||Header-Value||Accept-Encoding||utf8||NotAvailable||)")]
        public void GenerateKey_UsesVaryByHeader(string varyByHeader, string expected)
        {
            // Arrange
            var tagHelperContext = GetTagHelperContext();
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                VaryByHeader = varyByHeader
            };
            var headers = cacheTagHelper.ViewContext.HttpContext.Request.Headers;
            headers["Accept-Language"] = "en-us;charset=utf8";
            headers["Accept-Encoding"] = "utf8";
            headers["X-CustomHeader"] = "Header-Value";

            // Act
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Assert
            Assert.Equal(GetHashedBytes(expected), key);
        }

        [Theory]
        [InlineData("category", "CacheTagHelper||testid||VaryByQuery(category||cats)")]
        [InlineData("Category,SortOrder,SortOption",
            "CacheTagHelper||testid||VaryByQuery(Category||cats||SortOrder||||SortOption||Adorability)")]
        [InlineData("Category,  SortOrder, SortOption,  ",
            "CacheTagHelper||testid||VaryByQuery(Category||cats||SortOrder||||SortOption||Adorability)")]
        public void GenerateKey_UsesVaryByQuery(string varyByQuery, string expected)
        {
            // Arrange
            var tagHelperContext = GetTagHelperContext();
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                VaryByQuery = varyByQuery
            };
            cacheTagHelper.ViewContext.HttpContext.Request.QueryString =
                new Http.QueryString("?sortoption=Adorability&Category=cats&sortOrder=");

            // Act
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Assert
            Assert.Equal(GetHashedBytes(expected), key);
        }

        [Theory]
        [InlineData("id", "CacheTagHelper||testid||VaryByRoute(id||4)")]
        [InlineData("Category,,Id,OptionRouteValue",
            "CacheTagHelper||testid||VaryByRoute(Category||MyCategory||Id||4||OptionRouteValue||)")]
        [InlineData(" Category,  , Id,   OptionRouteValue,   ",
            "CacheTagHelper||testid||VaryByRoute(Category||MyCategory||Id||4||OptionRouteValue||)")]
        public void GenerateKey_UsesVaryByRoute(string varyByRoute, string expected)
        {
            // Arrange
            var tagHelperContext = GetTagHelperContext();
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                VaryByRoute = varyByRoute
            };
            cacheTagHelper.ViewContext.RouteData.Values["id"] = 4;
            cacheTagHelper.ViewContext.RouteData.Values["category"] = "MyCategory";

            // Act
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Assert
            Assert.Equal(GetHashedBytes(expected), key);
        }

        [Fact]
        public void GenerateKey_UsesVaryByUser_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var expected = "CacheTagHelper||testid||VaryByUser||";
            var tagHelperContext = GetTagHelperContext();
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                VaryByUser = true
            };

            // Act
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Assert
            Assert.Equal(GetHashedBytes(expected), key);
        }

        [Fact]
        public void GenerateKey_UsesVaryByUserAndAuthenticatedUserName()
        {
            // Arrange
            var expected = "CacheTagHelper||testid||VaryByUser||test_name";
            var tagHelperContext = GetTagHelperContext();
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                VaryByUser = true
            };
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimsIdentity.DefaultNameClaimType, "test_name") });
            cacheTagHelper.ViewContext.HttpContext.User = new ClaimsPrincipal(identity);

            // Act
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Assert
            Assert.Equal(GetHashedBytes(expected), key);
        }

        [Fact]
        public void GenerateKey_WithMultipleVaryByOptions_CreatesCombinedKey()
        {
            // Arrange
            var expected = GetHashedBytes("CacheTagHelper||testid||VaryBy||custom-value||" +
                                          "VaryByHeader(content-type||text/html)||VaryByUser||someuser");
            var tagHelperContext = GetTagHelperContext();
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                VaryByUser = true,
                VaryByHeader = "content-type",
                VaryBy = "custom-value"
            };
            cacheTagHelper.ViewContext.HttpContext.Request.Headers["Content-Type"] = "text/html";
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimsIdentity.DefaultNameClaimType, "someuser") });
            cacheTagHelper.ViewContext.HttpContext.User = new ClaimsPrincipal(identity);

            // Act
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Assert
            Assert.Equal(expected, key);
        }

        [Fact]
        public async Task ProcessAsync_DoesNotCache_IfDisabled()
        {
            // Arrange
            var id = "unique-id";
            var childContent = "original-child-content";
            var cache = new Mock<IMemoryCache>();
            cache.CallBase = true;
            cache.Setup(c => c.Set(
                /*key*/ It.IsAny<string>(),
                /*link*/ It.IsAny<IEntryLink>(),
                /*state*/ It.IsAny<object>(),
                /*create*/ It.IsAny<Func<ICacheSetContext, object>>()))
                .Returns(new DefaultTagHelperContent().SetContent("ok"))
                .Verifiable();
            object cacheResult;
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), It.IsAny<IEntryLink>(), out cacheResult))
                .Returns(false);
            var tagHelperContext = GetTagHelperContext(id, childContent);
            var tagHelperOutput = new TagHelperOutput("cache", new TagHelperAttributeList());
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                MemoryCache = cache.Object,
                Enabled = false
            };

            // Act
            await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

            // Assert
            Assert.Equal(childContent, tagHelperOutput.Content.GetContent());
            cache.Verify(c => c.Set(
                /*key*/ It.IsAny<string>(),
                /*link*/ It.IsAny<IEntryLink>(),
                /*state*/ It.IsAny<object>(),
                /*create*/ It.IsAny<Func<ICacheSetContext, object>>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessAsync_ReturnsCachedValue_IfEnabled()
        {
            // Arrange
            var id = "unique-id";
            var childContent = "original-child-content";
            var cache = new Mock<IMemoryCache>();
            cache.CallBase = true;
            cache.Setup(c => c.Set(
                /*key*/ It.IsAny<string>(),
                /*link*/ It.IsAny<IEntryLink>(),
                /*state*/ It.IsAny<object>(),
                /*create*/ It.IsAny<Func<ICacheSetContext, object>>()))
                .Returns(new DefaultTagHelperContent().SetContent("ok"))
                .Verifiable();
            object cacheResult;
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), It.IsAny<IEntryLink>(), out cacheResult))
                .Returns(false);
            var tagHelperContext = GetTagHelperContext(id, childContent);
            var tagHelperOutput = new TagHelperOutput("cache", new TagHelperAttributeList());
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                MemoryCache = cache.Object,
                Enabled = true
            };

            // Act
            await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

            // Assert
            Assert.Empty(tagHelperOutput.PreContent.GetContent());
            Assert.Empty(tagHelperOutput.PostContent.GetContent());
            Assert.True(tagHelperOutput.IsContentModified);
            Assert.Equal(childContent, tagHelperOutput.Content.GetContent());
            cache.Verify(c => c.Set(
                /*key*/ It.IsAny<string>(),
                /*link*/ It.IsAny<IEntryLink>(),
                /*state*/ It.IsAny<object>(),
                /*create*/ It.IsAny<Func<ICacheSetContext, object>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_ReturnsCachedValue_IfVaryByParamIsUnchanged()
        {
            // Arrange - 1
            var id = "unique-id";
            var childContent = "original-child-content";
            var cache = new MemoryCache(new MemoryCacheOptions());
            var tagHelperContext1 = GetTagHelperContext(id, childContent);
            var tagHelperOutput1 = new TagHelperOutput("cache", new TagHelperAttributeList());
            var cacheTagHelper1 = new CacheTagHelper
            {
                VaryByQuery = "key1,key2",
                ViewContext = GetViewContext(),
                MemoryCache = cache
            };
            cacheTagHelper1.ViewContext.HttpContext.Request.QueryString = new Http.QueryString(
                "?key1=value1&key2=value2");

            // Act - 1
            await cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1);

            // Assert - 1
            Assert.Empty(tagHelperOutput1.PreContent.GetContent());
            Assert.Empty(tagHelperOutput1.PostContent.GetContent());
            Assert.True(tagHelperOutput1.IsContentModified);
            Assert.Equal(childContent, tagHelperOutput1.Content.GetContent());

            // Arrange - 2
            var tagHelperContext2 = GetTagHelperContext(id, "different-content");
            var tagHelperOutput2 = new TagHelperOutput("cache", new TagHelperAttributeList());
            var cacheTagHelper2 = new CacheTagHelper
            {
                VaryByQuery = "key1,key2",
                ViewContext = GetViewContext(),
                MemoryCache = cache
            };
            cacheTagHelper2.ViewContext.HttpContext.Request.QueryString = new Http.QueryString(
                "?key1=value1&key2=value2");

            // Act - 2
            await cacheTagHelper2.ProcessAsync(tagHelperContext2, tagHelperOutput2);

            // Assert - 2
            Assert.Empty(tagHelperOutput2.PreContent.GetContent());
            Assert.Empty(tagHelperOutput2.PostContent.GetContent());
            Assert.True(tagHelperOutput2.IsContentModified);
            Assert.Equal(childContent, tagHelperOutput2.Content.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_RecalculatesValueIfCacheKeyChanges()
        {
            // Arrange - 1
            var id = "unique-id";
            var childContent1 = "original-child-content";
            var cache = new MemoryCache(new MemoryCacheOptions());
            var tagHelperContext1 = GetTagHelperContext(id, childContent1);
            var tagHelperOutput1 = new TagHelperOutput("cache", new TagHelperAttributeList { { "attr", "value" } });
            tagHelperOutput1.PreContent.Append("<cache>");
            tagHelperOutput1.PostContent.SetContent("</cache>");
            var cacheTagHelper1 = new CacheTagHelper
            {
                VaryByCookie = "cookie1,cookie2",
                ViewContext = GetViewContext(),
                MemoryCache = cache
            };
            cacheTagHelper1.ViewContext.HttpContext.Request.Headers["Cookie"] = "cookie1=value1;cookie2=value2";

            // Act - 1
            await cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1);

            // Assert - 1
            Assert.Empty(tagHelperOutput1.PreContent.GetContent());
            Assert.Empty(tagHelperOutput1.PostContent.GetContent());
            Assert.True(tagHelperOutput1.IsContentModified);
            Assert.Equal(childContent1, tagHelperOutput1.Content.GetContent());

            // Arrange - 2
            var childContent2 = "different-content";
            var tagHelperContext2 = GetTagHelperContext(id, childContent2);
            var tagHelperOutput2 = new TagHelperOutput("cache", new TagHelperAttributeList { { "attr", "value" } });
            tagHelperOutput2.PreContent.SetContent("<cache>");
            tagHelperOutput2.PostContent.SetContent("</cache>");
            var cacheTagHelper2 = new CacheTagHelper
            {
                VaryByCookie = "cookie1,cookie2",
                ViewContext = GetViewContext(),
                MemoryCache = cache
            };
            cacheTagHelper2.ViewContext.HttpContext.Request.Headers["Cookie"] = "cookie1=value1;cookie2=not-value2";

            // Act - 2
            await cacheTagHelper2.ProcessAsync(tagHelperContext2, tagHelperOutput2);

            // Assert - 2
            Assert.Empty(tagHelperOutput2.PreContent.GetContent());
            Assert.Empty(tagHelperOutput2.PostContent.GetContent());
            Assert.True(tagHelperOutput2.IsContentModified);
            Assert.Equal(childContent2, tagHelperOutput2.Content.GetContent());
        }

        [Fact]
        public void UpdateCacheContext_SetsAbsoluteExpiration_IfExpiresOnIsSet()
        {
            // Arrange
            var expiresOn = DateTimeOffset.UtcNow.AddMinutes(4);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheContext = new Mock<ICacheSetContext>(MockBehavior.Strict);
            cacheContext.Setup(c => c.SetAbsoluteExpiration(expiresOn))
                        .Verifiable();
            var cacheTagHelper = new CacheTagHelper
            {
                MemoryCache = cache,
                ExpiresOn = expiresOn
            };

            // Act
            cacheTagHelper.UpdateCacheContext(cacheContext.Object, new EntryLink());

            // Assert
            cacheContext.Verify();
        }

        [Fact]
        public void UpdateCacheContext_UsesAbsoluteExpirationSpecifiedOnEntryLink()
        {
            // Arrange
            var expiresOn = DateTimeOffset.UtcNow.AddMinutes(7);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheContext = new Mock<ICacheSetContext>(MockBehavior.Strict);
            cacheContext.Setup(c => c.SetAbsoluteExpiration(expiresOn))
                        .Verifiable();
            var cacheTagHelper = new CacheTagHelper
            {
                MemoryCache = cache
            };

            var entryLink = new EntryLink();
            entryLink.SetAbsoluteExpiration(expiresOn);

            // Act
            cacheTagHelper.UpdateCacheContext(cacheContext.Object, entryLink);

            // Assert
            cacheContext.Verify();
        }

        [Fact]
        public void UpdateCacheContext_PrefersAbsoluteExpirationSpecifiedOnEntryLinkOverExpiresOn()
        {
            // Arrange
            var expiresOn1 = DateTimeOffset.UtcNow.AddDays(12);
            var expiresOn2 = DateTimeOffset.UtcNow.AddMinutes(4);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheContext = new Mock<ICacheSetContext>();
            var sequence = new MockSequence();
            cacheContext.InSequence(sequence)
                        .Setup(c => c.SetAbsoluteExpiration(expiresOn1))
                        .Verifiable();

            cacheContext.InSequence(sequence)
                        .Setup(c => c.SetAbsoluteExpiration(expiresOn2))
                        .Verifiable();

            var cacheTagHelper = new CacheTagHelper
            {
                MemoryCache = cache,
                ExpiresOn = expiresOn1
            };

            var entryLink = new EntryLink();
            entryLink.SetAbsoluteExpiration(expiresOn2);

            // Act
            cacheTagHelper.UpdateCacheContext(cacheContext.Object, entryLink);

            // Assert
            cacheContext.Verify();
        }

        [Fact]
        public void UpdateCacheContext_SetsAbsoluteExpiration_IfExpiresAfterIsSet()
        {
            // Arrange
            var expiresAfter = TimeSpan.FromSeconds(42);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheContext = new Mock<ICacheSetContext>();
            cacheContext.Setup(c => c.SetAbsoluteExpiration(expiresAfter))
                        .Verifiable();
            var cacheTagHelper = new CacheTagHelper
            {
                MemoryCache = cache,
                ExpiresAfter = expiresAfter
            };

            // Act
            cacheTagHelper.UpdateCacheContext(cacheContext.Object, new EntryLink());

            // Assert
            cacheContext.Verify();
        }

        [Fact]
        public void UpdateCacheContext_SetsSlidingExpiration_IfExpiresSlidingIsSet()
        {
            // Arrange
            var expiresSliding = TimeSpan.FromSeconds(37);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheContext = new Mock<ICacheSetContext>();
            cacheContext.Setup(c => c.SetSlidingExpiration(expiresSliding))
                        .Verifiable();
            var cacheTagHelper = new CacheTagHelper
            {
                MemoryCache = cache,
                ExpiresSliding = expiresSliding
            };

            // Act
            cacheTagHelper.UpdateCacheContext(cacheContext.Object, new EntryLink());

            // Assert
            cacheContext.Verify();
        }

        [Fact]
        public void UpdateCacheContext_SetsCachePreservationPriority()
        {
            // Arrange
            var priority = CachePreservationPriority.High;
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheContext = new Mock<ICacheSetContext>();
            cacheContext.Setup(c => c.SetPriority(priority))
                        .Verifiable();
            var cacheTagHelper = new CacheTagHelper
            {
                MemoryCache = cache,
                Priority = priority
            };

            // Act
            cacheTagHelper.UpdateCacheContext(cacheContext.Object, new EntryLink());

            // Assert
            cacheContext.Verify();
        }

        [Fact]
        public void UpdateCacheContext_CopiesTriggersFromEntryLink()
        {
            // Arrange
            var expiresSliding = TimeSpan.FromSeconds(30);
            var expected = new[] { Mock.Of<IExpirationTrigger>(), Mock.Of<IExpirationTrigger>() };
            var triggers = new List<IExpirationTrigger>();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheContext = new Mock<ICacheSetContext>();
            cacheContext.Setup(c => c.SetSlidingExpiration(expiresSliding))
                        .Verifiable();
            cacheContext.Setup(c => c.AddExpirationTrigger(It.IsAny<IExpirationTrigger>()))
                        .Callback<IExpirationTrigger>(triggers.Add)
                        .Verifiable();
            var cacheTagHelper = new CacheTagHelper
            {
                MemoryCache = cache,
                ExpiresSliding = expiresSliding
            };

            var entryLink = new EntryLink();
            entryLink.AddExpirationTriggers(expected);

            // Act
            cacheTagHelper.UpdateCacheContext(cacheContext.Object, entryLink);

            // Assert
            cacheContext.Verify();
            Assert.Equal(expected, triggers);
        }

        [Fact]
        public async Task ProcessAsync_UsesExpiresAfter_ToExpireCacheEntry()
        {
            // Arrange - 1
            var currentTime = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var id = "unique-id";
            var childContent1 = "original-child-content";
            var clock = new Mock<ISystemClock>();
            clock.SetupGet(p => p.UtcNow)
                 .Returns(() => currentTime);
            var cache = new MemoryCache(new MemoryCacheOptions { Clock = clock.Object });
            var tagHelperContext1 = GetTagHelperContext(id, childContent1);
            var tagHelperOutput1 = new TagHelperOutput("cache", new TagHelperAttributeList { { "attr", "value" } });
            tagHelperOutput1.PreContent.SetContent("<cache>");
            tagHelperOutput1.PostContent.SetContent("</cache>");
            var cacheTagHelper1 = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                MemoryCache = cache,
                ExpiresAfter = TimeSpan.FromMinutes(10)
            };

            // Act - 1
            await cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1);

            // Assert - 1
            Assert.Empty(tagHelperOutput1.PreContent.GetContent());
            Assert.Empty(tagHelperOutput1.PostContent.GetContent());
            Assert.True(tagHelperOutput1.IsContentModified);
            Assert.Equal(childContent1, tagHelperOutput1.Content.GetContent());

            // Arrange - 2
            var childContent2 = "different-content";
            var tagHelperContext2 = GetTagHelperContext(id, childContent2);
            var tagHelperOutput2 = new TagHelperOutput("cache", new TagHelperAttributeList { { "attr", "value" } });
            tagHelperOutput2.PreContent.SetContent("<cache>");
            tagHelperOutput2.PostContent.SetContent("</cache>");
            var cacheTagHelper2 = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                MemoryCache = cache,
                ExpiresAfter = TimeSpan.FromMinutes(10)
            };
            currentTime = currentTime.AddMinutes(11);

            // Act - 2
            await cacheTagHelper2.ProcessAsync(tagHelperContext2, tagHelperOutput2);

            // Assert - 2
            Assert.Empty(tagHelperOutput2.PreContent.GetContent());
            Assert.Empty(tagHelperOutput2.PostContent.GetContent());
            Assert.True(tagHelperOutput2.IsContentModified);
            Assert.Equal(childContent2, tagHelperOutput2.Content.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_UsesExpiresOn_ToExpireCacheEntry()
        {
            // Arrange - 1
            var currentTime = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var id = "unique-id";
            var childContent1 = "original-child-content";
            var clock = new Mock<ISystemClock>();
            clock.SetupGet(p => p.UtcNow)
                 .Returns(() => currentTime);
            var cache = new MemoryCache(new MemoryCacheOptions { Clock = clock.Object });
            var tagHelperContext1 = GetTagHelperContext(id, childContent1);
            var tagHelperOutput1 = new TagHelperOutput("cache", new TagHelperAttributeList { { "attr", "value" } });
            tagHelperOutput1.PreContent.SetContent("<cache>");
            tagHelperOutput1.PostContent.SetContent("</cache>");
            var cacheTagHelper1 = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                MemoryCache = cache,
                ExpiresOn = currentTime.AddMinutes(5)
            };

            // Act - 1
            await cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1);

            // Assert - 1
            Assert.Empty(tagHelperOutput1.PreContent.GetContent());
            Assert.Empty(tagHelperOutput1.PostContent.GetContent());
            Assert.True(tagHelperOutput1.IsContentModified);
            Assert.Equal(childContent1, tagHelperOutput1.Content.GetContent());

            // Arrange - 2
            currentTime = currentTime.AddMinutes(5).AddSeconds(2);
            var childContent2 = "different-content";
            var tagHelperContext2 = GetTagHelperContext(id, childContent2);
            var tagHelperOutput2 = new TagHelperOutput("cache", new TagHelperAttributeList { { "attr", "value" } });
            tagHelperOutput2.PreContent.SetContent("<cache>");
            tagHelperOutput2.PostContent.SetContent("</cache>");
            var cacheTagHelper2 = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                MemoryCache = cache,
                ExpiresOn = currentTime.AddMinutes(5)
            };

            // Act - 2
            await cacheTagHelper2.ProcessAsync(tagHelperContext2, tagHelperOutput2);

            // Assert - 2
            Assert.Empty(tagHelperOutput2.PreContent.GetContent());
            Assert.Empty(tagHelperOutput2.PostContent.GetContent());
            Assert.True(tagHelperOutput2.IsContentModified);
            Assert.Equal(childContent2, tagHelperOutput2.Content.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_UsesExpiresSliding_ToExpireCacheEntryWithSlidingExpiration()
        {
            // Arrange - 1
            var currentTime = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var id = "unique-id";
            var childContent1 = "original-child-content";
            var clock = new Mock<ISystemClock>();
            clock.SetupGet(p => p.UtcNow)
                 .Returns(() => currentTime);
            var cache = new MemoryCache(new MemoryCacheOptions { Clock = clock.Object });
            var tagHelperContext1 = GetTagHelperContext(id, childContent1);
            var tagHelperOutput1 = new TagHelperOutput("cache", new TagHelperAttributeList { { "attr", "value" } });
            tagHelperOutput1.PreContent.SetContent("<cache>");
            tagHelperOutput1.PostContent.SetContent("</cache>");
            var cacheTagHelper1 = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                MemoryCache = cache,
                ExpiresSliding = TimeSpan.FromSeconds(30)
            };

            // Act - 1
            await cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1);

            // Assert - 1
            Assert.Empty(tagHelperOutput1.PreContent.GetContent());
            Assert.Empty(tagHelperOutput1.PostContent.GetContent());
            Assert.True(tagHelperOutput1.IsContentModified);
            Assert.Equal(childContent1, tagHelperOutput1.Content.GetContent());

            // Arrange - 2
            currentTime = currentTime.AddSeconds(35);
            var childContent2 = "different-content";
            var tagHelperContext2 = GetTagHelperContext(id, childContent2);
            var tagHelperOutput2 = new TagHelperOutput("cache", new TagHelperAttributeList { { "attr", "value" } });
            tagHelperOutput2.PreContent.SetContent("<cache>");
            tagHelperOutput2.PostContent.SetContent("</cache>");
            var cacheTagHelper2 = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                MemoryCache = cache,
                ExpiresSliding = TimeSpan.FromSeconds(30)
            };

            // Act - 2
            await cacheTagHelper2.ProcessAsync(tagHelperContext2, tagHelperOutput2);

            // Assert - 2
            Assert.Empty(tagHelperOutput2.PreContent.GetContent());
            Assert.Empty(tagHelperOutput2.PostContent.GetContent());
            Assert.True(tagHelperOutput2.IsContentModified);
            Assert.Equal(childContent2, tagHelperOutput2.Content.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_FlowsEntryLinkThatAllowsAddingTriggersToAddedEntry()
        {
            // Arrange
            var id = "some-id";
            var expectedContent = new DefaultTagHelperContent();
            expectedContent.SetContent("some-content");
            var tokenSource = new CancellationTokenSource();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(),
                items: new Dictionary<object, object>(),
                uniqueId: id,
                getChildContentAsync: () =>
                {
                    var entryLink = EntryLinkHelpers.ContextLink;
                    Assert.NotNull(entryLink);
                    entryLink.AddExpirationTriggers(new[]
                    {
                        new CancellationTokenTrigger(tokenSource.Token)
                    });
                    return Task.FromResult<TagHelperContent>(expectedContent);
                });
            var tagHelperOutput = new TagHelperOutput("cache", new TagHelperAttributeList { { "attr", "value" } });
            tagHelperOutput.PreContent.SetContent("<cache>");
            tagHelperOutput.PostContent.SetContent("</cache>");
            var cacheTagHelper = new CacheTagHelper
            {
                ViewContext = GetViewContext(),
                MemoryCache = cache,
            };
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Act - 1
            await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);
            TagHelperContent cachedValue;
            var result = cache.TryGetValue(key, out cachedValue);

            // Assert - 1
            Assert.Equal(expectedContent.GetContent(), tagHelperOutput.Content.GetContent());
            Assert.True(result);
            Assert.Equal(expectedContent, cachedValue);

            // Act - 2
            tokenSource.Cancel();
            result = cache.TryGetValue(key, out cachedValue);

            // Assert - 2
            Assert.False(result);
            Assert.Null(cachedValue);
        }

        private static ViewContext GetViewContext()
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            return new ViewContext(actionContext,
                                   Mock.Of<IView>(),
                                   new ViewDataDictionary(new EmptyModelMetadataProvider()),
                                   Mock.Of<ITempDataDictionary>(),
                                   TextWriter.Null);
        }

        private static TagHelperContext GetTagHelperContext(string id = "testid",
                                                            string childContent = "some child content")
        {
            return new TagHelperContext(
                allAttributes: new TagHelperAttributeList(),
                items: new Dictionary<object, object>(),
                uniqueId: id,
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent(childContent);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
        }

        private static string GetHashedBytes(string input)
        {
            using (var sha = SHA256.Create())
            {
                var contentBytes = Encoding.UTF8.GetBytes(input);
                var hashedBytes = sha.ComputeHash(contentBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}