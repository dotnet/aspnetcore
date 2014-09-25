// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperRunnerTest
    {
        [Fact]
        public async Task RunAsync_ProcessesAllTagHelpers()
        {
            // Arrange
            var runner = new TagHelperRunner();
            var executionContext = new TagHelpersExecutionContext("p");
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
            var executionContext = new TagHelpersExecutionContext("p");
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
            var executionContext = new TagHelpersExecutionContext("p");
            var tagHelper = new TagHelperContextTouchingTagHelper();

            // Act
            executionContext.Add(tagHelper);
            executionContext.AddTagHelperAttribute("foo", true);
            var output = await runner.RunAsync(executionContext);

            // Assert
            Assert.Equal("True", output.Attributes["foo"]);
        }

        [Fact]
        public async Task RunAsync_WithContentSetsOutputsContent()
        {
            // Arrange
            var runner = new TagHelperRunner();
            var executionContext = new TagHelpersExecutionContext("p");
            var tagHelper = new ExecutableTagHelper();
            var contentWriter = new StringWriter(new StringBuilder("Hello World"));

            // Act
            executionContext.Add(tagHelper);
            var output = await runner.RunAsync(executionContext, contentWriter);

            // Assert
            Assert.Equal(output.Content, "Hello World");
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

        private class TagHelperContextTouchingTagHelper : TagHelper
        {
            public override void Process(TagHelperContext context, TagHelperOutput output)
            {
                output.Attributes["foo"] = context.AllAttributes["foo"].ToString();
            }
        }
    }
}