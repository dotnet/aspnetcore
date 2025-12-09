// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

// Default implmentation of ICircuitPersistenceProvider that uses an in-memory cache
internal sealed partial class DefaultInMemoryCircuitPersistenceProvider : ICircuitPersistenceProvider
{
    private readonly Lock _lock = new();
    private readonly CircuitOptions _options;
    private readonly MemoryCache _persistedCircuits;
    private static readonly Task<PersistedCircuitState> _noMatch = Task.FromResult<PersistedCircuitState>(null);
    private readonly ILogger<ICircuitPersistenceProvider> _logger;

    public PostEvictionCallbackRegistration PostEvictionCallback { get; internal set; }

    public DefaultInMemoryCircuitPersistenceProvider(
        ISystemClock clock,
        ILogger<ICircuitPersistenceProvider> logger,
        IOptions<CircuitOptions> options)
    {
        _options = options.Value;
        _persistedCircuits = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = _options.PersistedCircuitInMemoryMaxRetained,
            Clock = clock
        });

        PostEvictionCallback = new PostEvictionCallbackRegistration
        {
            EvictionCallback = OnEntryEvicted
        };

        _logger = logger;
    }

    public Task PersistCircuitAsync(CircuitId circuitId, PersistedCircuitState persistedCircuitState, CancellationToken cancellation = default)
    {
        Log.CircuitPauseStarted(_logger, circuitId);

        lock (_lock)
        {
            PersistCore(circuitId, persistedCircuitState);
        }

        return Task.CompletedTask;
    }

    private void PersistCore(CircuitId circuitId, PersistedCircuitState persistedCircuitState)
    {
        var cancellationTokenSource = new CancellationTokenSource(_options.PersistedCircuitInMemoryRetentionPeriod);
        var options = new MemoryCacheEntryOptions
        {
            Size = 1,
            PostEvictionCallbacks = { PostEvictionCallback },
            ExpirationTokens = { new CancellationChangeToken(cancellationTokenSource.Token) },
        };

        var persistedCircuitEntry = new PersistedCircuitEntry
        {
            State = persistedCircuitState,
            TokenSource = cancellationTokenSource,
            CircuitId = circuitId
        };

        _persistedCircuits.Set(circuitId.Secret, persistedCircuitEntry, options);
    }

    private void OnEntryEvicted(object key, object value, EvictionReason reason, object state)
    {
        switch (reason)
        {
            case EvictionReason.Expired:
            case EvictionReason.TokenExpired:
            // Happens after the circuit state times out, this is triggered by the CancellationTokenSource we register
            // with the entry, which is what controls the expiration
            case EvictionReason.Capacity:
            // Happens when the cache is full
                var persistedCircuitEntry = (PersistedCircuitEntry)value;
                Log.CircuitStateEvicted(_logger, persistedCircuitEntry.CircuitId, reason);
                break;

            case EvictionReason.Removed:
            // Happens when the entry is explicitly removed as part of resuming a circuit.
                return;
            default:
                Debug.Fail($"Unexpected {nameof(EvictionReason)} {reason}");
                break;
        }
    }

    public Task<PersistedCircuitState> RestoreCircuitAsync(CircuitId circuitId, CancellationToken cancellation = default)
    {
        Log.CircuitResumeStarted(_logger, circuitId);

        lock (_lock)
        {
            var state = RestoreCore(circuitId);
            if (state == null)
            {
                Log.FailedToFindCircuitState(_logger, circuitId);
                return _noMatch;
            }

            return Task.FromResult(state);
        }
    }

    private PersistedCircuitState RestoreCore(CircuitId circuitId)
    {
        if (_persistedCircuits.TryGetValue(circuitId.Secret, out var value) && value is PersistedCircuitEntry entry)
        {
            DisposeTokenSource(entry);
            _persistedCircuits.Remove(circuitId.Secret);
            Log.CircuitStateFound(_logger, circuitId);
            return entry.State;
        }

        return null;
    }

    private void DisposeTokenSource(PersistedCircuitEntry entry)
    {
        try
        {
            entry.TokenSource.Dispose();
        }
        catch (Exception ex)
        {
            Log.ExceptionDisposingTokenSource(_logger, ex);
        }
    }

    private class PersistedCircuitEntry
    {
        public PersistedCircuitState State { get; set; }

        public CancellationTokenSource TokenSource { get; set; }

        public CircuitId CircuitId { get; set; }
    }

    private static partial class Log
    {
        [LoggerMessage(101, LogLevel.Debug, "Circuit state evicted for circuit {CircuitId} due to {Reason}", EventName = "CircuitStateEvicted")]
        public static partial void CircuitStateEvicted(ILogger logger, CircuitId circuitId, EvictionReason reason);

        [LoggerMessage(102, LogLevel.Debug, "Resuming circuit with ID {CircuitId}", EventName = "CircuitResumeStarted")]
        public static partial void CircuitResumeStarted(ILogger logger, CircuitId circuitId);

        [LoggerMessage(103, LogLevel.Debug, "Failed to find persisted circuit with ID {CircuitId}", EventName = "FailedToFindCircuitState")]
        public static partial void FailedToFindCircuitState(ILogger logger, CircuitId circuitId);

        [LoggerMessage(104, LogLevel.Debug, "Circuit state found for circuit {CircuitId}", EventName = "CircuitStateFound")]
        public static partial void CircuitStateFound(ILogger logger, CircuitId circuitId);

        [LoggerMessage(105, LogLevel.Error, "An exception occurred while disposing the token source.", EventName = "ExceptionDisposingTokenSource")]
        public static partial void ExceptionDisposingTokenSource(ILogger logger, Exception exception);

        [LoggerMessage(106, LogLevel.Debug, "Pausing circuit with ID {CircuitId}", EventName = "CircuitPauseStarted")]
        public static partial void CircuitPauseStarted(ILogger logger, CircuitId circuitId);
    }
}
