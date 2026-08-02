// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

// Implementation of ICircuitPersistenceProvider that uses HybridCache for distributed caching
internal sealed partial class HybridCacheCircuitPersistenceProvider : ICircuitPersistenceProvider
{
    private static readonly Func<CancellationToken, ValueTask<PersistedCircuitState>> _failOnCreate =
        static ct => throw new InvalidOperationException();

    private static readonly string[] _tags = ["Microsoft.AspNetCore.Components.Server.PersistedCircuitState"];

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly HybridCache _hybridCache;
    private readonly ILogger<ICircuitPersistenceProvider> _logger;
    private readonly HybridCacheEntryOptions _cacheWriteOptions;
    private readonly HybridCacheEntryOptions _cacheReadOptions;

    public HybridCacheCircuitPersistenceProvider(
        HybridCache hybridCache,
        ILogger<ICircuitPersistenceProvider> logger,
        IOptions<CircuitOptions> options)
    {
        _hybridCache = hybridCache;
        _logger = logger;
        _cacheWriteOptions = new HybridCacheEntryOptions
        {
            Expiration = options.Value.PersistedCircuitDistributedRetentionPeriod,
            LocalCacheExpiration = options.Value.PersistedCircuitInMemoryRetentionPeriod,
        };
        _cacheReadOptions = new HybridCacheEntryOptions
        {
            Flags = HybridCacheEntryFlags.DisableLocalCacheWrite |
                    HybridCacheEntryFlags.DisableDistributedCacheWrite |
                    HybridCacheEntryFlags.DisableUnderlyingData,
        };
    }

    public async Task PersistCircuitAsync(CircuitId circuitId, PersistedCircuitState persistedCircuitState, CancellationToken cancellation = default)
    {
        Log.CircuitPauseStarted(_logger, circuitId);

        try
        {
            await _lock.WaitAsync(cancellation);
            await _hybridCache.SetAsync(circuitId.Secret, persistedCircuitState, _cacheWriteOptions, _tags, cancellation);
        }
        catch (Exception ex)
        {
            Log.ExceptionPersistingCircuit(_logger, circuitId, ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<PersistedCircuitState> RestoreCircuitAsync(CircuitId circuitId, CancellationToken cancellation = default)
    {
        Log.CircuitResumeStarted(_logger, circuitId);

        try
        {
            await _lock.WaitAsync(cancellation);
            var state = await _hybridCache.GetOrCreateAsync(
                circuitId.Secret,
                factory: _failOnCreate,
                options: _cacheReadOptions,
                _tags,
                cancellation);

            if (state == null)
            {
                Log.FailedToFindCircuitState(_logger, circuitId);
                return null;
            }

            await _hybridCache.RemoveAsync(circuitId.Secret, cancellation);

            Log.CircuitStateFound(_logger, circuitId);
            return state;
        }
        catch (Exception ex)
        {
            Log.ExceptionRestoringCircuit(_logger, circuitId, ex);
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static partial class Log
    {
        [LoggerMessage(201, LogLevel.Debug, "Circuit state evicted for circuit {CircuitId} due to {Reason}", EventName = "CircuitStateEvicted")]
        public static partial void CircuitStateEvicted(ILogger logger, CircuitId circuitId, string reason);

        [LoggerMessage(202, LogLevel.Debug, "Resuming circuit with ID {CircuitId}", EventName = "CircuitResumeStarted")]
        public static partial void CircuitResumeStarted(ILogger logger, CircuitId circuitId);

        [LoggerMessage(203, LogLevel.Debug, "Failed to find persisted circuit with ID {CircuitId}", EventName = "FailedToFindCircuitState")]
        public static partial void FailedToFindCircuitState(ILogger logger, CircuitId circuitId);

        [LoggerMessage(204, LogLevel.Debug, "Circuit state found for circuit {CircuitId}", EventName = "CircuitStateFound")]
        public static partial void CircuitStateFound(ILogger logger, CircuitId circuitId);

        [LoggerMessage(205, LogLevel.Error, "An exception occurred while disposing the token source.", EventName = "ExceptionDisposingTokenSource")]
        public static partial void ExceptionDisposingTokenSource(ILogger logger, Exception exception);

        [LoggerMessage(206, LogLevel.Debug, "Pausing circuit with ID {CircuitId}", EventName = "CircuitPauseStarted")]
        public static partial void CircuitPauseStarted(ILogger logger, CircuitId circuitId);

        [LoggerMessage(207, LogLevel.Error, "An exception occurred while persisting circuit {CircuitId}.", EventName = "ExceptionPersistingCircuit")]
        public static partial void ExceptionPersistingCircuit(ILogger logger, CircuitId circuitId, Exception exception);

        [LoggerMessage(208, LogLevel.Error, "An exception occurred while restoring circuit {CircuitId}.", EventName = "ExceptionRestoringCircuit")]
        public static partial void ExceptionRestoringCircuit(ILogger logger, CircuitId circuitId, Exception exception);

        [LoggerMessage(209, LogLevel.Error, "An exception occurred during expiration handling for circuit {CircuitId}.", EventName = "ExceptionDuringExpiration")]
        public static partial void ExceptionDuringExpiration(ILogger logger, CircuitId circuitId, Exception exception);

        [LoggerMessage(210, LogLevel.Error, "An exception occurred while removing expired circuit {CircuitId}.", EventName = "ExceptionRemovingExpiredCircuit")]
        public static partial void ExceptionRemovingExpiredCircuit(ILogger logger, CircuitId circuitId, Exception exception);
    }
}
