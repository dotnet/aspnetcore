// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class CacheTagHelperTest
    {
        [Fact]
        public async Task ProcessAsync_DoesNotCache_IfDisabled()
        {
            // Arrange
            var id = "unique-id";
            var childContent = "original-child-content";
            var cache = new Mock<IMemoryCache>();
            var value = new Mock<ICacheEntry>();
            value.Setup(c => c.Value).Returns(new DefaultTagHelperContent().SetContent("ok"));
            cache.Setup(c => c.CreateEntry(
                /*key*/ It.IsAny<string>()))
                .Returns((object key) => value.Object)
                .Verifiable();
            object cacheResult;
            cache.Setup(c => c.TryGetValue(It.IsAny<string>(), out cacheResult))
                .Returns(false);
            var tagHelperContext = GetTagHelperContext(id);
            var tagHelperOutput = GetTagHelperOutput(
                attributes: new TagHelperAttributeList(),
                childContent: childContent);
            var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache.Object), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = false
            };

            // Act
            await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

            // Assert
            Assert.Equal(childContent, tagHelperOutput.Content.GetContent());
            cache.Verify(c => c.CreateEntry(
                /*key*/ It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessAsync_ReturnsCachedValue_IfEnabled()
        {
            // Arrange
            var id = "unique-id";
            var childContent = "original-child-content";
            var cache = new MemoryCache(new MemoryCacheOptions());
            var tagHelperContext = GetTagHelperContext(id);
            var tagHelperOutput = GetTagHelperOutput(
                attributes: new TagHelperAttributeList(),
                childContent: childContent);
            var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
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
            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                VaryByQuery = "key1,key2",
                ViewContext = GetViewContext(),
            };
            cacheTagHelper1.ViewContext.HttpContext.Request.QueryString = new QueryString(
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
            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                VaryByQuery = "key1,key2",
                ViewContext = GetViewContext(),
            };
            cacheTagHelper2.ViewContext.HttpContext.Request.QueryString = new QueryString(
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
            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
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
            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ExpiresOn = expiresOn
            };

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions();

            // Assert
            Assert.Equal(expiresOn, cacheEntryOptions.AbsoluteExpiration);
        }

        [Fact]
        public async Task ProcessAsync_SetsEntrySize_ForPlaceholderAndFinalCacheEntries()
        {
            // Arrange
            var mockCache = new Mock<IMemoryCache>();
            var tempEntry = new Mock<ICacheEntry>();
            tempEntry.SetupAllProperties();
            tempEntry.Setup(e => e.ExpirationTokens).Returns(new List<IChangeToken>());
            tempEntry.Setup(e => e.PostEvictionCallbacks).Returns(new List<PostEvictionCallbackRegistration>());
            var finalEntry = new Mock<ICacheEntry>();
            finalEntry.SetupAllProperties();
            finalEntry.Setup(e => e.ExpirationTokens).Returns(new List<IChangeToken>());
            finalEntry.Setup(e => e.PostEvictionCallbacks).Returns(new List<PostEvictionCallbackRegistration>());
            object value;
            mockCache
                .Setup(s => s.TryGetValue(It.IsAny<object>(), out value))
                .Returns(false);

            mockCache.SetupSequence(mc => mc.CreateEntry(It.IsAny<object>()))
                .Returns(tempEntry.Object)
                .Returns(finalEntry.Object);

            var id = "unique-id";
            var childContent1 = "original-child-content";
            var tagHelperContext1 = GetTagHelperContext(id);
            var tagHelperOutput1 = GetTagHelperOutput(childContent: childContent1);
            tagHelperOutput1.PreContent.Append("<cache>");
            tagHelperOutput1.PostContent.SetContent("</cache>");
            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(mockCache.Object), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
            };

            // Act
            await cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1);

            // Assert
            Assert.Empty(tagHelperOutput1.PreContent.GetContent());
            Assert.Empty(tagHelperOutput1.PostContent.GetContent());
            Assert.True(tagHelperOutput1.IsContentModified);
            Assert.Equal(childContent1, tagHelperOutput1.Content.GetContent());
            tempEntry.VerifySet(e => e.Size = 64);
            finalEntry.VerifySet(e => e.Size = childContent1.Length * 2);
        }

        [Fact]
        public void UpdateCacheEntryOptions_DefaultsTo30SecondsSliding_IfNoEvictionCriteriaIsProvided()
        {
            // Arrange
            var slidingExpiresIn = TimeSpan.FromSeconds(30);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder());

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions();

            // Assert
            Assert.Equal(slidingExpiresIn, cacheEntryOptions.SlidingExpiration);
        }

        [Fact]
        public void UpdateCacheEntryOptions_SetsAbsoluteExpiration_IfExpiresAfterIsSet()
        {
            // Arrange
            var expiresAfter = TimeSpan.FromSeconds(42);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ExpiresAfter = expiresAfter
            };

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions();

            // Assert
            Assert.Equal(expiresAfter, cacheEntryOptions.AbsoluteExpirationRelativeToNow);
        }

        [Fact]
        public void UpdateCacheEntryOptions_SetsSlidingExpiration_IfExpiresSlidingIsSet()
        {
            // Arrange
            var expiresSliding = TimeSpan.FromSeconds(37);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ExpiresSliding = expiresSliding
            };

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions();

            // Assert
            Assert.Equal(expiresSliding, cacheEntryOptions.SlidingExpiration);
        }

        [Fact]
        public void UpdateCacheEntryOptions_SetsCachePreservationPriority()
        {
            // Arrange
            var priority = CacheItemPriority.High;
            var cache = new MemoryCache(new MemoryCacheOptions());
            var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                Priority = priority
            };

            // Act
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions();

            // Assert
            Assert.Equal(priority, cacheEntryOptions.Priority);
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
            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
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
            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
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
            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
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
            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
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
            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
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
            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
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
                tagName: "cache",
                allAttributes: new TagHelperAttributeList(),
                items: new Dictionary<object, object>(),
                uniqueId: id);
            var tagHelperOutput = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList { { "attr", "value" } },
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    if (!cache.TryGetValue("key1", out TagHelperContent tagHelperContent))
                    {
                        tagHelperContent = expectedContent;
                        cache.Set("key1", tagHelperContent, cacheEntryOptions);
                    }

                    return Task.FromResult(tagHelperContent);
                });
            tagHelperOutput.PreContent.SetContent("<cache>");
            tagHelperOutput.PostContent.SetContent("</cache>");
            var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
            };

            var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
            var key = cacheTagKey.GenerateKey();

            // Act - 1
            await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);
            var result = cache.TryGetValue(cacheTagKey, out Task<IHtmlContent> cachedValue);

            // Assert - 1
            Assert.Equal("HtmlEncode[[some-content]]", tagHelperOutput.Content.GetContent());
            Assert.True(result);

            // Act - 2
            tokenSource.Cancel();
            result = cache.TryGetValue(cacheTagKey, out cachedValue);

            // Assert - 2
            Assert.False(result);
            Assert.Null(cachedValue);
        }

        [Fact]
        public async Task ProcessAsync_ComputesValueOnce_WithConcurrentRequests()
        {
            // Arrange
            var id = "unique-id";
            var childContent = "some-content";
            var event1 = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var event2 = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var event3 = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0;
            var cache = new MemoryCache(new MemoryCacheOptions());

            var tagHelperContext1 = GetTagHelperContext(id + 1);
            var tagHelperContext2 = GetTagHelperContext(id + 2);

            var tagHelperOutput1 = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    calls++;
                    event2.SetResult(0);

                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetHtmlContent(childContent);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            var tagHelperOutput2 = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: async (useCachedResult, encoder) =>
                {
                    calls++;
                    await event3.Task.TimeoutAfter(TimeSpan.FromSeconds(5));

                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetHtmlContent(childContent);
                    return tagHelperContent;
                });

            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            // Act

            var task1 = Task.Run(async () =>
            {
                await event1.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
                await cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1);
                event3.SetResult(0);
            });

            var task2 = Task.Run(async () =>
            {
                await event2.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
                await cacheTagHelper2.ProcessAsync(tagHelperContext1, tagHelperOutput2);
            });

            event1.SetResult(0);
            await Task.WhenAll(task1, task2);

            // Assert
            Assert.Empty(tagHelperOutput1.PreContent.GetContent());
            Assert.Empty(tagHelperOutput1.PostContent.GetContent());
            Assert.True(tagHelperOutput1.IsContentModified);
            Assert.Equal(childContent, tagHelperOutput1.Content.GetContent());

            Assert.Empty(tagHelperOutput2.PreContent.GetContent());
            Assert.Empty(tagHelperOutput2.PostContent.GetContent());
            Assert.True(tagHelperOutput2.IsContentModified);
            Assert.Equal(childContent, tagHelperOutput2.Content.GetContent());

            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task ProcessAsync_ExceptionInProcessing_DoesntBlockConcurrentRequests()
        {
            // Arrange
            var id = "unique-id";
            var childContent = "some-content";
            var event1 = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var event2 = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var event3 = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0;
            var cache = new MemoryCache(new MemoryCacheOptions());

            var tagHelperContext1 = GetTagHelperContext(id + 1);
            var tagHelperContext2 = GetTagHelperContext(id + 2);

            var tagHelperOutput1 = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    calls++;
                    event2.SetResult(0);

                    throw new Exception();
                });

            var tagHelperOutput2 = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: async (useCachedResult, encoder) =>
                {
                    calls++;
                    await event3.Task.TimeoutAfter(TimeSpan.FromSeconds(5));

                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetHtmlContent(childContent);
                    return tagHelperContent;
                });

            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            // Act

            var task1 = Task.Run(async () =>
            {
                await event1.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
                await Assert.ThrowsAsync<Exception>(() => cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1));
                event3.SetResult(0);
            });

            var task2 = Task.Run(async () =>
            {
                await event2.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
                await cacheTagHelper2.ProcessAsync(tagHelperContext2, tagHelperOutput2);
            });

            event1.SetResult(0);
            await Task.WhenAll(task1, task2);

            // Assert
            Assert.Empty(tagHelperOutput1.PreContent.GetContent());
            Assert.Empty(tagHelperOutput1.PostContent.GetContent());
            Assert.False(tagHelperOutput1.IsContentModified);
            Assert.Empty(tagHelperOutput1.Content.GetContent());

            Assert.Empty(tagHelperOutput2.PreContent.GetContent());
            Assert.Empty(tagHelperOutput2.PostContent.GetContent());
            Assert.True(tagHelperOutput2.IsContentModified);
            Assert.Equal(childContent, tagHelperOutput2.Content.GetContent());

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task ProcessAsync_ExceptionInProcessing_DoNotThrowInSubsequentRequests()
        {
            // Arrange
            var id = "unique-id";
            var childContent = "some-content";
            var cache = new MemoryCache(new MemoryCacheOptions());

            var counter = 0;

            Task<TagHelperContent> GetChildContentAsync(bool useCachedResult, HtmlEncoder encoder)
            {
                counter++;
                if (counter < 3)
                {
                    // throw on first two calls
                    throw new Exception();
                }
                else
                {
                    // produce content on third call
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetHtmlContent(childContent);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                }
            }

            var tagHelperContext1 = GetTagHelperContext(id);
            var tagHelperContext2 = GetTagHelperContext(id);
            var tagHelperContext3 = GetTagHelperContext(id);
            var tagHelperContext4 = GetTagHelperContext(id);

            var tagHelperOutput1 = new TagHelperOutput("cache", new TagHelperAttributeList(), GetChildContentAsync);
            var tagHelperOutput2 = new TagHelperOutput("cache", new TagHelperAttributeList(), GetChildContentAsync);
            var tagHelperOutput3 = new TagHelperOutput("cache", new TagHelperAttributeList(), GetChildContentAsync);
            var tagHelperOutput4 = new TagHelperOutput("cache", new TagHelperAttributeList(), GetChildContentAsync);

            var cacheTagHelper = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true,
                ExpiresAfter = TimeSpan.FromHours(1.0)
            };

            // Act - 1

            await Assert.ThrowsAsync<Exception>(() => cacheTagHelper.ProcessAsync(tagHelperContext1, tagHelperOutput1));

            // Assert - 1

            Assert.Equal(1, counter);
            Assert.Empty(tagHelperOutput1.PreContent.GetContent());
            Assert.Empty(tagHelperOutput1.PostContent.GetContent());
            Assert.False(tagHelperOutput1.IsContentModified);
            Assert.Empty(tagHelperOutput1.Content.GetContent());

            // Act - 2

            await Assert.ThrowsAsync<Exception>(() => cacheTagHelper.ProcessAsync(tagHelperContext2, tagHelperOutput2));

            // Assert - 2

            Assert.Equal(2, counter);
            Assert.Empty(tagHelperOutput2.PreContent.GetContent());
            Assert.Empty(tagHelperOutput2.PostContent.GetContent());
            Assert.False(tagHelperOutput2.IsContentModified);
            Assert.Empty(tagHelperOutput2.Content.GetContent());

            // Act - 3

            await cacheTagHelper.ProcessAsync(tagHelperContext3, tagHelperOutput3);

            // Assert - 3

            Assert.Equal(3, counter);
            Assert.Empty(tagHelperOutput3.PreContent.GetContent());
            Assert.Empty(tagHelperOutput3.PostContent.GetContent());
            Assert.True(tagHelperOutput3.IsContentModified);
            Assert.Equal(childContent, tagHelperOutput3.Content.GetContent());

            // Act - 4

            await cacheTagHelper.ProcessAsync(tagHelperContext4, tagHelperOutput4);

            // Assert - 4

            Assert.Equal(3, counter);
            Assert.Empty(tagHelperOutput4.PreContent.GetContent());
            Assert.Empty(tagHelperOutput4.PostContent.GetContent());
            Assert.True(tagHelperOutput4.IsContentModified);
            Assert.Equal(childContent, tagHelperOutput4.Content.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_WorksForNestedCacheTagHelpers()
        {
            // Arrange
            var expected = "Hello world";
            var cache = new MemoryCache(new MemoryCacheOptions());
            var encoder = new HtmlTestEncoder();
            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), encoder)
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), encoder)
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            var tagHelperOutput2 = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, _) =>
                {
                    var content = new DefaultTagHelperContent();
                    content.SetContent(expected);
                    return Task.FromResult<TagHelperContent>(content);
                });

            var tagHelperOutput1 = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: async (useCachedResult, _) =>
                {
                    var context = GetTagHelperContext("test2");
                    var output = tagHelperOutput2;
                    await cacheTagHelper2.ProcessAsync(context, output);
                    return await output.GetChildContentAsync();
                });

            // Act
            await cacheTagHelper1.ProcessAsync(GetTagHelperContext(), tagHelperOutput1);

            // Assert
            Assert.Equal(encoder.Encode(expected), tagHelperOutput1.Content.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_ThrowsExceptionForAwaiters_IfExecutorEncountersAnException()
        {
            // Arrange
            var expected = new DivideByZeroException();
            var cache = new TestMemoryCache();
            // The two instances represent two instances of the same cache tag helper appearance in the page.
            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };
            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            var invokeCount = 0;
            var tagHelperOutput = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, _) =>
                {
                    invokeCount++;
                    throw expected;
                });

            // Act
            var task1 = Task.Run(() => cacheTagHelper1.ProcessAsync(GetTagHelperContext(cache.Key1), tagHelperOutput));
            var task2 = Task.Run(() => cacheTagHelper2.ProcessAsync(GetTagHelperContext(cache.Key2), tagHelperOutput));

            // Assert
            await Assert.ThrowsAsync<DivideByZeroException>(() => task1);
            await Assert.ThrowsAsync<DivideByZeroException>(() => task2);
            Assert.Equal(1, invokeCount);
        }

        [Fact]
        [QuarantinedTest]
        public async Task ProcessAsync_AwaitersUseTheResultOfExecutor()
        {
            // Arrange
            var expected = "Hello world";
            var cache = new TestMemoryCache();
            var encoder = new HtmlTestEncoder();
            // The two instances represent two instances of the same cache tag helper appearance in the page.
            var cacheTagHelper1 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), encoder)
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            var cacheTagHelper2 = new CacheTagHelper(new CacheTagHelperMemoryCacheFactory(cache), encoder)
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            var invokeCount = 0;
            var tagHelperOutput = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, _) =>
                {
                    invokeCount++;

                    var content = new DefaultTagHelperContent();
                    content.SetContent(expected);
                    return Task.FromResult<TagHelperContent>(content);
                });

            // Act
            var task1 = Task.Run(() => cacheTagHelper1.ProcessAsync(GetTagHelperContext(cache.Key1), tagHelperOutput));
            var task2 = Task.Run(() => cacheTagHelper2.ProcessAsync(GetTagHelperContext(cache.Key2), tagHelperOutput));

            // Assert
            await Task.WhenAll(task1, task2);
            Assert.Equal(encoder.Encode(expected), tagHelperOutput.Content.GetContent());
            Assert.Equal(1, invokeCount);
        }

        private static ViewContext GetViewContext()
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            return new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());
        }

        private static TagHelperContext GetTagHelperContext(string id = "testid")
        {
            return new TagHelperContext(
                tagName: "cache",
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
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetHtmlContent(childContent);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
        }

        private class TestCacheEntry : ICacheEntry
        {
            public object Key { get; set; }
            public object Value { get; set; }
            public DateTimeOffset? AbsoluteExpiration { get; set; }
            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
            public TimeSpan? SlidingExpiration { get; set; }
            public long? Size { get; set; }

            public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();

            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } =
                new List<PostEvictionCallbackRegistration>();

            public CacheItemPriority Priority { get; set; }

            public Action DisposeCallback { get; set; }

            public void Dispose() => DisposeCallback();
        }

        // Simulates the scenario where a call to CacheTagHelper.ProcessAsync appears immediately after a prior one has assigned
        // a TaskCancellationSource as an entry. We want to ensure that both calls use the results of the TCS as their output.
        private class TestMemoryCache : IMemoryCache
        {
            private const int WaitTimeout = 5000;
            public readonly string Key1 = "Key1";
            public readonly string Key2 = "Key2";
            public readonly ManualResetEventSlim ManualResetEvent1 = new ManualResetEventSlim();
            public readonly ManualResetEventSlim ManualResetEvent2 = new ManualResetEventSlim();
            public TestCacheEntry Entry { get; private set; }

            public ICacheEntry CreateEntry(object key)
            {
                if (Entry != null)
                {
                    // We're being invoked in the inner "CreateEntry" call where the TCS is replaced by the GetChildContentAsync
                    // Task. Wait for the other concurrent Task to grab the TCS before we proceed.
                    Assert.True(ManualResetEvent1.Wait(WaitTimeout));
                }

                var cacheKey = Assert.IsType<CacheTagKey>(key);
                Assert.Equal(Key1, cacheKey.Key);

                Entry = new TestCacheEntry
                {
                    Key = key,
                    DisposeCallback = ManualResetEvent2.Set,
                };

                return Entry;
            }

            public void Dispose()
            {
            }

            public void Remove(object key)
            {
            }

            public bool TryGetValue(object key, out object value)
            {
                var cacheKey = Assert.IsType<CacheTagKey>(key);
                if (cacheKey.Key == Key2)
                {
                    Assert.True(ManualResetEvent2.Wait(WaitTimeout));

                    Assert.NotNull(Entry);
                    value = Entry.Value;
                    Assert.NotNull(value);

                    ManualResetEvent1.Set();

                    return true;
                }
                else if (cacheKey.Key == Key1)
                {
                    value = null;
                    return false;
                }

                throw new Exception();
            }
        }
    }
}
