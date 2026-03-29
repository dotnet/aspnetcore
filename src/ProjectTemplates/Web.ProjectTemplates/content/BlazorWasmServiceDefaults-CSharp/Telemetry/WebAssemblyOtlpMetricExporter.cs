// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net.Http.Headers;
using BlazorWasm.ServiceDefaults1.Telemetry.Serializer;

namespace BlazorWasm.ServiceDefaults1.Telemetry;

public sealed class WebAssemblyOtlpMetricExporter : IDisposable
{
    private static readonly MediaTypeHeaderValue s_protobufMediaType = new("application/x-protobuf");

    private readonly Uri _endpoint;
    private readonly HttpClient _httpClient;
    private readonly string _serviceName;
    private readonly Dictionary<string, string>? _headers;
    private readonly MeterListener _meterListener;
    private readonly List<OtlpMetricSerializer.CapturedMetric> _capturedMetrics = new();
    private readonly object _lock = new();
    private readonly Timer _exportTimer;
    private readonly DateTimeOffset _startTime;
    private bool _disposed;

    public WebAssemblyOtlpMetricExporter(
        Uri endpoint,
        string serviceName,
        Dictionary<string, string>? headers = null,
        IEnumerable<string>? meterNames = null,
        int exportIntervalMs = 10000,
        HttpClient? httpClient = null)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _headers = headers;
        _startTime = DateTimeOffset.UtcNow;
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var meterNamesSet = meterNames?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Create meter listener
        _meterListener = new MeterListener();

        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            // Filter by meter name if specified
            if (meterNamesSet is null || meterNamesSet.Contains(instrument.Meter.Name))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        // Set up measurement callbacks for different types
        _meterListener.SetMeasurementEventCallback<int>(OnMeasurement);
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurement);
        _meterListener.SetMeasurementEventCallback<float>(OnMeasurement);
        _meterListener.SetMeasurementEventCallback<double>(OnMeasurement);
        _meterListener.SetMeasurementEventCallback<decimal>(OnMeasurement);
        _meterListener.SetMeasurementEventCallback<short>(OnMeasurement);
        _meterListener.SetMeasurementEventCallback<byte>(OnMeasurement);

        _meterListener.Start();

        // Set up periodic export timer
        _exportTimer = new Timer(
            callback: _ => ExportMetrics(),
            state: null,
            dueTime: exportIntervalMs,
            period: exportIntervalMs);
    }

    private void OnMeasurement<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct
    {
        var value = Convert.ToDouble(measurement, CultureInfo.InvariantCulture);
        var longValue = Convert.ToInt64(measurement, CultureInfo.InvariantCulture);

        var metricType = instrument switch
        {
            Counter<T> or ObservableCounter<T> => typeof(T) == typeof(double) || typeof(T) == typeof(float)
                ? OtlpMetricSerializer.MetricType.DoubleSum
                : OtlpMetricSerializer.MetricType.LongSum,
            ObservableGauge<T> or UpDownCounter<T> or ObservableUpDownCounter<T> => typeof(T) == typeof(double) || typeof(T) == typeof(float)
                ? OtlpMetricSerializer.MetricType.DoubleGauge
                : OtlpMetricSerializer.MetricType.LongGauge,
            Histogram<T> => OtlpMetricSerializer.MetricType.DoubleSum, // Simplified: treat histograms as sums for now
            _ => OtlpMetricSerializer.MetricType.DoubleGauge
        };

        var capturedMetric = new OtlpMetricSerializer.CapturedMetric
        {
            Name = instrument.Name,
            Description = instrument.Description,
            Unit = instrument.Unit,
            Type = metricType,
            Value = value,
            LongValue = longValue,
            Timestamp = DateTimeOffset.UtcNow,
            StartTime = _startTime,
            Attributes = tags.Length > 0 ? tags.ToArray() : null
        };

        lock (_lock)
        {
            _capturedMetrics.Add(capturedMetric);
        }
    }

    private void ExportMetrics()
    {
        if (_disposed)
        {
            return;
        }

        List<OtlpMetricSerializer.CapturedMetric> metricsToExport;
        lock (_lock)
        {
            if (_capturedMetrics.Count == 0)
            {
                return;
            }

            metricsToExport = new List<OtlpMetricSerializer.CapturedMetric>(_capturedMetrics);
            _capturedMetrics.Clear();
        }

        try
        {
            var payload = OtlpMetricSerializer.SerializeMetricData(metricsToExport, _serviceName);

            // Fire-and-forget the HTTP call
            SendAsync(payload);
        }
        catch (Exception)
        {
            // Export failed - fire and forget pattern
        }
    }

    private async void SendAsync(byte[] payload)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Content = new ByteArrayContent(payload);
            request.Content.Headers.ContentType = s_protobufMediaType;

            // Add custom headers (e.g., x-otlp-api-key for authentication)
            if (_headers is not null)
            {
                foreach (var header in _headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            await _httpClient.SendAsync(request).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // SendAsync failed - fire and forget pattern
        }
    }

    public void Flush()
    {
        ExportMetrics();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _exportTimer.Dispose();
        _meterListener.Dispose();

        // Final flush
        ExportMetrics();
    }
}
