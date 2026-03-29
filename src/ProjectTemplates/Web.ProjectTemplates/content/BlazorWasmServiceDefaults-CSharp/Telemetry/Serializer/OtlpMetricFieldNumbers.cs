// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorWasm.ServiceDefaults1.Telemetry.Serializer;

internal static class OtlpMetricFieldNumbers
{
    // MetricsData
    internal const int MetricsData_Resource_Metrics = 1;

    // ResourceMetrics
    internal const int ResourceMetrics_Resource = 1;
    internal const int ResourceMetrics_Scope_Metrics = 2;
    internal const int ResourceMetrics_Schema_Url = 3;

    // Resource
    internal const int Resource_Attributes = 1;

    // ScopeMetrics
    internal const int ScopeMetrics_Scope = 1;
    internal const int ScopeMetrics_Metrics = 2;
    internal const int ScopeMetrics_Schema_Url = 3;

    // InstrumentationScope (from common.proto)
    internal const int InstrumentationScope_Name = 1;
    internal const int InstrumentationScope_Version = 2;
    internal const int InstrumentationScope_Attributes = 3;
    internal const int InstrumentationScope_Dropped_Attributes_Count = 4;

    // Metric
    internal const int Metric_Name = 1;
    internal const int Metric_Description = 2;
    internal const int Metric_Unit = 3;
    internal const int Metric_Data_Gauge = 5;
    internal const int Metric_Data_Sum = 7;
    internal const int Metric_Data_Histogram = 9;
    internal const int Metric_Data_Exponential_Histogram = 10;
    internal const int Metric_Data_Summary = 11;
    internal const int Metric_Metadata = 12;

    // Gauge
    internal const int Gauge_Data_Points = 1;

    // Sum
    internal const int Sum_Data_Points = 1;
    internal const int Sum_Aggregation_Temporality = 2;
    internal const int Sum_Is_Monotonic = 3;

    // Histogram
    internal const int Histogram_Data_Points = 1;
    internal const int Histogram_Aggregation_Temporality = 2;

    // ExponentialHistogram
    internal const int ExponentialHistogram_Data_Points = 1;
    internal const int ExponentialHistogram_Aggregation_Temporality = 2;

    // Summary
    internal const int Summary_Data_Points = 1;

    // NumberDataPoint
    internal const int NumberDataPoint_Attributes = 7;
    internal const int NumberDataPoint_Start_Time_Unix_Nano = 2;
    internal const int NumberDataPoint_Time_Unix_Nano = 3;
    internal const int NumberDataPoint_Value_As_Double = 4;
    internal const int NumberDataPoint_Value_As_Int = 6;
    internal const int NumberDataPoint_Exemplars = 5;
    internal const int NumberDataPoint_Flags = 8;

    // HistogramDataPoint
    internal const int HistogramDataPoint_Attributes = 9;
    internal const int HistogramDataPoint_Start_Time_Unix_Nano = 2;
    internal const int HistogramDataPoint_Time_Unix_Nano = 3;
    internal const int HistogramDataPoint_Count = 4;
    internal const int HistogramDataPoint_Sum = 5;
    internal const int HistogramDataPoint_Bucket_Counts = 6;
    internal const int HistogramDataPoint_Explicit_Bounds = 7;
    internal const int HistogramDataPoint_Exemplars = 8;
    internal const int HistogramDataPoint_Flags = 10;
    internal const int HistogramDataPoint_Min = 11;
    internal const int HistogramDataPoint_Max = 12;

    // ExponentialHistogramDataPoint
    internal const int ExponentialHistogramDataPoint_Attributes = 1;
    internal const int ExponentialHistogramDataPoint_Start_Time_Unix_Nano = 2;
    internal const int ExponentialHistogramDataPoint_Time_Unix_Nano = 3;
    internal const int ExponentialHistogramDataPoint_Count = 4;
    internal const int ExponentialHistogramDataPoint_Sum = 5;
    internal const int ExponentialHistogramDataPoint_Scale = 6;
    internal const int ExponentialHistogramDataPoint_Zero_Count = 7;
    internal const int ExponentialHistogramDataPoint_Positive = 8;
    internal const int ExponentialHistogramDataPoint_Negative = 9;
    internal const int ExponentialHistogramDataPoint_Flags = 10;
    internal const int ExponentialHistogramDataPoint_Exemplars = 11;
    internal const int ExponentialHistogramDataPoint_Min = 12;
    internal const int ExponentialHistogramDataPoint_Max = 13;
    internal const int ExponentialHistogramDataPoint_Zero_Threshold = 14;

    // ExponentialHistogramDataPoint.Buckets
    internal const int ExponentialHistogramDataPoint_Buckets_Offset = 1;
    internal const int ExponentialHistogramDataPoint_Buckets_Bucket_Counts = 2;

    // SummaryDataPoint
    internal const int SummaryDataPoint_Attributes = 7;
    internal const int SummaryDataPoint_Start_Time_Unix_Nano = 2;
    internal const int SummaryDataPoint_Time_Unix_Nano = 3;
    internal const int SummaryDataPoint_Count = 4;
    internal const int SummaryDataPoint_Sum = 5;
    internal const int SummaryDataPoint_Quantile_Values = 6;
    internal const int SummaryDataPoint_Flags = 8;

    // SummaryDataPoint.ValueAtQuantile
    internal const int ValueAtQuantile_Quantile = 1;
    internal const int ValueAtQuantile_Value = 2;

    // Exemplar
    internal const int Exemplar_Filtered_Attributes = 7;
    internal const int Exemplar_Time_Unix_Nano = 2;
    internal const int Exemplar_Value_As_Double = 3;
    internal const int Exemplar_Value_As_Int = 6;
    internal const int Exemplar_Span_Id = 4;
    internal const int Exemplar_Trace_Id = 5;

    // AggregationTemporality (enum values)
    internal const int Aggregation_Temporality_Unspecified = 0;
    internal const int Aggregation_Temporality_Delta = 1;
    internal const int Aggregation_Temporality_Cumulative = 2;

    // DataPointFlags (enum values)
    internal const int DataPointFlags_Do_Not_Use = 0;
    internal const int DataPointFlags_No_Recorded_Value_Mask = 1;

    // KeyValue (from common.proto)
    internal const int KeyValue_Key = 1;
    internal const int KeyValue_Value = 2;

    // AnyValue (from common.proto)
    internal const int AnyValue_String_Value = 1;
    internal const int AnyValue_Bool_Value = 2;
    internal const int AnyValue_Int_Value = 3;
    internal const int AnyValue_Double_Value = 4;
    internal const int AnyValue_Array_Value = 5;
    internal const int AnyValue_Kvlist_Value = 6;
    internal const int AnyValue_Bytes_Value = 7;
}
