// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "resources.h"

#define _va_start(ap, x) \
    __pragma(warning(push)) \
    __pragma(warning(disable:26481 26492)) /*Don't use pointer arithmetic. Don't use const_cast to cast away const.*/ \
    va_start(ap, x) \
    __pragma(warning(pop))

#define _va_end(args) \
    __pragma(warning(push)) \
    __pragma(warning(disable:26477)) /*Use 'nullptr' rather than 0 or NULL*/ \
    va_end(args) \
    __pragma(warning(pop))

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
       _va_start(args, pstrMsg);
       LogEventF(EVENTLOG_ERROR_TYPE, dwEventId, pstrMsg, args);
       _va_end(args);
    }

    static
    VOID
    Info(
        _In_ DWORD   dwEventId,
        _In_ PCWSTR  pstrMsg,
        ...)
    {
       va_list args;
       _va_start(args, pstrMsg);
       LogEventF(EVENTLOG_INFORMATION_TYPE, dwEventId, pstrMsg, args);
       _va_end(args);
    }

    static
    VOID
    Warn(
        _In_ DWORD   dwEventId,
        _In_ PCWSTR  pstrMsg,
        ...)
    {
       va_list args;
       _va_start(args, pstrMsg);
       LogEventF(EVENTLOG_WARNING_TYPE, dwEventId, pstrMsg, args);
       _va_end(args);
    }

    static
    bool
    LogEventNoTrace(
        _In_ WORD    dwEventInfoType,
        _In_ DWORD   dwEventId,
        _In_ LPCWSTR pstrMsg);

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
