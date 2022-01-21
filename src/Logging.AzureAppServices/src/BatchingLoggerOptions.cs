// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.AzureAppServices;

/// <summary>
/// Options for a logger which batches up log messages.
/// </summary>
public class BatchingLoggerOptions
{
    private int? _batchSize;
    private int? _backgroundQueueSize = 1000;
    private TimeSpan _flushPeriod = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the period after which logs will be flushed to the store.
    /// </summary>
    public TimeSpan FlushPeriod
    {
        get { return _flushPeriod; }
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(FlushPeriod)} must be positive.");
            }
            _flushPeriod = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum size of the background log message queue or null for no limit.
    /// After maximum queue size is reached log event sink would start blocking.
    /// Defaults to <c>1000</c>.
    /// </summary>
    public int? BackgroundQueueSize
    {
        get { return _backgroundQueueSize; }
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(BackgroundQueueSize)} must be non-negative.");
            }
            _backgroundQueueSize = value;
        }
    }

    /// <summary>
    /// Gets or sets a maximum number of events to include in a single batch or null for no limit.
    /// </summary>
    /// Defaults to <c>null</c>.
    public int? BatchSize
    {
        get { return _batchSize; }
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(BatchSize)} must be positive.");
            }
            _batchSize = value;
        }
    }

    /// <summary>
    /// Gets or sets value indicating if logger accepts and queues writes.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether scopes should be included in the message.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IncludeScopes { get; set; }
}
