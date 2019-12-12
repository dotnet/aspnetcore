// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Ignitor;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components
{
    // Base class for Ignitor-based tests.
    public abstract class IgnitorTest<TStartup> : IClassFixture<BasicTestAppServerSiteFixture<TStartup>>, IAsyncLifetime
        where TStartup: class
    {
        private static readonly TimeSpan DefaultTimeout = Debugger.IsAttached ? TimeSpan.MaxValue : TimeSpan.FromSeconds(30);

        protected IgnitorTest(BasicTestAppServerSiteFixture<TStartup> serverFixture, ITestOutputHelper output)
        {
            ServerFixture = serverFixture;
            Output = output;
        }

        protected BlazorClient Client { get; private set; }

        protected ConcurrentQueue<LogMessage> Logs { get; } = new ConcurrentQueue<LogMessage>();

        protected ITestOutputHelper Output { get; }

        protected BasicTestAppServerSiteFixture<TStartup> ServerFixture { get; }

        protected TimeSpan Timeout { get; set; } = DefaultTimeout;

        private TestSink TestSink { get; set; }

        protected IReadOnlyCollection<CapturedRenderBatch> Batches => Client?.Operations?.Batches;

        protected IReadOnlyCollection<string> DotNetCompletions => Client?.Operations?.DotNetCompletions;

        protected IReadOnlyCollection<string> Errors => Client?.Operations?.Errors;

        protected IReadOnlyCollection<CapturedJSInteropCall> JSInteropCalls => Client?.Operations?.JSInteropCalls;

        async Task IAsyncLifetime.InitializeAsync()
        {
            Client = new BlazorClient()
            {
                CaptureOperations = true,
                DefaultOperationTimeout = Timeout,
            };
            Client.LoggerProvider = new XunitLoggerProvider(Output);
            Client.FormatError = (error) =>
            {
                var logs = string.Join(Environment.NewLine, Logs);
                return new Exception(error + Environment.NewLine + logs);
            };
            
            _ = ServerFixture.RootUri; // This is needed for the side-effects of starting the server.

            if (ServerFixture is WebHostServerFixture hostFixture)
            {
                TestSink = hostFixture.Host.Services.GetRequiredService<TestSink>();
                TestSink.MessageLogged += TestSink_MessageLogged;
            }

            await InitializeAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync();
        }

        protected virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        protected async virtual Task DisposeAsync()
        {
            if (TestSink != null)
            {
                TestSink.MessageLogged -= TestSink_MessageLogged;
            }

            await Client.DisposeAsync();
        }

        private void TestSink_MessageLogged(WriteContext context)
        {
            var log = new LogMessage(context.LogLevel, context.EventId, context.Message, context.Exception);
            Logs.Enqueue(log);
            try
            {
                // This might produce an InvalidOperationException when the logger tries to log a message after
                // the test has completed but before the handler has been removed.
                //  ---> System.InvalidOperationException: There is no currently active test.
                // For that reason, we capture the exception here and silence it, as the message is captured inside the Logs
                // variable anyway.
                Output.WriteLine(log.ToString());
            }
            catch (Exception)
            {
            }
        }

        [DebuggerDisplay("{LogLevel.ToString(),nq} - {Message ?? \"null\",nq} - {Exception?.Message,nq}")]
        protected sealed class LogMessage
        {
            public LogMessage(LogLevel logLevel, EventId eventId, string message, Exception exception)
            {
                LogLevel = logLevel;
                EventId = eventId;
                Message = message;
                Exception = exception;
            }

            public LogLevel LogLevel { get; set; }
            public EventId EventId { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }

            public override string ToString()
            {
                return $"{LogLevel}: {EventId} {Message}{(Exception != null ? Environment.NewLine : "")}{Exception}";
            }
        }
    }
}
