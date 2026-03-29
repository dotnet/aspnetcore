// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorWasm.ServiceDefaults1.Telemetry.Serializer;

internal static class OtlpLogFieldNumbers
{
    // LogsData
    internal const int LogsData_Resource_Logs = 1;

    // ResourceLogs
    internal const int ResourceLogs_Resource = 1;
    internal const int ResourceLogs_Scope_Logs = 2;
    internal const int ResourceLogs_Schema_Url = 3;

    // Resource
    internal const int Resource_Attributes = 1;

    // ScopeLogs
    internal const int ScopeLogs_Scope = 1;
    internal const int ScopeLogs_Log_Records = 2;
    internal const int ScopeLogs_Schema_Url = 3;

    // InstrumentationScope (from common.proto)
    internal const int InstrumentationScope_Name = 1;
    internal const int InstrumentationScope_Version = 2;
    internal const int InstrumentationScope_Attributes = 3;
    internal const int InstrumentationScope_Dropped_Attributes_Count = 4;

    // LogRecord
    internal const int LogRecord_Time_Unix_Nano = 1;
    internal const int LogRecord_Observed_Time_Unix_Nano = 11;
    internal const int LogRecord_Severity_Number = 2;
    internal const int LogRecord_Severity_Text = 3;
    internal const int LogRecord_Body = 5;
    internal const int LogRecord_Attributes = 6;
    internal const int LogRecord_Dropped_Attributes_Count = 7;
    internal const int LogRecord_Flags = 8;
    internal const int LogRecord_Trace_Id = 9;
    internal const int LogRecord_Span_Id = 10;
    internal const int LogRecord_Event_Name = 12;

    // SeverityNumber (enum values)
    internal const int Severity_Number_Unspecified = 0;
    internal const int Severity_Number_Trace = 1;
    internal const int Severity_Number_Trace2 = 2;
    internal const int Severity_Number_Trace3 = 3;
    internal const int Severity_Number_Trace4 = 4;
    internal const int Severity_Number_Debug = 5;
    internal const int Severity_Number_Debug2 = 6;
    internal const int Severity_Number_Debug3 = 7;
    internal const int Severity_Number_Debug4 = 8;
    internal const int Severity_Number_Info = 9;
    internal const int Severity_Number_Info2 = 10;
    internal const int Severity_Number_Info3 = 11;
    internal const int Severity_Number_Info4 = 12;
    internal const int Severity_Number_Warn = 13;
    internal const int Severity_Number_Warn2 = 14;
    internal const int Severity_Number_Warn3 = 15;
    internal const int Severity_Number_Warn4 = 16;
    internal const int Severity_Number_Error = 17;
    internal const int Severity_Number_Error2 = 18;
    internal const int Severity_Number_Error3 = 19;
    internal const int Severity_Number_Error4 = 20;
    internal const int Severity_Number_Fatal = 21;
    internal const int Severity_Number_Fatal2 = 22;
    internal const int Severity_Number_Fatal3 = 23;
    internal const int Severity_Number_Fatal4 = 24;

    // LogRecordFlags
    internal const int LogRecord_Flags_Do_Not_Use = 0;
    internal const int LogRecord_Flags_Trace_Flags_Mask = 0x000000FF;

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
