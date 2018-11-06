// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.AzureAppServices.Internal;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test
{
    public class BatchingLoggerProviderTests
    {
        DateTimeOffset _timestampOne = new DateTimeOffset(2016, 05, 04, 03, 02, 01, TimeSpan.Zero);
        string _nl = Environment.NewLine;

        [Fact]
        public async Task LogsInIntervals()
        {
            var provider = new TestBatchingLoggingProvider();
            var logger = (BatchingLogger)provider.CreateLogger("Cat");

            await provider.IntervalControl.Pause;

            logger.Log(_timestampOne, LogLevel.Information, 0, "Info message", null, (state, ex) => state);
            logger.Log(_timestampOne.AddHours(1), LogLevel.Error, 0, "Error message", null, (state, ex) => state);

            provider.IntervalControl.Resume();
            await provider.IntervalControl.Pause;

            Assert.Equal("2016-05-04 03:02:01.000 +00:00 [Information] Cat: Info message" + _nl, provider.Batches[0][0].Message);
            Assert.Equal("2016-05-04 04:02:01.000 +00:00 [Error] Cat: Error message" + _nl, provider.Batches[0][1].Message);
        }

        [Fact]
        public async Task RespectsBatchSize()
        {
            var provider = new TestBatchingLoggingProvider(maxBatchSize: 1);
            var logger = (BatchingLogger)provider.CreateLogger("Cat");

            await provider.IntervalControl.Pause;

            logger.Log(_timestampOne, LogLevel.Information, 0, "Info message", null, (state, ex) => state);
            logger.Log(_timestampOne.AddHours(1), LogLevel.Error, 0, "Error message", null, (state, ex) => state);

            provider.IntervalControl.Resume();
            await provider.IntervalControl.Pause;

            Assert.Single(provider.Batches);
            Assert.Single(provider.Batches[0]);
            Assert.Equal("2016-05-04 03:02:01.000 +00:00 [Information] Cat: Info message" + _nl, provider.Batches[0][0].Message);

            provider.IntervalControl.Resume();
            await provider.IntervalControl.Pause;

            Assert.Equal(2, provider.Batches.Count);
            Assert.Single(provider.Batches[1]);

            Assert.Equal("2016-05-04 04:02:01.000 +00:00 [Error] Cat: Error message" + _nl, provider.Batches[1][0].Message);
        }

        [Fact]
        public async Task BlocksWhenReachingMaxQueue()
        {
            var provider = new TestBatchingLoggingProvider(maxQueueSize: 1);
            var logger = (BatchingLogger)provider.CreateLogger("Cat");

            await provider.IntervalControl.Pause;

            logger.Log(_timestampOne, LogLevel.Information, 0, "Info message", null, (state, ex) => state);
            var task = Task.Run(() => logger.Log(_timestampOne.AddHours(1), LogLevel.Error, 0, "Error message", null, (state, ex) => state));

            Assert.False(task.Wait(1000));

            provider.IntervalControl.Resume();
            await provider.IntervalControl.Pause;

            Assert.True(task.Wait(1000));
        }

        private class TestBatchingLoggingProvider: BatchingLoggerProvider
        {
            public List<LogMessage[]> Batches { get; } = new List<LogMessage[]>();
            public ManualIntervalControl IntervalControl { get; } = new ManualIntervalControl();

            public TestBatchingLoggingProvider(TimeSpan? interval = null, int? maxBatchSize = null, int? maxQueueSize = null)
                : base(new OptionsWrapperMonitor<BatchingLoggerOptions>(new BatchingLoggerOptions
                {
                    FlushPeriod = interval ?? TimeSpan.FromSeconds(1),
                    BatchSize = maxBatchSize,
                    BackgroundQueueSize = maxQueueSize,
                    IsEnabled = true
                }))
            {
            }

            protected override Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken token)
            {
                Batches.Add(messages.ToArray());
                return Task.CompletedTask;
            }

            protected override Task IntervalAsync(TimeSpan interval, CancellationToken cancellationToken)
            {
                return IntervalControl.IntervalAsync();
            }
        }
    }
}
