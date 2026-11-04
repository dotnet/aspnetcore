// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Mvc;

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecutingHandlerMethod_LogsHandlerNameAndModelState(bool isValidModelState)
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        var context = new PageContext();
        if (!isValidModelState)
        {
            context.ModelState.AddModelError("foo", "bar");
        }
        var handler = new HandlerMethodDescriptor
        {
            // Using a generic type to verify the use of a clean name
            MethodInfo = typeof(ValueTuple<int, string>).GetMethod(nameof(ToString)),
        };

        // Act
        logger.ExecutingHandlerMethod(context, handler, null);

        // Assert
        var write = Assert.Single(testSink.Writes);
        var validationState = isValidModelState ? "Valid" : "Invalid";
        Assert.Equal(
            $"Executing handler method System.ValueTuple<int, string>.ToString - ModelState is {validationState}",
            write.State.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("foo", "foo")]
    [InlineData("foo, 42", "foo", 42)]
    public void ExecutingHandlerMethod_WithArguments_LogsArguments(string expectedArgumentsMessage, params object[] arguments)
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        var context = new PageContext();
        var handler = new HandlerMethodDescriptor
        {
            // Using a generic type to verify the use of a clean name
            MethodInfo = typeof(ValueTuple<int, string>).GetMethod(nameof(ToString)),
        };

        // Act
        logger.ExecutingHandlerMethod(context, handler, arguments);

        // Assert
        Assert.Equal(2, testSink.Writes.Count);
        var enumerator = testSink.Writes.GetEnumerator();
        enumerator.MoveNext();
        enumerator.MoveNext();
        Assert.Equal(
            $"Executing handler method System.ValueTuple<int, string>.ToString with arguments ({expectedArgumentsMessage})",
            enumerator.Current.State.ToString());
    }
}
