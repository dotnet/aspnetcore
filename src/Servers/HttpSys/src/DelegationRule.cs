// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Rule that maintains a handle to the Request Queue and UrlPrefix to
/// delegate to.
/// </summary>
public class DelegationRule : IDisposable
{
    private readonly ILogger _logger;
    private readonly UrlGroup _sourceQueueUrlGroup;
    private bool _disposed;

    /// <summary>
    /// The name of the Http.Sys request queue
    /// </summary>
    public string QueueName { get; }

    /// <summary>
    /// The URL of the Http.Sys Url Prefix
    /// </summary>
    public string UrlPrefix { get; }

    internal RequestQueue Queue { get; }

    internal DelegationRule(UrlGroup sourceQueueUrlGroup, string queueName, string urlPrefix, ILogger logger)
    {
        _sourceQueueUrlGroup = sourceQueueUrlGroup;
        _logger = logger;
        QueueName = queueName;
        UrlPrefix = urlPrefix;
        Queue = new RequestQueue(queueName, _logger);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _sourceQueueUrlGroup.UnSetDelegationProperty(Queue, throwOnError: false);
        }
        catch (ObjectDisposedException) { /* Server may have been shutdown */ }
        Queue.Dispose();
    }
}
