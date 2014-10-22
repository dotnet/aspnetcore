// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.AspNet.Razor.Text;
#if !ASPNETCORE50
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Parser
{
    public class CallbackParserListenerTest
    {
        [Fact]
        public void ListenerConstructedWithSpanCallbackCallsCallbackOnEndSpan()
        {
            RunOnEndSpanTest(callback => new CallbackVisitor(callback));
        }

        [Fact]
        public void ListenerConstructedWithSpanCallbackDoesNotThrowOnStartBlockEndBlockOrError()
        {
            // Arrange
            Action<Span> spanCallback = _ => { };
            var listener = new CallbackVisitor(spanCallback);

            // Act/Assert
            listener.VisitStartBlock(new FunctionsBlock());
            listener.VisitError(new RazorError("Error", SourceLocation.Zero));
            listener.VisitEndBlock(new FunctionsBlock());
        }

        [Fact]
        public void ListenerConstructedWithSpanAndErrorCallbackCallsCallbackOnEndSpan()
        {
            RunOnEndSpanTest(spanCallback => new CallbackVisitor(spanCallback, _ => { }));
        }

        [Fact]
        public void ListenerConstructedWithSpanAndErrorCallbackCallsCallbackOnError()
        {
            RunOnErrorTest(errorCallback => new CallbackVisitor(_ => { }, errorCallback));
        }

        [Fact]
        public void ListenerConstructedWithAllCallbacksCallsCallbackOnEndSpan()
        {
            RunOnEndSpanTest(spanCallback => new CallbackVisitor(spanCallback, _ => { }, _ => { }, _ => { }));
        }

        [Fact]
        public void ListenerConstructedWithAllCallbacksCallsCallbackOnError()
        {
            RunOnErrorTest(errorCallback => new CallbackVisitor(_ => { }, errorCallback, _ => { }, _ => { }));
        }

        [Fact]
        public void ListenerConstructedWithAllCallbacksCallsCallbackOnStartBlock()
        {
            RunOnStartBlockTest(startBlockCallback => new CallbackVisitor(_ => { }, _ => { }, startBlockCallback, _ => { }));
        }

        [Fact]
        public void ListenerConstructedWithAllCallbacksCallsCallbackOnEndBlock()
        {
            RunOnEndBlockTest(endBlockCallback => new CallbackVisitor(_ => { }, _ => { }, _ => { }, endBlockCallback));
        }

#if !ASPNETCORE50
        [Fact]
        public void ListenerCallsOnEndSpanCallbackUsingSynchronizationContextIfSpecified()
        {
            RunSyncContextTest(new SpanBuilder().Build(),
                               spanCallback => new CallbackVisitor(spanCallback, _ => { }, _ => { }, _ => { }),
                               (listener, expected) => listener.VisitSpan(expected));
        }

        [Fact]
        public void ListenerCallsOnStartBlockCallbackUsingSynchronizationContextIfSpecified()
        {
            RunSyncContextTest(BlockType.Template,
                               startBlockCallback => new CallbackVisitor(_ => { }, _ => { }, startBlockCallback, _ => { }),
                               (listener, expected) => listener.VisitStartBlock(new BlockBuilder() { Type = expected }.Build()));
        }

        [Fact]
        public void ListenerCallsOnEndBlockCallbackUsingSynchronizationContextIfSpecified()
        {
            RunSyncContextTest(BlockType.Template,
                               endBlockCallback => new CallbackVisitor(_ => { }, _ => { }, _ => { }, endBlockCallback),
                               (listener, expected) => listener.VisitEndBlock(new BlockBuilder() { Type = expected }.Build()));
        }

        [Fact]
        public void ListenerCallsOnErrorCallbackUsingSynchronizationContextIfSpecified()
        {
            RunSyncContextTest(new RazorError("Bar", 42, 42, 42),
                               errorCallback => new CallbackVisitor(_ => { }, errorCallback, _ => { }, _ => { }),
                               (listener, expected) => listener.VisitError(expected));
        }

        private static void RunSyncContextTest<T>(T expected, Func<Action<T>, CallbackVisitor> ctor, Action<CallbackVisitor, T> call)
        {
            // Arrange
            Mock<SynchronizationContext> mockContext = new Mock<SynchronizationContext>();
            mockContext.Setup(c => c.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
                .Callback<SendOrPostCallback, object>((callback, state) => { callback(expected); });

            // Act/Assert
            RunCallbackTest<T>(default(T), callback =>
            {
                var listener = ctor(callback);
                listener.SynchronizationContext = mockContext.Object;
                return listener;
            }, call, (original, actual) =>
            {
                Assert.NotEqual(original, actual);
                Assert.Equal(expected, actual);
            });
        }
#endif

        private static void RunOnStartBlockTest(Func<Action<BlockType>, CallbackVisitor> ctor, Action<BlockType, BlockType> verifyResults = null)
        {
            RunCallbackTest(BlockType.Markup, ctor, (listener, expected) => listener.VisitStartBlock(new BlockBuilder() { Type = expected }.Build()), verifyResults);
        }

        private static void RunOnEndBlockTest(Func<Action<BlockType>, CallbackVisitor> ctor, Action<BlockType, BlockType> verifyResults = null)
        {
            RunCallbackTest(BlockType.Markup, ctor, (listener, expected) => listener.VisitEndBlock(new BlockBuilder() { Type = expected }.Build()), verifyResults);
        }

        private static void RunOnErrorTest(Func<Action<RazorError>, CallbackVisitor> ctor, Action<RazorError, RazorError> verifyResults = null)
        {
            RunCallbackTest(new RazorError("Foo", SourceLocation.Zero), ctor, (listener, expected) => listener.VisitError(expected), verifyResults);
        }

        private static void RunOnEndSpanTest(Func<Action<Span>, CallbackVisitor> ctor, Action<Span, Span> verifyResults = null)
        {
            RunCallbackTest(new SpanBuilder().Build(), ctor, (listener, expected) => listener.VisitSpan(expected), verifyResults);
        }

        private static void RunCallbackTest<T>(T expected, Func<Action<T>, CallbackVisitor> ctor, Action<CallbackVisitor, T> call, Action<T, T> verifyResults = null)
        {
            // Arrange
            object actual = null;
            Action<T> callback = t => actual = t;

            var listener = ctor(callback);

            // Act
            call(listener, expected);

            // Assert
            if (verifyResults == null)
            {
                Assert.Equal(expected, actual);
            }
            else
            {
                verifyResults(expected, (T)actual);
            }
        }
    }
}
