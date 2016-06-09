// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class HostingApplicationTests
    {
        [Fact]
        public void DisposeContextDoesNotThrowWhenContextScopeIsNull()
        {
            // Arrange
            var httpContextFactory = new HttpContextFactory(new DefaultObjectPoolProvider(), Options.Create(new FormOptions()), new HttpContextAccessor());
            var hostingApplication = new HostingApplication(ctx => Task.FromResult(0), new NullScopeLogger(), new NoopDiagnosticSource(), httpContextFactory);
            var context = hostingApplication.CreateContext(new FeatureCollection());

            // Act/Assert
            hostingApplication.DisposeContext(context, null);
        }

        private class NullScopeLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
            }
        }

        private class NoopDiagnosticSource : DiagnosticSource
        {
            public override bool IsEnabled(string name) => true;

            public override void Write(string name, object value)
            {
            }
        }
    }
}
