// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

public class TagHelperRunnerTest
{
    [Fact]
    public async Task RunAsync_CallsInitPriorToProcessAsync()
    {
        // Arrange
        var runner = new TagHelperRunner();
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
        var incrementer = 0;
        var callbackTagHelper = new CallbackTagHelper(
            initCallback: () =>
            {
                Assert.Equal(0, incrementer);

                incrementer++;
            },
            processAsyncCallback: () =>
            {
                Assert.Equal(1, incrementer);

                incrementer++;
            });
        executionContext.Add(callbackTagHelper);

        // Act
        await runner.RunAsync(executionContext);

        // Assert
        Assert.Equal(2, incrementer);
    }

    public static TheoryData<int[], int[]> TagHelperOrderData
    {
        get
        {
            // tagHelperOrders, expectedTagHelperOrders
            return new()
                {
                    {
                        new[] { 1000, int.MaxValue, 0 },
                        new[] { 0, 1000, int.MaxValue }
                    },
                    {
                        new[] { int.MaxValue, int.MaxValue, int.MinValue },
                        new[] { int.MinValue, int.MaxValue, int.MaxValue }
                    },
                    {
                        new[] { 0, 0, int.MinValue },
                        new[] { int.MinValue, 0, 0 }
                    },
                    {
                        new[] { int.MinValue, -1000, 0 },
                        new[] { int.MinValue, -1000, 0 }
                    },
                    {
                        new[] { 0, 1000, int.MaxValue },
                        new[] { 0, 1000, int.MaxValue }
                    },
                    {
                        new[] { int.MaxValue, int.MinValue, int.MaxValue, -1000, int.MaxValue, 0 },
                        new[] { int.MinValue, -1000, 0, int.MaxValue, int.MaxValue, int.MaxValue }
                    },
                    {
                        new[] { 0, 0, 0, 0 },
                        new[] { 0, 0, 0, 0 }
                    },

                    {
                        new[] { 1000, int.MaxValue, 0, -1000, int.MinValue },
                        new[] { int.MinValue, -1000, 0, 1000, int.MaxValue }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(TagHelperOrderData))]
    public async Task RunAsync_OrdersTagHelpers(
        int[] tagHelperOrders,
        int[] expectedTagHelperOrders)
    {
        // Arrange
        var runner = new TagHelperRunner();
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
        var processOrder = new List<int>();

        foreach (var order in tagHelperOrders)
        {
            var orderedTagHelper = new OrderedTagHelper(order)
            {
                ProcessOrderTracker = processOrder
            };
            executionContext.Add(orderedTagHelper);
        }

        // Act
        await runner.RunAsync(executionContext);

        // Assert
        Assert.Equal(expectedTagHelperOrders, processOrder);
    }

    [Theory]
    [InlineData(TagMode.SelfClosing)]
    [InlineData(TagMode.StartTagAndEndTag)]
    [InlineData(TagMode.StartTagOnly)]
    public async Task RunAsync_SetsTagHelperOutputTagMode(TagMode tagMode)
    {
        // Arrange
        var runner = new TagHelperRunner();
        var executionContext = new TagHelperExecutionContext("p", tagMode);
        var tagHelper = new TagHelperContextTouchingTagHelper();

        executionContext.Add(tagHelper);
        executionContext.AddTagHelperAttribute("foo", true, HtmlAttributeValueStyle.DoubleQuotes);

        // Act
        await runner.RunAsync(executionContext);

        // Assert
        Assert.Equal(tagMode, executionContext.Output.TagMode);
    }

    [Fact]
    public async Task RunAsync_ProcessesAllTagHelpers()
    {
        // Arrange
        var runner = new TagHelperRunner();
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
        var executableTagHelper1 = new ExecutableTagHelper();
        var executableTagHelper2 = new ExecutableTagHelper();

        // Act
        executionContext.Add(executableTagHelper1);
        executionContext.Add(executableTagHelper2);
        await runner.RunAsync(executionContext);

        // Assert
        Assert.True(executableTagHelper1.Processed);
        Assert.True(executableTagHelper2.Processed);
    }

    [Fact]
    public async Task RunAsync_AllowsModificationOfTagHelperOutput()
    {
        // Arrange
        var runner = new TagHelperRunner();
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
        var executableTagHelper = new ExecutableTagHelper();

        // Act
        executionContext.Add(executableTagHelper);
        executionContext.AddHtmlAttribute("class", "btn", HtmlAttributeValueStyle.DoubleQuotes);
        await runner.RunAsync(executionContext);

        // Assert
        var output = executionContext.Output;
        Assert.Equal("foo", output.TagName);
        Assert.Equal("somethingelse", output.Attributes["class"].Value);
        Assert.Equal("world", output.Attributes["hello"].Value);
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
    }

    [Fact]
    public async Task RunAsync_AllowsDataRetrievalFromTagHelperContext()
    {
        // Arrange
        var runner = new TagHelperRunner();
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
        var tagHelper = new TagHelperContextTouchingTagHelper();

        // Act
        executionContext.Add(tagHelper);
        executionContext.AddTagHelperAttribute("foo", true, HtmlAttributeValueStyle.DoubleQuotes);
        await runner.RunAsync(executionContext);

        // Assert
        Assert.Equal("True", executionContext.Output.Attributes["foo"].Value);
    }

    [Fact]
    public async Task RunAsync_ConfiguresTagHelperContextWithExecutionContextsItems()
    {
        // Arrange
        var runner = new TagHelperRunner();
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
        var tagHelper = new ContextInspectingTagHelper();
        executionContext.Add(tagHelper);

        // Act
        await runner.RunAsync(executionContext);

        // Assert
        Assert.NotNull(tagHelper.ContextProcessedWith);
        Assert.Same(tagHelper.ContextProcessedWith.Items, executionContext.Items);
    }

    private class ExecutableTagHelper : TagHelper
    {
        public bool Processed { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Processed = true;

            output.TagName = "foo";

            var classIndex = output.Attributes.IndexOfName("class");
            if (classIndex != -1)
            {
                output.Attributes[classIndex] = new TagHelperAttribute("class", "somethingelse");
            }

            output.Attributes.Add("hello", "world");
            output.TagMode = TagMode.SelfClosing;
        }
    }

    private class ContextInspectingTagHelper : TagHelper
    {
        public TagHelperContext ContextProcessedWith { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            ContextProcessedWith = context;
        }
    }

    private class TagHelperContextTouchingTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.Add("foo", context.AllAttributes["foo"].Value.ToString());
        }
    }

    private class OrderedTagHelper : TagHelper
    {
        public OrderedTagHelper(int order)
        {
            Order = order;
        }

        public override int Order { get; }
        public IList<int> ProcessOrderTracker { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // If using this class for testing, ensure that ProcessOrderTracker is always set prior to Process
            // execution.
            Debug.Assert(ProcessOrderTracker != null);

            ProcessOrderTracker.Add(Order);
        }
    }

    private class CallbackTagHelper : TagHelper
    {
        private readonly Action _initCallback;
        private readonly Action _processAsyncCallback;

        public CallbackTagHelper(Action initCallback, Action processAsyncCallback)
        {
            _initCallback = initCallback;
            _processAsyncCallback = processAsyncCallback;
        }

        public override void Init(TagHelperContext context)
        {
            _initCallback();
        }

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            _processAsyncCallback();

            return base.ProcessAsync(context, output);
        }
    }
}
