// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace BlazorWasm.ServiceDefaults1.Telemetry.Serializer;

internal static class OtlpTraceSerializer
{
    private const int ReserveSizeForLength = 4;
    private const int TraceIdSize = 16;
    private const int SpanIdSize = 8;

    internal static byte[] SerializeTraceData(List<Activity> activities, string serviceName)
    {
        // Initial buffer size - will grow if needed
        var buffer = new byte[4096];
        var writePosition = 0;

        // Group activities by source
        var grouped = activities.GroupBy(a => a.Source.Name).ToList();

        // Write TracesData.resource_spans (field 1)
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpTraceFieldNumbers.TracesData_Resource_Spans, ProtobufWireType.LEN);
        var resourceSpansLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Write ResourceSpans
        writePosition = WriteResourceSpans(ref buffer, writePosition, serviceName, grouped);

        ProtobufSerializer.WriteReservedLength(buffer, resourceSpansLengthPosition, writePosition - (resourceSpansLengthPosition + ReserveSizeForLength));

        // Return just the used portion of the buffer
        var result = new byte[writePosition];
        Array.Copy(buffer, result, writePosition);
        return result;
    }

    private static int WriteResourceSpans(ref byte[] buffer, int writePosition, string serviceName, List<IGrouping<string, Activity>> grouped)
    {
        // Write Resource
        writePosition = WriteResource(ref buffer, writePosition, serviceName);

        // Write ScopeSpans for each activity source
        foreach (var group in grouped)
        {
            writePosition = EnsureBufferSize(ref buffer, writePosition, 1024);
            writePosition = WriteScopeSpans(ref buffer, writePosition, group.Key, group.First().Source.Version, group.ToList());
        }

        return writePosition;
    }

    private static int WriteResource(ref byte[] buffer, int writePosition, string serviceName)
    {
        writePosition = EnsureBufferSize(ref buffer, writePosition, 256);

        // ResourceSpans.resource (field 1)
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpTraceFieldNumbers.ResourceSpans_Resource, ProtobufWireType.LEN);
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
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpTraceFieldNumbers.Resource_Attributes, ProtobufWireType.LEN);
        var keyValueLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // KeyValue.key (field 1)
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.KeyValue_Key, key);

        // KeyValue.value (field 2) - AnyValue message
        var valueUtf8Length = ProtobufSerializer.GetNumberOfUtf8CharsInString(value.AsSpan());
        var valueLengthSize = ProtobufSerializer.ComputeVarInt64Size((ulong)valueUtf8Length);
        // Total AnyValue length = string tag (1) + length varint + string bytes
        var anyValueLength = 1 + valueLengthSize + valueUtf8Length;

        writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, anyValueLength, OtlpTraceFieldNumbers.KeyValue_Value, ProtobufWireType.LEN);
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.AnyValue_String_Value, valueUtf8Length, value.AsSpan());

        ProtobufSerializer.WriteReservedLength(buffer, keyValueLengthPosition, writePosition - (keyValueLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteScopeSpans(ref byte[] buffer, int writePosition, string sourceName, string? sourceVersion, List<Activity> activities)
    {
        writePosition = EnsureBufferSize(ref buffer, writePosition, 512);

        // ResourceSpans.scope_spans (field 2)
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpTraceFieldNumbers.ResourceSpans_Scope_Spans, ProtobufWireType.LEN);
        var scopeSpansLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // ScopeSpans.scope (field 1) - InstrumentationScope
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpTraceFieldNumbers.ScopeSpans_Scope, ProtobufWireType.LEN);
        var scopeLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // InstrumentationScope.name (field 1)
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.InstrumentationScope_Name, sourceName);

        // InstrumentationScope.version (field 2)
        if (!string.IsNullOrEmpty(sourceVersion))
        {
            writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.InstrumentationScope_Version, sourceVersion);
        }

        ProtobufSerializer.WriteReservedLength(buffer, scopeLengthPosition, writePosition - (scopeLengthPosition + ReserveSizeForLength));

        // ScopeSpans.spans (field 2) - repeated Span
        foreach (var activity in activities)
        {
            writePosition = WriteSpan(ref buffer, writePosition, activity);
        }

        ProtobufSerializer.WriteReservedLength(buffer, scopeSpansLengthPosition, writePosition - (scopeSpansLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteSpan(ref byte[] buffer, int writePosition, Activity activity)
    {
        writePosition = EnsureBufferSize(ref buffer, writePosition, 1024);

        // ScopeSpans.spans (field 2)
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpTraceFieldNumbers.ScopeSpans_Span, ProtobufWireType.LEN);
        var spanLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Span.trace_id (field 1) - 16 bytes
        writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, TraceIdSize, OtlpTraceFieldNumbers.Span_Trace_Id, ProtobufWireType.LEN);
        writePosition = WriteTraceId(buffer, writePosition, activity.TraceId);

        // Span.span_id (field 2) - 8 bytes
        writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, SpanIdSize, OtlpTraceFieldNumbers.Span_Span_Id, ProtobufWireType.LEN);
        writePosition = WriteSpanId(buffer, writePosition, activity.SpanId);

        // Span.trace_state (field 3)
        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.Span_Trace_State, activity.TraceStateString);
        }

        // Span.parent_span_id (field 4)
        if (activity.ParentSpanId != default)
        {
            writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, SpanIdSize, OtlpTraceFieldNumbers.Span_Parent_Span_Id, ProtobufWireType.LEN);
            writePosition = WriteSpanId(buffer, writePosition, activity.ParentSpanId);
        }

        // Span.flags (field 16) - write before name for correct order
        writePosition = WriteSpanFlags(buffer, writePosition, activity.ActivityTraceFlags, activity.HasRemoteParent);

        // Span.name (field 5)
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.Span_Name, activity.DisplayName);

        // Span.kind (field 6) - SpanKind enum (+1 because OTLP uses 1-based indexing)
        var spanKind = (int)activity.Kind + 1;
        writePosition = ProtobufSerializer.WriteEnumWithTag(buffer, writePosition, OtlpTraceFieldNumbers.Span_Kind, spanKind);

        // Span.start_time_unix_nano (field 7)
        var startTimeNanos = ToUnixNanoseconds(activity.StartTimeUtc);
        writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, OtlpTraceFieldNumbers.Span_Start_Time_Unix_Nano, (ulong)startTimeNanos);

        // Span.end_time_unix_nano (field 8)
        var endTimeNanos = ToUnixNanoseconds(activity.StartTimeUtc + activity.Duration);
        writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, OtlpTraceFieldNumbers.Span_End_Time_Unix_Nano, (ulong)endTimeNanos);

        // Span.attributes (field 9)
        writePosition = WriteSpanAttributes(ref buffer, writePosition, activity);

        // Span.events (field 11)
        writePosition = WriteSpanEvents(ref buffer, writePosition, activity);

        // Span.links (field 13)
        writePosition = WriteSpanLinks(ref buffer, writePosition, activity);

        // Span.status (field 15)
        writePosition = WriteSpanStatus(ref buffer, writePosition, activity);

        ProtobufSerializer.WriteReservedLength(buffer, spanLengthPosition, writePosition - (spanLengthPosition + ReserveSizeForLength));
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

    private static int WriteSpanFlags(byte[] buffer, int position, ActivityTraceFlags traceFlags, bool hasRemoteParent)
    {
        uint spanFlags = (uint)traceFlags & 0x000000FF;
        spanFlags |= 0x00000100; // has_remote_parent is known
        if (hasRemoteParent)
        {
            spanFlags |= 0x00000200;
        }

        position = ProtobufSerializer.WriteFixed32WithTag(buffer, position, OtlpTraceFieldNumbers.Span_Flags, spanFlags);
        return position;
    }

    private static int WriteSpanAttributes(ref byte[] buffer, int writePosition, Activity activity)
    {
        foreach (var tag in activity.Tags)
        {
            if (tag.Value == null)
            {
                continue;
            }

            // Skip otel status tags - they're handled in WriteSpanStatus
            if (tag.Key == "otel.status_code" || tag.Key == "otel.status_description")
            {
                continue;
            }

            writePosition = EnsureBufferSize(ref buffer, writePosition, 256);
            writePosition = WriteAttribute(ref buffer, writePosition, OtlpTraceFieldNumbers.Span_Attributes, tag.Key, tag.Value);
        }

        return writePosition;
    }

    private static int WriteAttribute(ref byte[] buffer, int writePosition, int parentFieldNumber, string key, string value)
    {
        // Span.attributes (field 9) - KeyValue message
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, parentFieldNumber, ProtobufWireType.LEN);
        var keyValueLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // KeyValue.key (field 1)
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.KeyValue_Key, key);

        // KeyValue.value (field 2) - AnyValue message
        var valueUtf8Length = ProtobufSerializer.GetNumberOfUtf8CharsInString(value.AsSpan());
        var valueLengthSize = ProtobufSerializer.ComputeVarInt64Size((ulong)valueUtf8Length);
        var anyValueLength = 1 + valueLengthSize + valueUtf8Length;

        writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, anyValueLength, OtlpTraceFieldNumbers.KeyValue_Value, ProtobufWireType.LEN);
        writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.AnyValue_String_Value, valueUtf8Length, value.AsSpan());

        ProtobufSerializer.WriteReservedLength(buffer, keyValueLengthPosition, writePosition - (keyValueLengthPosition + ReserveSizeForLength));
        return writePosition;
    }

    private static int WriteSpanEvents(ref byte[] buffer, int writePosition, Activity activity)
    {
        foreach (var evt in activity.Events)
        {
            writePosition = EnsureBufferSize(ref buffer, writePosition, 512);

            // Span.events (field 11)
            writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpTraceFieldNumbers.Span_Events, ProtobufWireType.LEN);
            var eventLengthPosition = writePosition;
            writePosition += ReserveSizeForLength;

            // Event.time_unix_nano (field 1)
            var eventTimeNanos = ToUnixNanoseconds(evt.Timestamp.UtcDateTime);
            writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, OtlpTraceFieldNumbers.Event_Time_Unix_Nano, (ulong)eventTimeNanos);

            // Event.name (field 2)
            writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.Event_Name, evt.Name);

            // Event.attributes (field 3)
            foreach (var tag in evt.Tags)
            {
                if (tag.Value != null)
                {
                    writePosition = WriteAttribute(ref buffer, writePosition, OtlpTraceFieldNumbers.Event_Attributes, tag.Key, tag.Value.ToString()!);
                }
            }

            ProtobufSerializer.WriteReservedLength(buffer, eventLengthPosition, writePosition - (eventLengthPosition + ReserveSizeForLength));
        }

        return writePosition;
    }

    private static int WriteSpanLinks(ref byte[] buffer, int writePosition, Activity activity)
    {
        foreach (var link in activity.Links)
        {
            writePosition = EnsureBufferSize(ref buffer, writePosition, 256);

            // Span.links (field 13)
            writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpTraceFieldNumbers.Span_Links, ProtobufWireType.LEN);
            var linkLengthPosition = writePosition;
            writePosition += ReserveSizeForLength;

            // Link.trace_id (field 1)
            writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, TraceIdSize, OtlpTraceFieldNumbers.Link_Trace_Id, ProtobufWireType.LEN);
            writePosition = WriteTraceId(buffer, writePosition, link.Context.TraceId);

            // Link.span_id (field 2)
            writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, SpanIdSize, OtlpTraceFieldNumbers.Link_Span_Id, ProtobufWireType.LEN);
            writePosition = WriteSpanId(buffer, writePosition, link.Context.SpanId);

            // Link.trace_state (field 3)
            if (!string.IsNullOrEmpty(link.Context.TraceState))
            {
                writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.Link_Trace_State, link.Context.TraceState);
            }

            // Link.attributes (field 4)
            if (link.Tags != null)
            {
                foreach (var tag in link.Tags)
                {
                    if (tag.Value != null)
                    {
                        writePosition = WriteAttribute(ref buffer, writePosition, OtlpTraceFieldNumbers.Link_Attributes, tag.Key, tag.Value.ToString()!);
                    }
                }
            }

            // Link.flags (field 6)
            writePosition = WriteSpanFlags(buffer, writePosition, link.Context.TraceFlags, link.Context.IsRemote);

            ProtobufSerializer.WriteReservedLength(buffer, linkLengthPosition, writePosition - (linkLengthPosition + ReserveSizeForLength));
        }

        return writePosition;
    }

    private static int WriteSpanStatus(ref byte[] buffer, int writePosition, Activity activity)
    {
        var hasStatus = activity.Status != ActivityStatusCode.Unset;
        var hasDescription = !string.IsNullOrEmpty(activity.StatusDescription);

        if (!hasStatus && !hasDescription)
        {
            return writePosition;
        }

        writePosition = EnsureBufferSize(ref buffer, writePosition, 128);

        // Span.status (field 15)
        writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, OtlpTraceFieldNumbers.Span_Status, ProtobufWireType.LEN);
        var statusLengthPosition = writePosition;
        writePosition += ReserveSizeForLength;

        // Status.message (field 2)
        if (hasDescription)
        {
            writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, OtlpTraceFieldNumbers.Status_Message, activity.StatusDescription!);
        }

        // Status.code (field 3)
        var statusCode = activity.Status switch
        {
            ActivityStatusCode.Ok => OtlpTraceFieldNumbers.StatusCode_Ok,
            ActivityStatusCode.Error => OtlpTraceFieldNumbers.StatusCode_Error,
            _ => OtlpTraceFieldNumbers.StatusCode_Unset
        };
        writePosition = ProtobufSerializer.WriteEnumWithTag(buffer, writePosition, OtlpTraceFieldNumbers.Status_Code, statusCode);

        ProtobufSerializer.WriteReservedLength(buffer, statusLengthPosition, writePosition - (statusLengthPosition + ReserveSizeForLength));
        return writePosition;
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
