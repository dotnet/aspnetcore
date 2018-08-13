// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "EventLog.h"
#include "debugutil.h"

extern HANDLE       g_hEventLog;

VOID
EventLog::LogEvent(
    _In_ WORD    dwEventInfoType,
    _In_ DWORD   dwEventId,
    _In_ LPCWSTR pstrMsg
)
{
    if (g_hEventLog != NULL)
    {
        ReportEventW(g_hEventLog,
            dwEventInfoType,
            0,        // wCategory
            dwEventId,
            NULL,     // lpUserSid
            1,        // wNumStrings
            0,        // dwDataSize,
            &pstrMsg,
            NULL      // lpRawData
        );
    }

    DebugPrintf(dwEventInfoType == EVENTLOG_ERROR_TYPE ? ASPNETCORE_DEBUG_FLAG_ERROR : ASPNETCORE_DEBUG_FLAG_INFO, "Event Log: %S", pstrMsg);
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
