// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperRunnerTest
    {
        public static TheoryData TagHelperOrderData
        {
            get
            {
                // tagHelperOrders, expectedTagHelperOrders
                return new TheoryData<int[], int[]>
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
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
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
        [InlineData(true)]
        [InlineData(false)]
        public async Task RunAsync_SetTagHelperOutputSelfClosing(bool selfClosing)
        {
            // Arrange
            var runner = new TagHelperRunner();
            var executionContext = new TagHelperExecutionContext("p", selfClosing);
            var tagHelper = new TagHelperContextTouchingTagHelper();

            executionContext.Add(tagHelper);
            executionContext.AddTagHelperAttribute("foo", true);

            // Act
            var output = await runner.RunAsync(executionContext);

            // Assert
            Assert.Equal(selfClosing, output.SelfClosing);
        }

        [Fact]
        public async Task RunAsync_ProcessesAllTagHelpers()
        {
            // Arrange
            var runner = new TagHelperRunner();
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
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
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
            var executableTagHelper = new ExecutableTagHelper();

            // Act
            executionContext.Add(executableTagHelper);
            executionContext.AddHtmlAttribute("class", "btn");
            var output = await runner.RunAsync(executionContext);

            // Assert
            Assert.Equal("foo", output.TagName);
            Assert.Equal("somethingelse", output.Attributes["class"]);
            Assert.Equal("world", output.Attributes["hello"]);
            Assert.Equal(true, output.SelfClosing);
        }

        [Fact]
        public async Task RunAsync_AllowsDataRetrievalFromTagHelperContext()
        {
            // Arrange
            var runner = new TagHelperRunner();
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
            var tagHelper = new TagHelperContextTouchingTagHelper();

            // Act
            executionContext.Add(tagHelper);
            executionContext.AddTagHelperAttribute("foo", true);
            var output = await runner.RunAsync(executionContext);

            // Assert
            Assert.Equal("True", output.Attributes["foo"]);
        }

        [Fact]
        public async Task RunAsync_ConfiguresTagHelperContextWithExecutionContextsItems()
        {
            // Arrange
            var runner = new TagHelperRunner();
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
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
                output.Attributes["class"] = "somethingelse";
                output.Attributes["hello"] = "world";
                output.SelfClosing = true;
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
                output.Attributes["foo"] = context.AllAttributes["foo"].ToString();
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
    }
}