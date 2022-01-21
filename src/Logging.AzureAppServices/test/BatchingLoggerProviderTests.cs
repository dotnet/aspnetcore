// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test;

public class BatchingLoggerProviderTests
{
    private DateTimeOffset _timestampOne = new DateTimeOffset(2016, 05, 04, 03, 02, 01, TimeSpan.Zero);
    private string _nl = Environment.NewLine;
    private Regex _timeStampRegex = new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{3} .\d{2}:\d{2} ");

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
    public async Task IncludesScopes()
    {
        var provider = new TestBatchingLoggingProvider(includeScopes: true);
        var factory = new LoggerFactory(new[] { provider });
        var logger = factory.CreateLogger("Cat");

        await provider.IntervalControl.Pause;

        using (logger.BeginScope("Scope"))
        {
            using (logger.BeginScope("Scope2"))
            {
                logger.Log(LogLevel.Information, 0, "Info message", null, (state, ex) => state);
            }
        }

        provider.IntervalControl.Resume();
        await provider.IntervalControl.Pause;

        Assert.Matches(_timeStampRegex, provider.Batches[0][0].Message);
        Assert.EndsWith(
            " [Information] Cat => Scope => Scope2:" + _nl +
            "Info message" + _nl,
            provider.Batches[0][0].Message);
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

        Assert.Equal(2, provider.Batches.Count);
        Assert.Single(provider.Batches[0]);
        Assert.Equal("2016-05-04 03:02:01.000 +00:00 [Information] Cat: Info message" + _nl, provider.Batches[0][0].Message);

        Assert.Single(provider.Batches[1]);
        Assert.Equal("2016-05-04 04:02:01.000 +00:00 [Error] Cat: Error message" + _nl, provider.Batches[1][0].Message);
    }

    [Fact]
    public async Task DropsMessagesWhenReachingMaxQueue()
    {
        var provider = new TestBatchingLoggingProvider(maxQueueSize: 1);
        var logger = (BatchingLogger)provider.CreateLogger("Cat");

        await provider.IntervalControl.Pause;

        logger.Log(_timestampOne, LogLevel.Information, 0, "Info message", null, (state, ex) => state);
        logger.Log(_timestampOne.AddHours(1), LogLevel.Error, 0, "Error message", null, (state, ex) => state);

        provider.IntervalControl.Resume();
        await provider.IntervalControl.Pause;

        Assert.Equal(2, provider.Batches[0].Length);
        Assert.Equal("2016-05-04 03:02:01.000 +00:00 [Information] Cat: Info message" + _nl, provider.Batches[0][0].Message);
        Assert.Equal("1 message(s) dropped because of queue size limit. Increase the queue size or decrease logging verbosity to avoid this." + _nl, provider.Batches[0][1].Message);
    }

    private class TestBatchingLoggingProvider : BatchingLoggerProvider
    {
        public List<LogMessage[]> Batches { get; } = new List<LogMessage[]>();
        public ManualIntervalControl IntervalControl { get; } = new ManualIntervalControl();

        public TestBatchingLoggingProvider(TimeSpan? interval = null, int? maxBatchSize = null, int? maxQueueSize = null, bool includeScopes = false)
            : base(new OptionsWrapperMonitor<BatchingLoggerOptions>(new BatchingLoggerOptions
            {
                FlushPeriod = interval ?? TimeSpan.FromSeconds(1),
                BatchSize = maxBatchSize,
                BackgroundQueueSize = maxQueueSize,
                IsEnabled = true,
                IncludeScopes = includeScopes
            }))
        {
        }

        internal override Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken token)
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
