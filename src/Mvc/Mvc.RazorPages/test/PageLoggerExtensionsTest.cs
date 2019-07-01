// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class PageLoggerExtensionsTest
    {
        [Fact]
        public void ExecutingPageFactory_LogsPageName()
        {
            // Arrange
            var testSink = new TestSink();
            var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
            var logger = loggerFactory.CreateLogger("test");

            var context = new PageContext
            {
                ActionDescriptor = new CompiledPageActionDescriptor
                {
                    // Using a generic type to verify the use of a clean name
                    PageTypeInfo = typeof(ValueTuple<int, string>).GetTypeInfo()
                }
            };

            // Act
            logger.ExecutingPageFactory(context);

            // Assert
            var write = Assert.Single(testSink.Writes);
            Assert.Equal(
                "Executing page factory for page " +
                "System.ValueTuple<int, string> (System.Private.CoreLib)",
                write.State.ToString());
        }

        [Fact]
        public void ExecutedPageFactory_LogsPageName()
        {
            // Arrange
            var testSink = new TestSink();
            var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
            var logger = loggerFactory.CreateLogger("test");

            var context = new PageContext
            {
                ActionDescriptor = new CompiledPageActionDescriptor
                {
                    // Using a generic type to verify the use of a clean name
                    PageTypeInfo = typeof(ValueTuple<int, string>).GetTypeInfo()
                }
            };

            // Act
            logger.ExecutedPageFactory(context);

            // Assert
            var write = Assert.Single(testSink.Writes);
            Assert.Equal(
                "Executed page factory for page " +
                "System.ValueTuple<int, string> (System.Private.CoreLib)",
                write.State.ToString());
        }

        [Fact]
        public void ExecutingPageModelFactory_LogsPageName()
        {
            // Arrange
            var testSink = new TestSink();
            var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
            var logger = loggerFactory.CreateLogger("test");

            var context = new PageContext
            {
                ActionDescriptor = new CompiledPageActionDescriptor
                {
                    // Using a generic type to verify the use of a clean name
                    PageTypeInfo = typeof(ValueTuple<int, string>).GetTypeInfo()
                }
            };

            // Act
            logger.ExecutingPageModelFactory(context);

            // Assert
            var write = Assert.Single(testSink.Writes);
            Assert.Equal(
                "Executing page model factory for page " +
                "System.ValueTuple<int, string> (System.Private.CoreLib)",
                write.State.ToString());
        }

        [Fact]
        public void ExecutedPageModelFactory_LogsPageName()
        {
            // Arrange
            var testSink = new TestSink();
            var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
            var logger = loggerFactory.CreateLogger("test");

            var context = new PageContext
            {
                ActionDescriptor = new CompiledPageActionDescriptor
                {
                    // Using a generic type to verify the use of a clean name
                    PageTypeInfo = typeof(ValueTuple<int, string>).GetTypeInfo()
                }
            };

            // Act
            logger.ExecutedPageModelFactory(context);

            // Assert
            var write = Assert.Single(testSink.Writes);
            Assert.Equal(
                "Executed page model factory for page " +
                "System.ValueTuple<int, string> (System.Private.CoreLib)",
                write.State.ToString());
        }
    }
}
