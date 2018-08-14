// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "resources.h"

class EventLog
{
public:
    static
    VOID
    Error(
        _In_ DWORD   dwEventId,
        _In_ PCWSTR  pstrMsg,
        ...)
    {
       va_list args;
       va_start(args, pstrMsg);
       LogEventF(EVENTLOG_ERROR_TYPE, dwEventId, pstrMsg, args);
       va_end(args);
    }

    static
    VOID
    Info(
        _In_ DWORD   dwEventId,
        _In_ PCWSTR  pstrMsg,
        ...)
    {
       va_list args;
       va_start(args, pstrMsg);
       LogEventF(EVENTLOG_INFORMATION_TYPE, dwEventId, pstrMsg, args);
       va_end(args);
    }

    static
    VOID
    Warn(
        _In_ DWORD   dwEventId,
        _In_ PCWSTR  pstrMsg,
        ...)
    {
       va_list args;
       va_start(args, pstrMsg);
       LogEventF(EVENTLOG_WARNING_TYPE, dwEventId, pstrMsg, args);
       va_end(args);
    }

private:
    static
    VOID
    LogEvent(
        _In_ WORD    dwEventInfoType,
        _In_ DWORD   dwEventId,
        _In_ LPCWSTR pstrMsg
    );

    static
    VOID
    LogEventF(
        _In_ WORD    dwEventInfoType,
        _In_ DWORD   dwEventId,
        __in PCWSTR  pstrMsg,
        va_list argsList
    );
};
