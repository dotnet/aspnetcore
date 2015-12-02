// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Html;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.WebEncoders.Testing;
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
            var cacheTagHelper = new CacheTagHelper(Mock.Of<IMemoryCache>(), new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(Mock.Of<IMemoryCache>(), new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(Mock.Of<IMemoryCache>(), new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(Mock.Of<IMemoryCache>(), new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(Mock.Of<IMemoryCache>(), new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(Mock.Of<IMemoryCache>(), new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(Mock.Of<IMemoryCache>(), new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(Mock.Of<IMemoryCache>(), new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(Mock.Of<IMemoryCache>(), new HtmlTestEncoder())
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
            var value = new DefaultTagHelperContent().SetContent("ok");
            cache.Setup(c => c.Set(
                /*key*/ It.IsAny<string>(),
                /*value*/ value,
                /*optons*/ It.IsAny<MemoryCacheEntryOptions>()))
                .Returns(value)
                .Verifiable();
            object cacheResult;
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cacheResult))
                .Returns(false);
            var tagHelperContext = GetTagHelperContext(id);
            var tagHelperOutput = GetTagHelperOutput(
                attributes: new TagHelperAttributeList(),
                childContent: childContent);
            var cacheTagHelper = new CacheTagHelper(cache.Object, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = false
            };

            // Act
            await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

            // Assert
            Assert.Equal(childContent, tagHelperOutput.Content.GetContent());
            cache.Verify(c => c.Set(
                /*key*/ It.IsAny<string>(),
                /*value*/ It.IsAny<object>(),
                /*options*/ It.IsAny<MemoryCacheEntryOptions>()),
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
            var value = new DefaultTagHelperContent().SetContent("ok");
            cache.Setup(c => c.CreateLinkingScope()).Returns(new Mock<IEntryLink>().Object);
            cache.Setup(c => c.Set(
                /*key*/ It.IsAny<string>(),
                /*value*/ It.IsAny<object>(),
                /*options*/ It.IsAny<MemoryCacheEntryOptions>()))
                .Returns(value)
                .Verifiable();
            object cacheResult;
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cacheResult))
                .Returns(false);
            var tagHelperContext = GetTagHelperContext(id);
            var tagHelperOutput = GetTagHelperOutput(
                attributes: new TagHelperAttributeList(),
                childContent: childContent);
            var cacheTagHelper = new CacheTagHelper(cache.Object, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
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
                /*value*/ It.IsAny<object>(),
                /*options*/ It.IsAny<MemoryCacheEntryOptions>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_ReturnsCachedValue_IfVaryByParamIsUnchanged()
        {
            // Arrange - 1
            var id = "unique-id";
            var childContent = "original-child-content";
            var cache = new MemoryCache(new MemoryCacheOptions());
            var tagHelperContext1 = GetTagHelperContext(id);
            var tagHelperOutput1 = GetTagHelperOutput(
                attributes: new TagHelperAttributeList(),
                childContent: childContent);
            var cacheTagHelper1 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                VaryByQuery = "key1,key2",
                ViewContext = GetViewContext(),
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
            var tagHelperContext2 = GetTagHelperContext(id);
            var tagHelperOutput2 = GetTagHelperOutput(
                attributes: new TagHelperAttributeList(),
                childContent: "different-content");
            var cacheTagHelper2 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                VaryByQuery = "key1,key2",
                ViewContext = GetViewContext(),
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
            var tagHelperContext1 = GetTagHelperContext(id);
            var tagHelperOutput1 = GetTagHelperOutput(childContent: childContent1);
            tagHelperOutput1.PreContent.Append("<cache>");
            tagHelperOutput1.PostContent.SetContent("</cache>");
            var cacheTagHelper1 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                VaryByCookie = "cookie1,cookie2",
                ViewContext = GetViewContext(),
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
            var tagHelperContext2 = GetTagHelperContext(id);
            var tagHelperOutput2 = GetTagHelperOutput(childContent: childContent2);
            tagHelperOutput2.PreContent.SetContent("<cache>");
            tagHelperOutput2.PostContent.SetContent("</cache>");
            var cacheTagHelper2 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                VaryByCookie = "cookie1,cookie2",
                ViewContext = GetViewContext(),
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
        public void UpdateCacheEntryOptions_SetsAbsoluteExpiration_IfExpiresOnIsSet()
        {
            // Arrange
            var expiresOn = DateTimeOffset.UtcNow.AddMinutes(4);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ExpiresOn = expiresOn
            };

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions(new EntryLink());

            // Assert
            Assert.Equal(expiresOn, cacheEntryOptions.AbsoluteExpiration);
        }

        [Fact]
        public void UpdateCacheEntryOptions_UsesAbsoluteExpirationSpecifiedOnEntryLink()
        {
            // Arrange
            var expiresOn = DateTimeOffset.UtcNow.AddMinutes(7);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
            };

            var entryLink = new EntryLink();
            entryLink.SetAbsoluteExpiration(expiresOn);

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions(entryLink);

            // Assert
            Assert.Equal(expiresOn, cacheEntryOptions.AbsoluteExpiration);
        }

        [Fact]
        public void UpdateCacheEntryOptions_PrefersAbsoluteExpirationSpecifiedOnEntryLinkOverExpiresOn()
        {
            // Arrange
            var expiresOn1 = DateTimeOffset.UtcNow.AddDays(12);
            var expiresOn2 = DateTimeOffset.UtcNow.AddMinutes(4);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ExpiresOn = expiresOn1
            };

            var entryLink = new EntryLink();
            entryLink.SetAbsoluteExpiration(expiresOn2);

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions(entryLink);

            // Assert
            Assert.Equal(expiresOn2, cacheEntryOptions.AbsoluteExpiration);
        }

        [Fact]
        public void UpdateCacheEntryOptions_SetsAbsoluteExpiration_IfExpiresAfterIsSet()
        {
            // Arrange
            var expiresAfter = TimeSpan.FromSeconds(42);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ExpiresAfter = expiresAfter
            };

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions(new EntryLink());

            // Assert
            Assert.Equal(expiresAfter, cacheEntryOptions.AbsoluteExpirationRelativeToNow);
        }

        [Fact]
        public void UpdateCacheEntryOptions_SetsSlidingExpiration_IfExpiresSlidingIsSet()
        {
            // Arrange
            var expiresSliding = TimeSpan.FromSeconds(37);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ExpiresSliding = expiresSliding
            };

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions(new EntryLink());

            // Assert
            Assert.Equal(expiresSliding, cacheEntryOptions.SlidingExpiration);
        }

        [Fact]
        public void UpdateCacheEntryOptions_SetsCachePreservationPriority()
        {
            // Arrange
            var priority = CacheItemPriority.High;
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                Priority = priority
            };

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions(new EntryLink());

            // Assert
            Assert.Equal(priority, cacheEntryOptions.Priority);
        }

        [Fact]
        public void UpdateCacheEntryOptions_CopiesTriggersFromEntryLink()
        {
            // Arrange
            var expiresSliding = TimeSpan.FromSeconds(30);
            var expected = new[] { Mock.Of<IChangeToken>(), Mock.Of<IChangeToken>() };
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ExpiresSliding = expiresSliding
            };

            var entryLink = new EntryLink();
            entryLink.AddExpirationTokens(expected);

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions(entryLink);

            // Assert
            Assert.Equal(expected, cacheEntryOptions.ExpirationTokens.ToArray());
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
            var tagHelperContext1 = GetTagHelperContext(id);
            var tagHelperOutput1 = GetTagHelperOutput(childContent: childContent1);
            tagHelperOutput1.PreContent.SetContent("<cache>");
            tagHelperOutput1.PostContent.SetContent("</cache>");
            var cacheTagHelper1 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
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
            var tagHelperContext2 = GetTagHelperContext(id);
            var tagHelperOutput2 = GetTagHelperOutput(childContent: childContent2);
            tagHelperOutput2.PreContent.SetContent("<cache>");
            tagHelperOutput2.PostContent.SetContent("</cache>");
            var cacheTagHelper2 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
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
            var tagHelperContext1 = GetTagHelperContext(id);
            var tagHelperOutput1 = GetTagHelperOutput(childContent: childContent1);
            tagHelperOutput1.PreContent.SetContent("<cache>");
            tagHelperOutput1.PostContent.SetContent("</cache>");
            var cacheTagHelper1 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
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
            var tagHelperContext2 = GetTagHelperContext(id);
            var tagHelperOutput2 = GetTagHelperOutput(childContent: childContent2);
            tagHelperOutput2.PreContent.SetContent("<cache>");
            tagHelperOutput2.PostContent.SetContent("</cache>");
            var cacheTagHelper2 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
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
            var tagHelperContext1 = GetTagHelperContext(id);
            var tagHelperOutput1 = GetTagHelperOutput(childContent: childContent1);
            tagHelperOutput1.PreContent.SetContent("<cache>");
            tagHelperOutput1.PostContent.SetContent("</cache>");
            var cacheTagHelper1 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
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
            var tagHelperContext2 = GetTagHelperContext(id);
            var tagHelperOutput2 = GetTagHelperOutput(childContent: childContent2);
            tagHelperOutput2.PreContent.SetContent("<cache>");
            tagHelperOutput2.PostContent.SetContent("</cache>");
            var cacheTagHelper2 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
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
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .AddExpirationToken(new CancellationChangeToken(tokenSource.Token));
            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(),
                items: new Dictionary<object, object>(),
                uniqueId: id);
            var tagHelperOutput = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList { { "attr", "value" } },
                getChildContentAsync: useCachedResult =>
                {
                    TagHelperContent tagHelperContent;
                    if (!cache.TryGetValue("key1", out tagHelperContent))
                    {
                        tagHelperContent = expectedContent;
                        cache.Set("key1", tagHelperContent, cacheEntryOptions);
                    }

                    return Task.FromResult(tagHelperContent);
                });
            tagHelperOutput.PreContent.SetContent("<cache>");
            tagHelperOutput.PostContent.SetContent("</cache>");
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
            };
            var key = cacheTagHelper.GenerateKey(tagHelperContext);

            // Act - 1
            await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);
            IHtmlContent cachedValue;
            var result = cache.TryGetValue(key, out cachedValue);

            // Assert - 1
            Assert.Equal("HtmlEncode[[some-content]]", tagHelperOutput.Content.GetContent());
            Assert.True(result);

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
                                   TextWriter.Null,
                                   new HtmlHelperOptions());
        }

        private static TagHelperContext GetTagHelperContext(string id = "testid")
        {
            return new TagHelperContext(
                allAttributes: new TagHelperAttributeList(),
                items: new Dictionary<object, object>(),
                uniqueId: id);
        }

        private static TagHelperOutput GetTagHelperOutput(
            string tagName = "cache",
            TagHelperAttributeList attributes = null,
            string childContent = "some child content")
        {
            attributes = attributes ?? new TagHelperAttributeList { { "attr", "value" } };

            return new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: useCachedResult =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetHtmlContent(childContent);
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
