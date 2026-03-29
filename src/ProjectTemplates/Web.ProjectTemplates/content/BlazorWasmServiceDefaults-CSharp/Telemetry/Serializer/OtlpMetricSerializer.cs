// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorWasm.ServiceDefaults1.Telemetry.Serializer;

internal static class OtlpMetricSerializer
{
    private const int ReserveSizeForLength = 4;

    internal readonly struct CapturedMetric
    {
        public string Name { get; init; }
        public string? Description { get; init; }
        public string? Unit { get; init; }
        public MetricType Type { get; init; }
        public double Value { get; init; }
        public long LongValue { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public DateTimeOffset StartTime { get; init; }
        public IReadOnlyList<KeyValuePair<string, object?>>? Attributes { get; init; }
    }

    internal enum MetricType
    {
        LongGauge,
        DoubleGauge,
        LongSum,
        DoubleSum
    }

    internal static byte[] SerializeMetricData(IReadOnlyList<CapturedMetric> metrics, string serviceName)
    {
        // Pre-allocate buffer (start with 8KB, grow as needed)
        var buffer = new byte[8192];
        var writePosition = 0;

        // Write MetricsData
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpMetricFieldNumbers.MetricsData_Resource_Metrics, ProtobufWireType.LEN);
        var resourceMetricsLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Write ResourceMetrics
        writePosition = WriteResourceMetrics(buffer, writePosition, metrics, serviceName);

        // Write the length
        ProtobufSerializer.WriteReservedLength(buffer, resourceMetricsLengthPosition, writePosition - (resourceMetricsLengthPosition + ReserveSizeForLength));

        // Return the final payload
        var result = new byte[writePosition];
        Buffer.BlockCopy(buffer, 0, result, 0, writePosition);
        return result;
    }

    private static int WriteResourceMetrics(byte[] buffer, int writePosition, IReadOnlyList<CapturedMetric> metrics, string serviceName)
    {
        // Write Resource
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpMetricFieldNumbers.ResourceMetrics_Resource, ProtobufWireType.LEN);
        var resourceLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        writePosition = WriteResource(buffer, writePosition, serviceName);

        ProtobufSerializer.WriteReservedLength(buffer, resourceLengthPosition, writePosition - (resourceLengthPosition + ReserveSizeForLength));

