// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Allows consumers to perform cleanup during a graceful shutdown.
/// </summary>
[DebuggerDisplay("ApplicationStarted = {ApplicationStarted.IsCancellationRequested}, " +
    "ApplicationStopping = {ApplicationStopping.IsCancellationRequested}, " +
    "ApplicationStopped = {ApplicationStopped.IsCancellationRequested}")]
#pragma warning disable CS0618 // Type or member is obsolete
internal sealed class ApplicationLifetime : IApplicationLifetime, Extensions.Hosting.IApplicationLifetime, IHostApplicationLifetime
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly CancellationTokenSource _startedSource = new CancellationTokenSource();
    private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();
    private readonly CancellationTokenSource _stoppedSource = new CancellationTokenSource();
    private readonly ILogger<ApplicationLifetime> _logger;

    public ApplicationLifetime(ILogger<ApplicationLifetime> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Triggered when the application host has fully started and is about to wait
    /// for a graceful shutdown.
    /// </summary>
    public CancellationToken ApplicationStarted => _startedSource.Token;

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// Request may still be in flight. Shutdown will block until this event completes.
    /// </summary>
    public CancellationToken ApplicationStopping => _stoppingSource.Token;

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// All requests should be complete at this point. Shutdown will block
    /// until this event completes.
    /// </summary>
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    /// <summary>
    /// Signals the ApplicationStopping event and blocks until it completes.
    /// </summary>
    public void StopApplication()
    {
        // Lock on CTS to synchronize multiple calls to StopApplication. This guarantees that the first call
        // to StopApplication and its callbacks run to completion before subsequent calls to StopApplication,
        // which will no-op since the first call already requested cancellation, get a chance to execute.
        lock (_stoppingSource)
        {
            try
            {
                _stoppingSource.Cancel();
            }
            catch (Exception ex)
            {
                _logger.ApplicationError(LoggerEventIds.ApplicationStoppingException,
                                         "An error occurred stopping the application",
                                         ex);
            }
        }
    }

    /// <summary>
    /// Signals the ApplicationStarted event and blocks until it completes.
    /// </summary>
    public void NotifyStarted()
    {
        try
        {
            _startedSource.Cancel();
        }
        catch (Exception ex)
        {
            _logger.ApplicationError(LoggerEventIds.ApplicationStartupException,
                                     "An error occurred starting the application",
                                     ex);
        }
    }

    /// <summary>
    /// Signals the ApplicationStopped event and blocks until it completes.
    /// </summary>
    public void NotifyStopped()
    {
        try
        {
            _stoppedSource.Cancel();
        }
        catch (Exception ex)
        {
            _logger.ApplicationError(LoggerEventIds.ApplicationStoppedException,
                                     "An error occurred stopping the application",
                                     ex);
        }
    }
}
