// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace BlazorWasm.ServiceDefaults1.Telemetry.Serializer;

internal static class OtlpLogSerializer
{
    private const int ReserveSizeForLength = 4;
    private const int TraceIdSize = 16;
    private const int SpanIdSize = 8;

    internal static byte[] SerializeLogsData(List<LogRecordData> logRecords, string serviceName)
    {
        // Initial buffer size - will grow if needed
        var buffer = new byte[4096];
        var writePosition = 0;

        // Group logs by category/logger name
        var grouped = logRecords.GroupBy(l => l.CategoryName ?? "Default").ToList();

        // Write LogsData.resource_logs (field 1)
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpLogFieldNumbers.LogsData_Resource_Logs, ProtobufWireType.LEN);
        var resourceLogsLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Write ResourceLogs
        writePosition = WriteResourceLogs(ref buffer, writePosition, serviceName, grouped);

        ProtobufSerializer.WriteReservedLength(buffer, resourceLogsLengthPosition, writePosition - (resourceLogsLengthPosition + ReserveSizeForLength));

        // Return just the used portion of the buffer
        var result = new byte[writePosition];
        Array.Copy(buffer, result, writePosition);
        return result;
    }

    private static int WriteResourceLogs(ref byte[] buffer, int writePosition, string serviceName, List<IGrouping<string, LogRecordData>> grouped)
    {
        // Write Resource
        writePosition = WriteResource(ref buffer, writePosition, serviceName);

        // Write ScopeLogs for each category
        foreach (var group in grouped)
        {
            writePosition = EnsureBufferSize(ref buffer, writePosition, 1024);
            writePosition = WriteScopeLogs(ref buffer, writePosition, group.Key, group.ToList());
        }

        return writePosition;
    }

    private static int WriteResource(ref byte[] buffer, int writePosition, string serviceName)
    {
        writePosition = EnsureBufferSize(ref buffer, writePosition, 256);

        // ResourceLogs.resource (field 1)
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpLogFieldNumbers.ResourceLogs_Resource, ProtobufWireType.LEN);
        var resourceLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Write resource attributes
        writePosition = WriteResourceAttribute(ref buffer, writePosition, "service.name", serviceName);
        writePosition = WriteResourceAttribute(ref buffer, writePosition, "telemetry.sdk.name", "opentelemetry");
        writePosition = WriteResourceAttribute(ref buffer, writePosition, "telemetry.sdk.language", "dotnet");
        writePosition = WriteResourceAttribute(ref buffer, writePosition, "telemetry.sdk.version", "1.0.0");

        ProtobufSerializer.WriteReservedLength(buffer, resourceLengthPosition, writePosition - (resourceLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteResourceAttribute(ref byte[] buffer, int writePosition, string key, string value)
    {
        writePosition = EnsureBufferSize(ref buffer, writePosition, 128);

        // Resource.attributes (field 1) - KeyValue message
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpLogFieldNumbers.Resource_Attributes, ProtobufWireType.LEN);
        var keyValueLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // KeyValue.key (field 1)
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpLogFieldNumbers.KeyValue_Key, key);

        // KeyValue.value (field 2) - AnyValue message
        var valueUtf8Length = ProtobufSerializer.GetNumberOfUtf8CharsInString(value.AsSpan());
        var valueLengthSize = ProtobufSerializer.ComputeVarInt64Size((ulong)valueUtf8Length);
        var anyValueLength = 1 + valueLengthSize + valueUtf8Length;

        writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, anyValueLength, OtlpLogFieldNumbers.KeyValue_Value, ProtobufWireType.LEN);
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpLogFieldNumbers.AnyValue_String_Value, valueUtf8Length, value.AsSpan());

        ProtobufSerializer.WriteReservedLength(buffer, keyValueLengthPosition, writePosition - (keyValueLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteScopeLogs(ref byte[] buffer, int writePosition, string categoryName, List<LogRecordData> logRecords)
    {
        writePosition = EnsureBufferSize(ref buffer, writePosition, 512);

        // ResourceLogs.scope_logs (field 2)
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpLogFieldNumbers.ResourceLogs_Scope_Logs, ProtobufWireType.LEN);
        var scopeLogsLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // ScopeLogs.scope (field 1) - InstrumentationScope
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpLogFieldNumbers.ScopeLogs_Scope, ProtobufWireType.LEN);
        var scopeLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // InstrumentationScope.name (field 1)
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpLogFieldNumbers.InstrumentationScope_Name, categoryName);

        ProtobufSerializer.WriteReservedLength(buffer, scopeLengthPosition, writePosition - (scopeLengthPosition + ReserveSizeForLength));

        // ScopeLogs.log_records (field 2) - repeated LogRecord
        foreach (var logRecord in logRecords)
        {
            writePosition = WriteLogRecord(ref buffer, writePosition, logRecord);
        }

        ProtobufSerializer.WriteReservedLength(buffer, scopeLogsLengthPosition, writePosition - (scopeLogsLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteLogRecord(ref byte[] buffer, int writePosition, LogRecordData logRecord)
    {
        writePosition = EnsureBufferSize(ref buffer, writePosition, 1024);

        // ScopeLogs.log_records (field 2)
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpLogFieldNumbers.ScopeLogs_Log_Records, ProtobufWireType.LEN);
        var logRecordLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // LogRecord.time_unix_nano (field 1)
        var timestamp = (ulong)ToUnixNanoseconds(logRecord.Timestamp.UtcDateTime);
        writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, OtlpLogFieldNumbers.LogRecord_Time_Unix_Nano, timestamp);

        // LogRecord.observed_time_unix_nano (field 11)
        writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, OtlpLogFieldNumbers.LogRecord_Observed_Time_Unix_Nano, timestamp);

        // LogRecord.severity_number (field 2)
        var severityNumber = LogLevelToSeverityNumber(logRecord.LogLevel);
        writePosition = ProtobufSerializer.WriteEnumWithTag(buffer, writePosition, OtlpLogFieldNumbers.LogRecord_Severity_Number, severityNumber);

        // LogRecord.severity_text (field 3)
        var severityText = LogLevelToSeverityText(logRecord.LogLevel);
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpLogFieldNumbers.LogRecord_Severity_Text, severityText);

        // LogRecord.body (field 5) - the formatted message
        if (!string.IsNullOrEmpty(logRecord.FormattedMessage))
        {
            writePosition = WriteLogRecordBody(ref buffer, writePosition, logRecord.FormattedMessage);
        }

        // LogRecord.attributes (field 6) - log attributes
        if (logRecord.Attributes != null)
        {
            foreach (var attr in logRecord.Attributes)
            {
                if (attr.Key != "{OriginalFormat}" && attr.Value != null)
                {
                    writePosition = EnsureBufferSize(ref buffer, writePosition, 256);
                    writePosition = WriteLogAttribute(ref buffer, writePosition, attr.Key, attr.Value.ToString()!);
                }
            }
        }

        // Add EventId as attribute if present
        if (logRecord.EventId.Id != 0)
        {
            writePosition = EnsureBufferSize(ref buffer, writePosition, 128);
            writePosition = WriteLogAttributeInt(ref buffer, writePosition, "event.id", logRecord.EventId.Id);
        }

        if (!string.IsNullOrEmpty(logRecord.EventId.Name))
        {
            writePosition = EnsureBufferSize(ref buffer, writePosition, 128);
            writePosition = WriteLogAttribute(ref buffer, writePosition, "event.name", logRecord.EventId.Name);
        }

        // Add exception info as attributes
        if (logRecord.Exception != null)
        {
            writePosition = EnsureBufferSize(ref buffer, writePosition, 512);
            writePosition = WriteLogAttribute(ref buffer, writePosition, "exception.type", logRecord.Exception.GetType().FullName ?? logRecord.Exception.GetType().Name);
            writePosition = WriteLogAttribute(ref buffer, writePosition, "exception.message", logRecord.Exception.Message);
            if (!string.IsNullOrEmpty(logRecord.Exception.StackTrace))
            {
                writePosition = WriteLogAttribute(ref buffer, writePosition, "exception.stacktrace", logRecord.Exception.StackTrace);
            }
        }

        // LogRecord.trace_id (field 9) and LogRecord.span_id (field 10) - if in trace context
        if (logRecord.TraceId != default && logRecord.SpanId != default)
        {
            // Trace ID
            writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, TraceIdSize, OtlpLogFieldNumbers.LogRecord_Trace_Id, ProtobufWireType.LEN);
            writePosition = WriteTraceId(buffer, writePosition, logRecord.TraceId);

            // Span ID
            writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, SpanIdSize, OtlpLogFieldNumbers.LogRecord_Span_Id, ProtobufWireType.LEN);
            writePosition = WriteSpanId(buffer, writePosition, logRecord.SpanId);

            // LogRecord.flags (field 8)
            writePosition = ProtobufSerializer.WriteFixed32WithTag(buffer, writePosition, OtlpLogFieldNumbers.LogRecord_Flags, (uint)logRecord.TraceFlags);
        }

        ProtobufSerializer.WriteReservedLength(buffer, logRecordLengthPosition, writePosition - (logRecordLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteLogRecordBody(ref byte[] buffer, int writePosition, string body)
    {
        writePosition = EnsureBufferSize(ref buffer, writePosition, body.Length + 64);

        // Calculate the AnyValue message size
        var bodyUtf8Length = ProtobufSerializer.GetNumberOfUtf8CharsInString(body.AsSpan());
        var bodyLengthSize = ProtobufSerializer.ComputeVarInt64Size((ulong)bodyUtf8Length);
        // AnyValue length = string tag (1) + length varint + string bytes
        var anyValueLength = 1 + bodyLengthSize + bodyUtf8Length;

        writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, anyValueLength, OtlpLogFieldNumbers.LogRecord_Body, ProtobufWireType.LEN);
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpLogFieldNumbers.AnyValue_String_Value, bodyUtf8Length, body.AsSpan());

        return writePosition;
    }

    private static int WriteLogAttribute(ref byte[] buffer, int writePosition, string key, string value)
    {
        // LogRecord.attributes (field 6) - KeyValue message
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpLogFieldNumbers.LogRecord_Attributes, ProtobufWireType.LEN);
        var keyValueLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // KeyValue.key (field 1)
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpLogFieldNumbers.KeyValue_Key, key);

        // KeyValue.value (field 2) - AnyValue message
        var valueUtf8Length = ProtobufSerializer.GetNumberOfUtf8CharsInString(value.AsSpan());
        var valueLengthSize = ProtobufSerializer.ComputeVarInt64Size((ulong)valueUtf8Length);
        var anyValueLength = 1 + valueLengthSize + valueUtf8Length;

        writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, anyValueLength, OtlpLogFieldNumbers.KeyValue_Value, ProtobufWireType.LEN);
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpLogFieldNumbers.AnyValue_String_Value, valueUtf8Length, value.AsSpan());

        ProtobufSerializer.WriteReservedLength(buffer, keyValueLengthPosition, writePosition - (keyValueLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteLogAttributeInt(ref byte[] buffer, int writePosition, string key, int value)
    {
        // LogRecord.attributes (field 6) - KeyValue message
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpLogFieldNumbers.LogRecord_Attributes, ProtobufWireType.LEN);
        var keyValueLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // KeyValue.key (field 1)
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpLogFieldNumbers.KeyValue_Key, key);

        // KeyValue.value (field 2) - AnyValue message with int_value
        // int_value uses VARINT wire type, compute size
        var intVarIntSize = ProtobufSerializer.ComputeVarInt64Size((ulong)value);
        var anyValueLength = 1 + intVarIntSize; // tag (1) + varint

        writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, anyValueLength, OtlpLogFieldNumbers.KeyValue_Value, ProtobufWireType.LEN);
        writePosition = ProtobufSerializer.WriteInt64WithTag(buffer, writePosition, OtlpLogFieldNumbers.AnyValue_Int_Value, (ulong)value);

        ProtobufSerializer.WriteReservedLength(buffer, keyValueLengthPosition, writePosition - (keyValueLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteTraceId(byte[] buffer, int position, ActivityTraceId traceId)
    {
        var traceBytes = new Span<byte>(buffer, position, TraceIdSize);
        traceId.CopyTo(traceBytes);
        return position + TraceIdSize;
    }

    private static int WriteSpanId(byte[] buffer, int position, ActivitySpanId spanId)
    {
        var spanBytes = new Span<byte>(buffer, position, SpanIdSize);
        spanId.CopyTo(spanBytes);
        return position + SpanIdSize;
    }

    private static int LogLevelToSeverityNumber(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => OtlpLogFieldNumbers.Severity_Number_Trace,
            LogLevel.Debug => OtlpLogFieldNumbers.Severity_Number_Debug,
            LogLevel.Information => OtlpLogFieldNumbers.Severity_Number_Info,
            LogLevel.Warning => OtlpLogFieldNumbers.Severity_Number_Warn,
            LogLevel.Error => OtlpLogFieldNumbers.Severity_Number_Error,
            LogLevel.Critical => OtlpLogFieldNumbers.Severity_Number_Fatal,
            _ => OtlpLogFieldNumbers.Severity_Number_Unspecified
        };
    }

    private static string LogLevelToSeverityText(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "FATAL",
            _ => "UNSPECIFIED"
        };
    }

    private static long ToUnixNanoseconds(DateTime utcDateTime)
    {
        // Ticks since Unix epoch (each tick is 100ns)
        var ticksSinceEpoch = utcDateTime.Ticks - DateTime.UnixEpoch.Ticks;
        // Convert to nanoseconds (multiply by 100)
        return ticksSinceEpoch * 100;
    }

    private static int EnsureBufferSize(ref byte[] buffer, int currentPosition, int additionalSpaceNeeded)
    {
        var requiredSize = currentPosition + additionalSpaceNeeded;
        if (buffer.Length >= requiredSize)
        {
            return currentPosition;
        }

        // Double the buffer size or add the required space, whichever is larger
        var newSize = Math.Max(buffer.Length * 2, requiredSize);
        var newBuffer = new byte[newSize];
        Array.Copy(buffer, newBuffer, currentPosition);
        buffer = newBuffer;

        return currentPosition;
    }
}

internal sealed class LogRecordData
{
    public DateTimeOffset Timestamp { get; set; }
    public string? CategoryName { get; set; }
    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public string? FormattedMessage { get; set; }
    public Exception? Exception { get; set; }
    public List<KeyValuePair<string, object?>>? Attributes { get; set; }
    public ActivityTraceId TraceId { get; set; }
    public ActivitySpanId SpanId { get; set; }
    public ActivityTraceFlags TraceFlags { get; set; }
}
