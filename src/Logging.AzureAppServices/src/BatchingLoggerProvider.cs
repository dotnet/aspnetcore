// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureAppServices;

/// <summary>
/// A provider of <see cref="BatchingLogger"/> instances.
/// </summary>
public abstract class BatchingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly List<LogMessage> _currentBatch = new List<LogMessage>();
    private readonly TimeSpan _interval;
    private readonly int? _queueSize;
    private readonly int? _batchSize;
    private readonly IDisposable _optionsChangeToken;

    private int _messagesDropped;

    private BlockingCollection<LogMessage> _messageQueue;
    private Task _outputTask;
    private CancellationTokenSource _cancellationTokenSource;

    private bool _includeScopes;
    private IExternalScopeProvider _scopeProvider;

    internal IExternalScopeProvider ScopeProvider => _includeScopes ? _scopeProvider : null;

    internal BatchingLoggerProvider(IOptionsMonitor<BatchingLoggerOptions> options)
    {
        // NOTE: Only IsEnabled is monitored

        var loggerOptions = options.CurrentValue;
        if (loggerOptions.BatchSize <= 0)
        {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            throw new ArgumentOutOfRangeException(nameof(loggerOptions.BatchSize), $"{nameof(loggerOptions.BatchSize)} must be a positive number.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }
        if (loggerOptions.FlushPeriod <= TimeSpan.Zero)
        {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
            throw new ArgumentOutOfRangeException(nameof(loggerOptions.FlushPeriod), $"{nameof(loggerOptions.FlushPeriod)} must be longer than zero.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }

        _interval = loggerOptions.FlushPeriod;
        _batchSize = loggerOptions.BatchSize;
        _queueSize = loggerOptions.BackgroundQueueSize;

        _optionsChangeToken = options.OnChange(UpdateOptions);
        UpdateOptions(options.CurrentValue);
    }

    /// <summary>
    /// Checks if the queue is enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

    private void UpdateOptions(BatchingLoggerOptions options)
    {
        var oldIsEnabled = IsEnabled;
        IsEnabled = options.IsEnabled;
        _includeScopes = options.IncludeScopes;

        if (oldIsEnabled != IsEnabled)
        {
            if (IsEnabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

    }

    internal abstract Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken token);

    private async Task ProcessLogQueue()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            var limit = _batchSize ?? int.MaxValue;

            while (limit > 0 && _messageQueue.TryTake(out var message))
            {
                _currentBatch.Add(message);
                limit--;
            }

            var messagesDropped = Interlocked.Exchange(ref _messagesDropped, 0);
            if (messagesDropped != 0)
            {
                _currentBatch.Add(new LogMessage(DateTimeOffset.Now, $"{messagesDropped} message(s) dropped because of queue size limit. Increase the queue size or decrease logging verbosity to avoid this.{Environment.NewLine}"));
            }

            if (_currentBatch.Count > 0)
            {
                try
                {
                    await WriteMessagesAsync(_currentBatch, _cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }

                _currentBatch.Clear();
            }
            else
            {
                await IntervalAsync(_interval, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Wait for the given <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="interval">The amount of time to wait.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the delay.</param>
    /// <returns>A <see cref="Task"/> which completes when the <paramref name="interval"/> has passed or the <paramref name="cancellationToken"/> has been canceled.</returns>
    protected virtual Task IntervalAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        return Task.Delay(interval, cancellationToken);
    }

    internal void AddMessage(DateTimeOffset timestamp, string message)
    {
        if (!_messageQueue.IsAddingCompleted)
        {
            try
            {
                if (!_messageQueue.TryAdd(new LogMessage(timestamp, message), millisecondsTimeout: 0, cancellationToken: _cancellationTokenSource.Token))
                {
                    Interlocked.Increment(ref _messagesDropped);
                }
            }
            catch
            {
                //cancellation token canceled or CompleteAdding called
            }
        }
    }

    private void Start()
    {
        _messageQueue = _queueSize == null ?
            new BlockingCollection<LogMessage>(new ConcurrentQueue<LogMessage>()) :
            new BlockingCollection<LogMessage>(new ConcurrentQueue<LogMessage>(), _queueSize.Value);

        _cancellationTokenSource = new CancellationTokenSource();
        _outputTask = Task.Run(ProcessLogQueue);
    }

    private void Stop()
    {
        _cancellationTokenSource.Cancel();
        _messageQueue.CompleteAdding();

        try
        {
            _outputTask.Wait(_interval);
        }
        catch (TaskCanceledException)
        {
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException)
        {
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _optionsChangeToken?.Dispose();
        if (IsEnabled)
        {
            Stop();
        }
    }

    /// <summary>
    /// Creates a <see cref="BatchingLogger"/> with the given <paramref name="categoryName"/>.
    /// </summary>
    /// <param name="categoryName">The name of the category to create this logger with.</param>
    /// <returns>The <see cref="BatchingLogger"/> that was created.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return new BatchingLogger(this, categoryName);
    }

    /// <summary>
    /// Sets the scope on this provider.
    /// </summary>
    /// <param name="scopeProvider">Provides the scope.</param>
    void ISupportExternalScope.SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }
}
