// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.HeaderPropagation.Tests
{
    public class HeaderPropagationLoggerScopeMiddlewareTest
    {
        public HeaderPropagationLoggerScopeMiddlewareTest()
        {
            Context = new DefaultHttpContext();
            Next = ctx => Task.CompletedTask;
            Logger = new TestLogger<HeaderPropagationLoggerScopeMiddleware>();
            Builder = new TestHeaderPropagationLoggerScopeBuilder();

            Middleware = new HeaderPropagationLoggerScopeMiddleware(Next, Logger, Builder);
        }

        public DefaultHttpContext Context { get; set; }
        public RequestDelegate Next { get; set; }
        public TestLogger<HeaderPropagationLoggerScopeMiddleware> Logger { get; set; }
        public TestHeaderPropagationLoggerScopeBuilder Builder { get; set; }
        public HeaderPropagationLoggerScopeMiddleware Middleware { get; set; }

        [Fact]
        public async Task GetsScopeFromBuilderAndAddsItToLogger()
        {
            // Act
            await Middleware.Invoke(Context);

            // Assert
            Assert.NotNull(Logger.Scope);
            Assert.Same(Builder.Scope, Logger.Scope);
        }
    }

    public class TestHeaderPropagationLoggerScopeBuilder : IHeaderPropagationLoggerScopeBuilder
    {
        internal HeaderPropagationLoggerScope Scope { get; set; } =
            new HeaderPropagationLoggerScope(new List<string>(), new Dictionary<string, StringValues>());

        HeaderPropagationLoggerScope IHeaderPropagationLoggerScopeBuilder.Build() => Scope;
    }

    public class TestLogger<T> : ILogger<T>
    {
        public object Scope { get; private set; }

        public IDisposable BeginScope<TState>(TState state)
        {
            Scope = state;
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;


        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }
    }
}
