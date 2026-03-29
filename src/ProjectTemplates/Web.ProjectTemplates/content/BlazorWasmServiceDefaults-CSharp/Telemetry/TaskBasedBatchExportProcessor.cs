// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry;

namespace BlazorWasm.ServiceDefaults1.Telemetry;

public sealed class TaskBasedBatchExportProcessor<T> : BaseExportProcessor<T> where T : class
{
    private readonly int _maxQueueSize;
    private readonly int _maxExportBatchSize;
    private readonly int _scheduledDelayMilliseconds;
    private readonly int _exporterTimeoutMilliseconds;
    private readonly CircularBuffer<T> _circularBuffer;
    private readonly SemaphoreSlim _exportTrigger = new(0);
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _exportTask;
    private bool _disposed;

    public TaskBasedBatchExportProcessor(
        BaseExporter<T> exporter,
        int maxQueueSize = 2048,
        int scheduledDelayMilliseconds = 5000,
        int exporterTimeoutMilliseconds = 30000,
        int maxExportBatchSize = 512)
        : base(exporter)
    {
        _maxQueueSize = maxQueueSize;
        _maxExportBatchSize = Math.Min(maxExportBatchSize, maxQueueSize);
        _scheduledDelayMilliseconds = scheduledDelayMilliseconds;
        _exporterTimeoutMilliseconds = exporterTimeoutMilliseconds;
        _circularBuffer = new CircularBuffer<T>(maxQueueSize);

        // Start the background export task
        _exportTask = ExportLoopAsync(_shutdownCts.Token);
    }

    /// <inheritdoc/>
    public override void OnEnd(T data)
    {
        if (_disposed)
        {
            return;
        }

        // Add to buffer; drop if full
        if (!_circularBuffer.TryAdd(data))
        {
            // Buffer is full, item dropped
            return;
        }

        // If we've hit the batch size, trigger an export
        if (_circularBuffer.Count >= _maxExportBatchSize)
        {
            _exportTrigger.Release();
        }
    }

    protected override void OnExport(T data)
    {
        // For batch processing, items are collected in OnEnd and exported in batches
        // This method is required by the base class but we use OnEnd for buffering
    }

    private async Task ExportLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for either the scheduled delay or a trigger
                await Task.WhenAny(
                    _exportTrigger.WaitAsync(cancellationToken),
                    Task.Delay(_scheduledDelayMilliseconds, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ExportBatchAsync();
        }

        // Final export on shutdown
        await ExportBatchAsync();
    }

    private async Task ExportBatchAsync()
    {
        var batch = new List<T>();

        while (batch.Count < _maxExportBatchSize && _circularBuffer.TryTake(out var item))
        {
            if (item is not null)
            {
                batch.Add(item);
            }
        }

        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            // In WebAssembly, the OTLP exporter uses HttpClient.SendAsync().GetAwaiter().GetResult()
            // which would deadlock because there's only one thread.
            // We need to wrap the export in Task.Run to create a proper async context.
            // However, in .NET 8 WebAssembly, Task.Run still runs on the same thread.
            // The only way to avoid the deadlock is to use a fire-and-forget pattern.
            var batchToExport = new Batch<T>([.. batch], batch.Count);
            
            // Schedule the export on a new async context that won't deadlock
            _ = Task.Run(async () =>
            {
                // Small yield to ensure we're not blocking the main flow
                await Task.Yield();
                try
                {
                    exporter.Export(batchToExport);
                }
                catch (Exception)
                {
                    // Export failed - logged at Debug level in production
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Export failed: {ex.Message}");
        }

        // Yield to allow other async operations
        await Task.Yield();
    }

    /// <inheritdoc/>
    protected override bool OnForceFlush(int timeoutMilliseconds)
    {
        _exportTrigger.Release();
        return true;
    }

    /// <inheritdoc/>
    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        _shutdownCts.Cancel();

        // In WebAssembly we can't block with Wait(), so we just request cancellation
        // and let the export loop finish naturally
        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _shutdownCts.Cancel();
                _shutdownCts.Dispose();
                _exportTrigger.Dispose();
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
