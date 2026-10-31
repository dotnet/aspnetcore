// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

public class TagHelperComponentTagHelperTest
{
    [Fact]
    public void Init_InvokesComponentsInitInCorrectOrder()
    {
        // Arrange
        var tagHelperContext = new TagHelperContext(
            "head",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var incrementer = 0;
        var testTagHelperComponentManager = new TagHelperComponentManager(new[]
        {
                new CallbackTagHelperComponent(
                    order: 2,
                    initCallback: () =>
                    {
                        Assert.Equal(1, incrementer);
                        incrementer++;
                    },
                    processAsyncCallback: null),
                new CallbackTagHelperComponent(
                    order: 3,
                    initCallback: () =>
                    {
                        Assert.Equal(2, incrementer);
                        incrementer++;
                    },
                    processAsyncCallback: null),
                new CallbackTagHelperComponent(
                    order: 1,
                    initCallback: () =>
                    {
                        Assert.Equal(0, incrementer);
                        incrementer++;
                    },
                    processAsyncCallback: null),
            });

        var testTagHelperComponentTagHelper = new TestTagHelperComponentTagHelper(testTagHelperComponentManager, NullLoggerFactory.Instance);

        // Act
        testTagHelperComponentTagHelper.Init(tagHelperContext);

        // Assert
        Assert.Equal(3, incrementer);
    }

    [Fact]
    public async Task ProcessAsync_InvokesComponentsProcessAsyncInCorrectOrder()
    {
        // Arrange
        var tagHelperContext = new TagHelperContext(
            "head",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            "head",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));

        var incrementer = 0;
        var testTagHelperComponentManager = new TagHelperComponentManager(new[]
        {
                new CallbackTagHelperComponent(
                    order: 2,
                    initCallback: () => { },
                    processAsyncCallback: () =>
                    {
                        Assert.Equal(1, incrementer);
                        incrementer++;
                    }),
                new CallbackTagHelperComponent(
                    order: 3,
                    initCallback: () => { },
                    processAsyncCallback: () =>
                    {
                        Assert.Equal(2, incrementer);
                        incrementer++;
                    }),
                new CallbackTagHelperComponent(
                    order: 1,
                    initCallback: () => { },
                    processAsyncCallback: () =>
                    {
                        Assert.Equal(0, incrementer);
                        incrementer++;
                    }),
            });

        var testTagHelperComponentTagHelper = new TestTagHelperComponentTagHelper(testTagHelperComponentManager, NullLoggerFactory.Instance);

        // Act
        testTagHelperComponentTagHelper.Init(tagHelperContext);
        await testTagHelperComponentTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(3, incrementer);
    }

    [Fact]
    public void Init_InvokesTagHelperComponentInit()
    {
        // Arrange
        var tagHelperContext = new TagHelperContext(
            "head",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            "head",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));

        var testTagHelperComponentManager = new TagHelperComponentManager(new[]
        {
                new TestTagHelperComponent()
            });

        var testTagHelperComponentTagHelper = new TestTagHelperComponentTagHelper(testTagHelperComponentManager, NullLoggerFactory.Instance);

        // Act
        testTagHelperComponentTagHelper.Init(tagHelperContext);

