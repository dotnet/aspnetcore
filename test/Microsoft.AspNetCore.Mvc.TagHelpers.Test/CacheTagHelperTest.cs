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
            var cacheTagHelper = new CacheTagHelper(cache.Object, new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
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
            var cacheTagHelper1 = new CacheTagHelper(cache, new HtmlTestEncoder())
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
            var cacheTagHelper2 = new CacheTagHelper(cache, new HtmlTestEncoder())
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
            var cacheEntryOptions = cacheTagHelper.GetMemoryCacheEntryOptions();

            // Assert
            Assert.Equal(expiresOn, cacheEntryOptions.AbsoluteExpiration);
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
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
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
            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
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
                tagName: "cache",
                allAttributes: new TagHelperAttributeList(),
                items: new Dictionary<object, object>(),
                uniqueId: id);
            var tagHelperOutput = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList { { "attr", "value" } },
                getChildContentAsync: (useCachedResult, encoder) =>
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

            var cacheTagKey = new CacheTagKey(cacheTagHelper, tagHelperContext);
            var key = cacheTagKey.GenerateKey();

            // Act - 1
            await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);
            Task<IHtmlContent> cachedValue;
            var result = cache.TryGetValue(cacheTagKey, out cachedValue);

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
            var resetEvent1 = new ManualResetEvent(false);
            var resetEvent2 = new ManualResetEvent(false);
            var resetEvent3 = new ManualResetEvent(false);
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
                    resetEvent2.Set();

                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetHtmlContent(childContent);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            var tagHelperOutput2 = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    calls++;
                    resetEvent3.WaitOne(5000);

                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetHtmlContent(childContent);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            var cacheTagHelper1 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            var cacheTagHelper2 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            // Act

            var task1 = Task.Run(async () =>
            {
                resetEvent1.WaitOne(5000);
                await cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1);
                resetEvent3.Set();
            });

            var task2 = Task.Run(async () =>
            {
                resetEvent2.WaitOne(5000);
                await cacheTagHelper2.ProcessAsync(tagHelperContext1, tagHelperOutput2);
            });

            resetEvent1.Set();
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
            var resetEvent1 = new ManualResetEvent(false);
            var resetEvent2 = new ManualResetEvent(false);
            var resetEvent3 = new ManualResetEvent(false);
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
                    resetEvent2.Set();

                    throw new Exception();
                });

            var tagHelperOutput2 = new TagHelperOutput(
                "cache",
                new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    calls++;
                    resetEvent3.WaitOne(5000);

                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetHtmlContent(childContent);
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            var cacheTagHelper1 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            var cacheTagHelper2 = new CacheTagHelper(cache, new HtmlTestEncoder())
            {
                ViewContext = GetViewContext(),
                Enabled = true
            };

            // Act

            var task1 = Task.Run(async () =>
            {
                resetEvent1.WaitOne(5000);
                await Assert.ThrowsAsync<Exception>(() => cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1));
                resetEvent3.Set();
            });

            var task2 = Task.Run(async () =>
            {
                resetEvent2.WaitOne(5000);
                await cacheTagHelper2.ProcessAsync(tagHelperContext2, tagHelperOutput2);
            });

            resetEvent1.Set();
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

            var cacheTagHelper = new CacheTagHelper(cache, new HtmlTestEncoder())
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

        private static ViewContext GetViewContext()
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            return new ViewContext(actionContext,
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
    }
}