        // Write ScopeMetrics
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpMetricFieldNumbers.ResourceMetrics_Scope_Metrics, ProtobufWireType.LEN);
        var scopeMetricsLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        writePosition = WriteScopeMetrics(buffer, writePosition, metrics, serviceName);

        ProtobufSerializer.WriteReservedLength(buffer, scopeMetricsLengthPosition, writePosition - (scopeMetricsLengthPosition + ReserveSizeForLength));

        return writePosition;
    }

    private static int WriteResource(byte[] buffer, int writePosition, string serviceName)
    {
        // Write service.name attribute
        writePosition = WriteKeyValue(buffer, writePosition, OtlpMetricFieldNumbers.Resource_Attributes, "service.name", serviceName);
        return writePosition;
    }

    private static int WriteScopeMetrics(byte[] buffer, int writePosition, IReadOnlyList<CapturedMetric> metrics, string serviceName)
    {
        // Write InstrumentationScope
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpMetricFieldNumbers.ScopeMetrics_Scope, ProtobufWireType.LEN);
        var scopeLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpMetricFieldNumbers.InstrumentationScope_Name, serviceName);

        ProtobufSerializer.WriteReservedLength(buffer, scopeLengthPosition, writePosition - (scopeLengthPosition + ReserveSizeForLength));

        // Write each metric
        foreach (var metric in metrics)
        {
            writePosition = EnsureCapacity(ref buffer, writePosition, 1024);
            writePosition = WriteMetric(buffer, writePosition, metric);
        }

        return writePosition;
    }

    private static int WriteMetric(byte[] buffer, int writePosition, in CapturedMetric metric)
    {
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpMetricFieldNumbers.ScopeMetrics_Metrics, ProtobufWireType.LEN);
        var metricLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Write metric name
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpMetricFieldNumbers.Metric_Name, metric.Name);

        // Write description if present
        if (!string.IsNullOrEmpty(metric.Description))
        {
            writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpMetricFieldNumbers.Metric_Description, metric.Description);
        }

        // Write unit if present
        if (!string.IsNullOrEmpty(metric.Unit))
        {
            writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpMetricFieldNumbers.Metric_Unit, metric.Unit);
        }

        // Write data based on type
        switch (metric.Type)
        {
            case MetricType.LongGauge:
            case MetricType.DoubleGauge:
                writePosition = WriteGauge(buffer, writePosition, metric);
                break;
            case MetricType.LongSum:
            case MetricType.DoubleSum:
                writePosition = WriteSum(buffer, writePosition, metric);
                break;
        }

        ProtobufSerializer.WriteReservedLength(buffer, metricLengthPosition, writePosition - (metricLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteGauge(byte[] buffer, int writePosition, in CapturedMetric metric)
    {
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpMetricFieldNumbers.Metric_Data_Gauge, ProtobufWireType.LEN);
        var gaugeLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Write data point
        writePosition = WriteNumberDataPoint(buffer, writePosition, OtlpMetricFieldNumbers.Gauge_Data_Points, metric);

        ProtobufSerializer.WriteReservedLength(buffer, gaugeLengthPosition, writePosition - (gaugeLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteSum(byte[] buffer, int writePosition, in CapturedMetric metric)
    {
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpMetricFieldNumbers.Metric_Data_Sum, ProtobufWireType.LEN);
        var sumLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Write aggregation temporality (cumulative)
        writePosition = ProtobufSerializer.WriteEnumWithTag(buffer, writePosition, OtlpMetricFieldNumbers.Sum_Aggregation_Temporality, OtlpMetricFieldNumbers.Aggregation_Temporality_Cumulative);

        // Write is_monotonic (true for counters)
        writePosition = ProtobufSerializer.WriteBoolWithTag(buffer, writePosition, OtlpMetricFieldNumbers.Sum_Is_Monotonic, true);

        // Write data point
        writePosition = WriteNumberDataPoint(buffer, writePosition, OtlpMetricFieldNumbers.Sum_Data_Points, metric);

        ProtobufSerializer.WriteReservedLength(buffer, sumLengthPosition, writePosition - (sumLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteNumberDataPoint(byte[] buffer, int writePosition, int fieldNumber, in CapturedMetric metric)
    {
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.LEN);
        var dataPointLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Write start time
        var startTimeNano = (ulong)metric.StartTime.ToUnixTimeMilliseconds() * 1_000_000;
        writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, OtlpMetricFieldNumbers.NumberDataPoint_Start_Time_Unix_Nano, startTimeNano);

        // Write time
        var timeNano = (ulong)metric.Timestamp.ToUnixTimeMilliseconds() * 1_000_000;
        writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, OtlpMetricFieldNumbers.NumberDataPoint_Time_Unix_Nano, timeNano);

        // Write value
        if (metric.Type is MetricType.LongGauge or MetricType.LongSum)
        {
            writePosition = ProtobufSerializer.WriteInt64WithTag(buffer, writePosition, OtlpMetricFieldNumbers.NumberDataPoint_Value_As_Int, (ulong)metric.LongValue);
        }
        else
        {
            writePosition = ProtobufSerializer.WriteDoubleWithTag(buffer, writePosition, OtlpMetricFieldNumbers.NumberDataPoint_Value_As_Double, metric.Value);
        }

        // Write attributes if present
        if (metric.Attributes is not null)
        {
            foreach (var attr in metric.Attributes)
            {
                if (attr.Value is not null)
                {
                    writePosition = EnsureCapacity(ref buffer, writePosition, 256);
                    writePosition = WriteKeyValue(buffer, writePosition, OtlpMetricFieldNumbers.NumberDataPoint_Attributes, attr.Key, attr.Value);
                }
            }
        }

        ProtobufSerializer.WriteReservedLength(buffer, dataPointLengthPosition, writePosition - (dataPointLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteKeyValue(byte[] buffer, int writePosition, int fieldNumber, string key, object value)
    {
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, fieldNumber, ProtobufWireType.LEN);
        var kvLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Write key
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpMetricFieldNumbers.KeyValue_Key, key);

        // Write value
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpMetricFieldNumbers.KeyValue_Value, ProtobufWireType.LEN);
        var valueLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        writePosition = WriteAnyValue(buffer, writePosition, value);

        ProtobufSerializer.WriteReservedLength(buffer, valueLengthPosition, writePosition - (valueLengthPosition + ReserveSizeForLength));
        ProtobufSerializer.WriteReservedLength(buffer, kvLengthPosition, writePosition - (kvLengthPosition + ReserveSizeForLength));

        return writePosition;
    }

    private static int WriteAnyValue(byte[] buffer, int writePosition, object value)
    {
        return value switch
        {
            string s => ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpMetricFieldNumbers.AnyValue_String_Value, s),
            bool b => ProtobufSerializer.WriteBoolWithTag(buffer, writePosition, OtlpMetricFieldNumbers.AnyValue_Bool_Value, b),
            int i => ProtobufSerializer.WriteInt64WithTag(buffer, writePosition, OtlpMetricFieldNumbers.AnyValue_Int_Value, (ulong)i),
            long l => ProtobufSerializer.WriteInt64WithTag(buffer, writePosition, OtlpMetricFieldNumbers.AnyValue_Int_Value, (ulong)l),
            double d => ProtobufSerializer.WriteDoubleWithTag(buffer, writePosition, OtlpMetricFieldNumbers.AnyValue_Double_Value, d),
            float f => ProtobufSerializer.WriteDoubleWithTag(buffer, writePosition, OtlpMetricFieldNumbers.AnyValue_Double_Value, f),
            _ => ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpMetricFieldNumbers.AnyValue_String_Value, value.ToString() ?? string.Empty)
        };
    }

    private static int EnsureCapacity(ref byte[] buffer, int writePosition, int additionalNeeded)
    {
        if (writePosition + additionalNeeded <= buffer.Length)
        {
            return writePosition;
        }

        var newBuffer = new byte[buffer.Length * 2];
        Buffer.BlockCopy(buffer, 0, newBuffer, 0, writePosition);
        buffer = newBuffer;
        return writePosition;
    }
}
