// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageDirectiveFeatureTest
    {
        [Fact]
        public void TryGetPageDirective_FindsTemplate()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Test.cshtml", @"@page ""Some/Path/{value}""
The rest of the thing");
            var sink = new TestSink();
            var logger = new TestLogger("logger", sink, enabled: true);

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(logger, projectItem, out var template));
            Assert.Equal("Some/Path/{value}", template);
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void TryGetPageDirective_NoNewLine()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Test.cshtml", @"@page ""Some/Path/{value}""");
            var sink = new TestSink();
            var logger = new TestLogger("logger", sink, enabled: true);

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(logger, projectItem, out var template));
            Assert.Equal("Some/Path/{value}", template);
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void TryGetPageDirective_JunkBeforeDirective()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Test.cshtml", @"Not a directive @page ""Some/Path/{value}""");
            var sink = new TestSink();
            var logger = new TestLogger("logger", sink, enabled: true);

            // Act & Assert
            Assert.False(PageDirectiveFeature.TryGetPageDirective(logger, projectItem, out var template));
            Assert.Null(template);
            Assert.Empty(sink.Writes);
        }

        [Theory]
        [InlineData(@"""Some/Path/{value}")]
        [InlineData(@"Some/Path/{value}""")]
        public void TryGetPageDirective_WithoutBothQuotes_LogsWarning(string inTemplate)
        {
            // Arrange
            var expected = "The page directive at 'Test.cshtml' is malformed. Please fix the following issues: The 'page' directive expects a string surrounded by double quotes.";
            var sink = new TestSink();
            var logger = new TestLogger("logger", sink, enabled: true);
            var projectItem = new TestRazorProjectItem("Test.cshtml", $@"@page {inTemplate}");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(logger, projectItem, out var template));
            Assert.Null(template);
            Assert.Collection(sink.Writes,
                log =>
                {
                    Assert.Equal(LogLevel.Warning, log.LogLevel);
                    Assert.Equal(expected, log.State.ToString());
                });
        }

        [Fact]
        public void TryGetPageDirective_NoQuotesAroundPath_LogsWarning()
        {
            // Arrange
            var expected = "The page directive at 'Test.cshtml' is malformed. Please fix the following issues: The 'page' directive expects a string surrounded by double quotes.";
            var sink = new TestSink();
            var logger = new TestLogger("logger", sink, enabled: true);
            var projectItem = new TestRazorProjectItem("Test.cshtml", @"@page Some/Path/{value}");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(logger, projectItem, out var template));
            Assert.Null(template);
            var logs = sink.Writes.Select(w => w.State.ToString().Trim()).ToList();
            Assert.Collection(sink.Writes,
                log =>
                {
                    Assert.Equal(LogLevel.Warning, log.LogLevel);
                    Assert.Equal(expected, log.State.ToString());
                });
        }

        [Fact]
        public void TryGetPageDirective_NewLineBeforeDirective()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Test.cshtml", "\n @page \"Some/Path/{value}\"");
            var sink = new TestSink();
            var logger = new TestLogger("logger", sink, enabled: true);

            // Act
            Assert.True(PageDirectiveFeature.TryGetPageDirective(logger, projectItem, out var template));
            Assert.Equal("Some/Path/{value}", template);
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void TryGetPageDirective_Directive_WithoutPathOrContent()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Test.cshtml", @"@page");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(NullLogger.Instance, projectItem, out var template));
            Assert.Null(template);
        }

        [Fact]
        public void TryGetPageDirective_DirectiveWithContent_WithoutPath()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Test.cshtml", @"@page
Non-path things");
            var sink = new TestSink();
            var logger = new TestLogger("logger", sink, enabled: true);

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(logger, projectItem, out var template));
            Assert.Null(template);
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void TryGetPageDirective_NoDirective()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("Test.cshtml", @"This is junk
Nobody will use it");
            var sink = new TestSink();
            var logger = new TestLogger("logger", sink, enabled: true);

            // Act & Assert
            Assert.False(PageDirectiveFeature.TryGetPageDirective(logger, projectItem, out var template));
            Assert.Null(template);
            Assert.Empty(sink.Writes);
        }
    }
}
