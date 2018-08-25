// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include <array>
#include "EventLog.h"
#include "debugutil.h"
#include "StringHelpers.h"

extern HANDLE       g_hEventLog;

VOID
EventLog::LogEvent(
    _In_ WORD    dwEventInfoType,
    _In_ DWORD   dwEventId,
    _In_ LPCWSTR pstrMsg
)
{
    // Static locals to avoid getting the process ID and string multiple times.
    // Effectively have the same semantics as global variables, except initialized
    // on first occurence.
    static const auto processIdString = GetProcessIdString();
    static const auto versionInfoString = GetVersionInfoString();

    std::array<LPCWSTR, 3> eventLogDataStrings
    {
        pstrMsg,
        processIdString.c_str(),
        versionInfoString.c_str()
    };

    if (g_hEventLog != NULL)
    {
        ReportEventW(g_hEventLog,
            dwEventInfoType,
            0,        // wCategory
            dwEventId,
            NULL,     // lpUserSid
            3,        // wNumStrings
            0,        // dwDataSize,
            eventLogDataStrings.data(),
            NULL      // lpRawData
        );
    }

    DebugPrintfW(dwEventInfoType == EVENTLOG_ERROR_TYPE ? ASPNETCORE_DEBUG_FLAG_ERROR : ASPNETCORE_DEBUG_FLAG_INFO, L"Event Log: '%ls' \r\nEnd Event Log Message.", pstrMsg);
}

VOID
EventLog::LogEventF(
    _In_ WORD    dwEventInfoType,
    _In_ DWORD   dwEventId,
    _In_ LPCWSTR pstrMsg,
    va_list argsList
)
{
    STACK_STRU ( strEventMsg, 256 );

    if (SUCCEEDED(strEventMsg.SafeVsnwprintf(
        pstrMsg,
        argsList)))
    {
        EventLog::LogEvent(
            dwEventInfoType,
            dwEventId,
            strEventMsg.QueryStr());
    }
}