        // Assert
        Assert.Equal("Value", tagHelperContext.Items["Key"]);
    }

    [Fact]
    public async Task ProcessAsync_InvokesTagHelperComponentProcessAsync()
    {
        // Arrange
        var tagHelperContext = new TagHelperContext(
            "head",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            "head",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));

        var testTagHelperComponentManager = new TagHelperComponentManager(new[]
        {
                new TestTagHelperComponent()
            });

        var testTagHelperComponentTagHelper = new TestTagHelperComponentTagHelper(testTagHelperComponentManager, NullLoggerFactory.Instance);

        // Act
        await testTagHelperComponentTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal("Processed1", output.PostContent.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_InvokesTagHelperComponentProcessAsync_WithAddedTagHelperComponents()
    {
        // Arrange
        var tagHelperContext = new TagHelperContext(
            "head",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            "head",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));

        var testTagHelperComponentManager = new TagHelperComponentManager(new[]
        {
                new TestTagHelperComponent()
            });

        testTagHelperComponentManager.Components.Add(new TestAddTagHelperComponent(0));
        testTagHelperComponentManager.Components.Add(new TestAddTagHelperComponent(2));
        var testTagHelperComponentTagHelper = new TestTagHelperComponentTagHelper(testTagHelperComponentManager, NullLoggerFactory.Instance);

        // Act
        await testTagHelperComponentTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal("Processed0Processed1Processed2", output.PostContent.GetContent());
    }

    [Fact]
    public void Init_LogsTagHelperComponentInitialized()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var tagHelperContext = new TagHelperContext(
            "head",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            "head",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));

        var testTagHelperComponentManager = new TagHelperComponentManager(new[]
        {
                new TestTagHelperComponent()
            });

        var testTagHelperComponentTagHelper = new TestTagHelperComponentTagHelper(testTagHelperComponentManager, loggerFactory);

        // Act
        testTagHelperComponentTagHelper.Init(tagHelperContext);

        // Assert
        Assert.Equal($"Tag helper component '{typeof(TestTagHelperComponent)}' initialized.", sink.Writes.First().State.ToString(), StringComparer.Ordinal);
    }

    [Fact]
    public async Task ProcessAsync_LogsTagHelperComponentProcessed()
    {
        // Arrange
        var sink = new TestSink();
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        var tagHelperContext = new TagHelperContext(
            "head",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var output = new TagHelperOutput(
            "head",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(
                new DefaultTagHelperContent()));

        var testTagHelperComponentManager = new TagHelperComponentManager(new[]
        {
                new TestTagHelperComponent()
            });

        var testTagHelperComponentTagHelper = new TestTagHelperComponentTagHelper(testTagHelperComponentManager, loggerFactory);

        // Act
        await testTagHelperComponentTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal($"Tag helper component '{typeof(TestTagHelperComponent)}' processed.", sink.Writes.First().State.ToString(), StringComparer.Ordinal);
    }

    [Fact]
    public void Init_GetsTagHelperComponentPropertyActivator_FromRequestServices()
    {
        // Arrange
        var tagHelperContext = new TagHelperContext(
            "head",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        var testTagHelperComponentManager = new TagHelperComponentManager(new[]
        {
                new TestTagHelperComponent()
            });

        var testTagHelperComponentTagHelper = new TestTagHelperComponentTagHelper(
            testTagHelperComponentManager,
            NullLoggerFactory.Instance);

        // Act
        testTagHelperComponentTagHelper.Init(tagHelperContext);

        // Assert
        Assert.NotNull(testTagHelperComponentTagHelper.PropertyActivator);
    }

    private class TestTagHelperComponentTagHelper : TagHelperComponentTagHelper
    {
        public TestTagHelperComponentTagHelper(
            ITagHelperComponentManager manager,
            ILoggerFactory loggerFactory)
            : base(manager, loggerFactory)
        {
            ViewContext = CreateViewContext();
        }
    }

    private class CallbackTagHelperComponent : ITagHelperComponent
    {
        private readonly Action _initCallback;
        private readonly Action _processAsyncCallback;
        private readonly int _order;

        public CallbackTagHelperComponent(int order, Action initCallback, Action processAsyncCallback)
        {
            _initCallback = initCallback;
            _processAsyncCallback = processAsyncCallback;
            _order = order;
        }

        public int Order => _order;

        public void Init(TagHelperContext context)
        {
            _initCallback();
        }

        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            _processAsyncCallback();
            return Task.CompletedTask;
        }
    }

    private class TestTagHelperComponent : ITagHelperComponent
    {
        public int Order => 1;

        public void Init(TagHelperContext context)
        {
            context.Items["Key"] = "Value";
        }

        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.PostContent.AppendHtml("Processed1");
            return Task.CompletedTask;
        }
    }

    private class TestAddTagHelperComponent : ITagHelperComponent
    {
        private readonly int _order;

        public TestAddTagHelperComponent(int order)
        {
            _order = order;
        }

        public int Order => _order;

        public void Init(TagHelperContext context)
        {
        }

        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.PostContent.AppendHtml("Processed" + Order);
            return Task.CompletedTask;
        }
    }

    private static ViewContext CreateViewContext()
    {
        var httpContext = new DefaultHttpContext()
        {
            RequestServices = new ServiceCollection()
            .AddSingleton<ITagHelperComponentPropertyActivator>(new TagHelperComponentPropertyActivator())
            .BuildServiceProvider()
        };

        var viewContext = Mock.Of<ViewContext>(vc => vc.HttpContext == httpContext);
        return viewContext;
    }
}
