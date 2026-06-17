// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
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
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class DistributedCacheTagHelperTest
{

    [Fact]
    public async Task ProcessAsync_DoesNotCache_IfDisabled()
    {
        // Arrange
        var childContent = "original-child-content";
        var storage = new Mock<IDistributedCacheTagHelperStorage>();
        var value = Encoding.UTF8.GetBytes("ok");
        storage.Setup(c => c.SetAsync(
            /*key*/ It.IsAny<string>(),
            /*value*/ value,
            /*options*/ It.IsAny<DistributedCacheEntryOptions>()));
        storage.Setup(c => c.GetAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(value));
        var tagHelperContext = GetTagHelperContext();
        var service = new DistributedCacheTagHelperService(
            storage.Object,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance);
        var tagHelperOutput = GetTagHelperOutput(
            attributes: new TagHelperAttributeList(),
            childContent: childContent);
        var cacheTagHelper = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            Enabled = false
        };

        // Act
        await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

        // Assert
        Assert.Equal(childContent, tagHelperOutput.Content.GetContent());
        storage.Verify(c => c.SetAsync(
            /*key*/ It.IsAny<string>(),
            /*content*/ value,
            /*options*/ It.IsAny<DistributedCacheEntryOptions>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsCachedValue_IfEnabled()
    {
        // Arrange
        var childContent = "original-child-content";
        var storage = new Mock<IDistributedCacheTagHelperStorage>();
        var value = Encoding.UTF8.GetBytes(childContent);
        storage.Setup(c => c.SetAsync(
            /*key*/ It.IsAny<string>(),
            /*value*/ value,
            /*options*/ It.IsAny<DistributedCacheEntryOptions>()));
        storage.Setup(c => c.GetAsync(It.IsAny<string>()))
            .Returns(Task.FromResult<byte[]>(null));
        var service = new DistributedCacheTagHelperService(
            storage.Object,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance);
        var tagHelperContext = GetTagHelperContext();
        var tagHelperOutput = GetTagHelperOutput(
            attributes: new TagHelperAttributeList(),
            childContent: childContent);
        var cacheTagHelper = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            Enabled = true,
            Name = "some-name"
        };

        // Act
        await cacheTagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

        // Assert
        Assert.Empty(tagHelperOutput.PreContent.GetContent());
        Assert.Empty(tagHelperOutput.PostContent.GetContent());
        Assert.True(tagHelperOutput.IsContentModified);
        Assert.Equal(childContent, tagHelperOutput.Content.GetContent());

        storage.Verify(c => c.GetAsync(
            /*key*/ It.IsAny<string>()
            ),
            Times.Once);

        storage.Verify(c => c.SetAsync(
            /*key*/ It.IsAny<string>(),
            /*value*/ It.IsAny<byte[]>(),
            /*options*/ It.IsAny<DistributedCacheEntryOptions>()),
            Times.Once);

    }

    [Fact]
    public async Task ProcessAsync_ReturnsCachedValue_IfVaryByParamIsUnchanged()
    {
        // Arrange - 1
        var childContent = "original-child-content";
        var storage = GetStorage();
        var formatter = GetFormatter();
        var tagHelperContext1 = GetTagHelperContext();
        var tagHelperOutput1 = GetTagHelperOutput(
            attributes: new TagHelperAttributeList(),
            childContent: childContent);
        var service = new DistributedCacheTagHelperService(
            storage,
            formatter,
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance);
        var cacheTagHelper1 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            Enabled = true,
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
        var tagHelperContext2 = GetTagHelperContext();
        var tagHelperOutput2 = GetTagHelperOutput(
            attributes: new TagHelperAttributeList(),
            childContent: "different-content");
        var cacheTagHelper2 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            Enabled = true,
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
        var childContent1 = "original-child-content";
        var storage = GetStorage();
        var service = new DistributedCacheTagHelperService(
            storage,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance);
        var tagHelperContext1 = GetTagHelperContext();
        var tagHelperOutput1 = GetTagHelperOutput(childContent: childContent1);
        tagHelperOutput1.PreContent.Append("<cache>");
        tagHelperOutput1.PostContent.SetContent("</cache>");
        var cacheTagHelper1 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
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
        var tagHelperContext2 = GetTagHelperContext();
        var tagHelperOutput2 = GetTagHelperOutput(childContent: childContent2);
        tagHelperOutput2.PreContent.SetContent("<cache>");
        tagHelperOutput2.PostContent.SetContent("</cache>");
        var cacheTagHelper2 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
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
        var storage = GetStorage();
        var service = new DistributedCacheTagHelperService(
            storage,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance
            );
        var cacheTagHelper = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            ExpiresOn = expiresOn
        };

        // Act
        var cacheEntryOptions = cacheTagHelper.GetDistributedCacheEntryOptions();

        // Assert
        Assert.Equal(expiresOn, cacheEntryOptions.AbsoluteExpiration);
    }

    [Fact]
    public void UpdateCacheEntryOptions_DefaultsTo30SecondsSliding_IfNoEvictionCriteriaIsProvided()
    {
        // Arrange
        var slidingExpiresIn = TimeSpan.FromSeconds(30);
        var storage = GetStorage();
        var service = new DistributedCacheTagHelperService(
            storage,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance
            );
        var cacheTagHelper = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder());

        // Act
        var cacheEntryOptions = cacheTagHelper.GetDistributedCacheEntryOptions();

        // Assert
        Assert.Equal(slidingExpiresIn, cacheEntryOptions.SlidingExpiration);
    }

    [Fact]
    public void UpdateCacheEntryOptions_SetsAbsoluteExpiration_IfExpiresAfterIsSet()
    {
        // Arrange
        var expiresAfter = TimeSpan.FromSeconds(42);
        var storage = GetStorage();
        var service = new DistributedCacheTagHelperService(
            storage,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance
            );
        var cacheTagHelper = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            ExpiresAfter = expiresAfter
        };

        // Act
        var cacheEntryOptions = cacheTagHelper.GetDistributedCacheEntryOptions();

        // Assert
        Assert.Equal(expiresAfter, cacheEntryOptions.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public void UpdateCacheEntryOptions_SetsSlidingExpiration_IfExpiresSlidingIsSet()
    {
        // Arrange
        var expiresSliding = TimeSpan.FromSeconds(37);
        var storage = GetStorage();
        var service = new DistributedCacheTagHelperService(
            storage,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance
            );
        var cacheTagHelper = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            ExpiresSliding = expiresSliding
        };

        // Act
        var cacheEntryOptions = cacheTagHelper.GetDistributedCacheEntryOptions();

        // Assert
        Assert.Equal(expiresSliding, cacheEntryOptions.SlidingExpiration);
    }

    [Fact]
    public async Task ProcessAsync_UsesExpiresAfter_ToExpireCacheEntry()
    {
        // Arrange - 1
        var currentTime = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var childContent1 = "original-child-content";
        var clock = new Mock<ISystemClock>();
        clock.SetupGet(p => p.UtcNow)
            .Returns(() => currentTime);
        var storage = GetStorage(Options.Create(new MemoryDistributedCacheOptions { Clock = clock.Object }));
        var service = new DistributedCacheTagHelperService(
            storage,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance
            );
        var tagHelperContext1 = GetTagHelperContext();
        var tagHelperOutput1 = GetTagHelperOutput(childContent: childContent1);
        tagHelperOutput1.PreContent.SetContent("<cache>");
        tagHelperOutput1.PostContent.SetContent("</cache>");
        var cacheTagHelper1 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
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
        var tagHelperContext2 = GetTagHelperContext();
        var tagHelperOutput2 = GetTagHelperOutput(childContent: childContent2);
        tagHelperOutput2.PreContent.SetContent("<cache>");
        tagHelperOutput2.PostContent.SetContent("</cache>");
        var cacheTagHelper2 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
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
        var childContent1 = "original-child-content";
        var clock = new Mock<ISystemClock>();
        clock.SetupGet(p => p.UtcNow)
            .Returns(() => currentTime);
        var storage = GetStorage(Options.Create(new MemoryDistributedCacheOptions { Clock = clock.Object }));
        var service = new DistributedCacheTagHelperService(
            storage,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance
            );
        var tagHelperContext1 = GetTagHelperContext();
        var tagHelperOutput1 = GetTagHelperOutput(childContent: childContent1);
        tagHelperOutput1.PreContent.SetContent("<distributed-cache>");
        tagHelperOutput1.PostContent.SetContent("</distributed-cache>");
        var cacheTagHelper1 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
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
        var tagHelperContext2 = GetTagHelperContext();
        var tagHelperOutput2 = GetTagHelperOutput(childContent: childContent2);
        tagHelperOutput2.PreContent.SetContent("<distributed-cache>");
        tagHelperOutput2.PostContent.SetContent("</distributed-cache>");
        var cacheTagHelper2 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
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
        var childContent1 = "original-child-content";
        var clock = new Mock<ISystemClock>();
        clock.SetupGet(p => p.UtcNow)
            .Returns(() => currentTime);
        var storage = GetStorage(Options.Create(new MemoryDistributedCacheOptions { Clock = clock.Object }));
        var service = new DistributedCacheTagHelperService(
            storage,
            Mock.Of<IDistributedCacheTagHelperFormatter>(),
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance
            );
        var tagHelperContext1 = GetTagHelperContext();
        var tagHelperOutput1 = GetTagHelperOutput(childContent: childContent1);
        tagHelperOutput1.PreContent.SetContent("<distributed-cache>");
        tagHelperOutput1.PostContent.SetContent("</distributed-cache>");
        var cacheTagHelper1 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
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
        var tagHelperContext2 = GetTagHelperContext();
        var tagHelperOutput2 = GetTagHelperOutput(childContent: childContent2);
        tagHelperOutput2.PreContent.SetContent("<distributed-cache>");
        tagHelperOutput2.PostContent.SetContent("</distributed-cache>");
        var cacheTagHelper2 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
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
    public async Task ProcessAsync_ComputesValueOnce_WithConcurrentRequests()
    {
        // Arrange
        var childContent = "some-content";
        var event1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var event2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var event3 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var formatter = GetFormatter();
        var storage = GetStorage();
        var service = new DistributedCacheTagHelperService(
            storage,
            formatter,
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance
            );
        var tagHelperContext1 = GetTagHelperContext();
        var tagHelperContext2 = GetTagHelperContext();

        var tagHelperOutput1 = new TagHelperOutput(
            "distributed-cache",
            new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                calls++;
                event2.SetResult();

                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetHtmlContent(childContent);
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        var tagHelperOutput2 = new TagHelperOutput(
            "distributed-cache",
            new TagHelperAttributeList(),
            getChildContentAsync: async (useCachedResult, encoder) =>
            {
                calls++;
                await event3.Task.TimeoutAfter(TimeSpan.FromSeconds(5));

                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetHtmlContent(childContent);
                return tagHelperContent;
            });

        var cacheTagHelper1 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            Enabled = true
        };

        var cacheTagHelper2 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            Enabled = true
        };

        // Act

        var task1 = Task.Run(async () =>
        {
            await event1.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
            await cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1);
            event3.SetResult();
        });

        var task2 = Task.Run(async () =>
        {
            await event2.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
            await cacheTagHelper2.ProcessAsync(tagHelperContext1, tagHelperOutput2);
        });

        event1.SetResult();
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
        var childContent = "some-content";
        var event1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var event2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var event3 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var formatter = GetFormatter();
        var storage = GetStorage();
        var service = new DistributedCacheTagHelperService(
            storage,
            formatter,
            new HtmlTestEncoder(),
            NullLoggerFactory.Instance
            );
        var tagHelperContext1 = GetTagHelperContext();
        var tagHelperContext2 = GetTagHelperContext();

        var tagHelperOutput1 = new TagHelperOutput(
            "distributed-cache",
            new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                calls++;
                event2.SetResult();

                throw new Exception();
            });

        var tagHelperOutput2 = new TagHelperOutput(
            "distributed-cache",
            new TagHelperAttributeList(),
            getChildContentAsync: async (useCachedResult, encoder) =>
            {
                calls++;
                await event3.Task.TimeoutAfter(TimeSpan.FromSeconds(5));

                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetHtmlContent(childContent);
                return tagHelperContent;
            });

        var cacheTagHelper1 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            Enabled = true
        };

        var cacheTagHelper2 = new DistributedCacheTagHelper(
            service,
            new HtmlTestEncoder())
        {
            ViewContext = GetViewContext(),
            Enabled = true
        };

        // Act

        var task1 = Task.Run(async () =>
        {
            await event1.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
            await Assert.ThrowsAsync<Exception>(() => cacheTagHelper1.ProcessAsync(tagHelperContext1, tagHelperOutput1));
            event3.SetResult();
        });

        var task2 = Task.Run(async () =>
        {
            await event2.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
            await cacheTagHelper2.ProcessAsync(tagHelperContext2, tagHelperOutput2);
        });

        event1.SetResult();
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
    public async Task Deserialize_DoesntAlterValue_WhenSerialized()
    {
        // Arrange
        var content = "<b>some content</b>";
        var formatter = GetFormatter();
        var context = new DistributedCacheTagHelperFormattingContext
        {
            Html = new HtmlString(content)
        };
        var serialized = await formatter.SerializeAsync(context);

        // Act
        var deserialized = await formatter.DeserializeAsync(serialized);

        // Assert
        Assert.Equal(deserialized.ToString(), content);
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

    private static TagHelperContext GetTagHelperContext()
    {
        return new TagHelperContext(
            tagName: "test",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "testid");
    }

    private static TagHelperOutput GetTagHelperOutput(
        string tagName = "distributed-cache",
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

    private static IDistributedCacheTagHelperStorage GetStorage(IOptions<MemoryDistributedCacheOptions> options = null)
    {
        return new DistributedCacheTagHelperStorage(new MemoryDistributedCache(options ?? Options.Create(new MemoryDistributedCacheOptions())));
    }

    private static IDistributedCacheTagHelperFormatter GetFormatter()
    {
        return new DistributedCacheTagHelperFormatter();
    }
}
