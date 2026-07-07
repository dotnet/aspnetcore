// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms;

internal enum ValidationOutcome
{
    Valid,
    Invalid,
    ThrowOperationCanceled,
    ThrowInfraException,
    ThrowSynchronously,
    HangForever,
}

internal sealed record ValidationConfig
{
    public ValidationOutcome Outcome { get; init; } = ValidationOutcome.Valid;

    public string ErrorMessage { get; init; } = "Test error";

    public TimeSpan Latency { get; init; }

    public bool ObserveCancellation { get; init; } = true;

    public Func<FieldIdentifier, CancellationToken, Task<string>> Custom { get; init; }
}

internal sealed class TestAsyncValidator : IDisposable
{
    private readonly EditContext _editContext;
    private readonly ValidationMessageStore _store;
    private readonly Dictionary<FieldIdentifier, ValidationConfig> _configs = new();
    private readonly Dictionary<FieldIdentifier, TaskCompletionSource> _gates = new();
    private readonly Dictionary<FieldIdentifier, Counter> _counters = new();
    private readonly Dictionary<FieldIdentifier, CancellationToken> _lastTokens = new();
    private readonly List<FieldIdentifier> _validationOrder = new();
    private bool _disposed;

    public TestAsyncValidator(EditContext editContext)
    {
        _editContext = editContext;
        _store = new ValidationMessageStore(editContext);
        _editContext.OnFieldChanged += OnFieldChanged;
        _editContext.OnValidationRequested += OnValidationRequested;
        _editContext.OnValidationRequestedAsync += OnValidationRequestedAsync;
    }

    public ValidationOutcome DefaultOutcome { get; set; } = ValidationOutcome.Valid;

    public TimeSpan DefaultLatency { get; set; }

    public int FormValidationStartCount { get; private set; }

    public IReadOnlyList<FieldIdentifier> ValidationOrder => _validationOrder;

    public void Configure(FieldIdentifier field, ValidationConfig config)
    {
        _configs[field] = config;
        _ = GetCounter(field);
    }

    public void Configure<TField>(Expression<Func<TField>> accessor, ValidationConfig config)
        => Configure(FieldIdentifier.Create(accessor), config);

    public TaskCompletionSource GetGate(FieldIdentifier field)
    {
        if (!_gates.TryGetValue(field, out var gate) || gate.Task.IsCompleted)
        {
            gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _gates[field] = gate;
        }

        return gate;
    }

    public void OpenGate(FieldIdentifier field, ValidationOutcome outcome)
    {
        var current = GetConfig(field);
        Configure(field, current with { Outcome = outcome });
        GetGate(field).TrySetResult();
    }

    public int FieldValidationStartCount(FieldIdentifier field)
        => GetCounter(field).Started;

    public int FieldValidationCompletedCount(FieldIdentifier field)
        => GetCounter(field).Completed;

    public int CancellationObservedCount(FieldIdentifier field)
        => GetCounter(field).Cancelled;

    public CancellationToken? LastTokenFor(FieldIdentifier field)
        => _lastTokens.TryGetValue(field, out var token) ? token : null;

    public void NotifyFieldChanged(FieldIdentifier field)
        => _editContext.NotifyFieldChanged(field);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _editContext.OnFieldChanged -= OnFieldChanged;
        _editContext.OnValidationRequested -= OnValidationRequested;
        _editContext.OnValidationRequestedAsync -= OnValidationRequestedAsync;
    }

    private void OnFieldChanged(object sender, FieldChangedEventArgs args)
    {
        var field = args.FieldIdentifier;
        var config = GetConfig(field);
        _store.Clear(field);
        var cts = new CancellationTokenSource();
        var task = RunValidationAsync(field, config, cts.Token);
        _editContext.AddValidationTask(field, task, cts);
    }

    private void OnValidationRequested(object sender, ValidationRequestedEventArgs args)
    {
    }

    private async Task OnValidationRequestedAsync(object sender, ValidationRequestedEventArgs args)
    {
        FormValidationStartCount++;
        _store.Clear();

        var tasks = _configs.Keys
            .Select(field => RunValidationAsync(field, GetConfig(field), args.CancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task RunValidationAsync(FieldIdentifier field, ValidationConfig config, CancellationToken cancellationToken)
    {
        var counter = GetCounter(field);
        counter.Started++;
        _lastTokens[field] = cancellationToken;
        _validationOrder.Add(field);

        try
        {
            if (config.Outcome == ValidationOutcome.ThrowSynchronously)
            {
                throw new InvalidOperationException("Sync throw");
            }

            if (config.Custom is not null)
            {
                var message = await config.Custom(field, cancellationToken);
                if (message is not null)
                {
                    _store.Add(field, message);
                }

                return;
            }

            if (_gates.TryGetValue(field, out var gate) && !gate.Task.IsCompleted)
            {
                await gate.Task.WaitAsync(cancellationToken);
            }
            else if (config.Latency > TimeSpan.Zero)
            {
                await Task.Delay(config.Latency, cancellationToken);
            }

            switch (config.Outcome)
            {
                case ValidationOutcome.Valid:
                    break;
                case ValidationOutcome.Invalid:
                    _store.Add(field, config.ErrorMessage);
                    break;
                case ValidationOutcome.ThrowOperationCanceled:
                    throw new OperationCanceledException(cancellationToken);
                case ValidationOutcome.ThrowInfraException:
                    throw new InvalidOperationException("Test infrastructure failure");
                case ValidationOutcome.HangForever:
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                    break;
                case ValidationOutcome.ThrowSynchronously:
                    break;
                default:
                    throw new InvalidOperationException($"Unknown validation outcome '{config.Outcome}'.");
            }
        }
        catch (OperationCanceledException) when (config.ObserveCancellation && cancellationToken.IsCancellationRequested)
        {
            counter.Cancelled++;
            throw;
        }
        finally
        {
            counter.Completed++;
        }
    }

    private ValidationConfig GetConfig(FieldIdentifier field)
        => _configs.TryGetValue(field, out var config)
            ? config
            : new ValidationConfig { Outcome = DefaultOutcome, Latency = DefaultLatency };

    private Counter GetCounter(FieldIdentifier field)
    {
        if (!_counters.TryGetValue(field, out var counter))
        {
            counter = new Counter();
            _counters[field] = counter;
        }

        return counter;
    }

    private sealed class Counter
    {
        public int Started;

        public int Completed;

        public int Cancelled;
    }
}
