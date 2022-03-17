// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once


#ifndef _STTIMER_H
#define _STTIMER_H
#include "stringu.h"

class STTIMER
{
public:

    STTIMER()
        : _pTimer( NULL )
    {
        fInCanel = FALSE;
    }

    virtual
    ~STTIMER()
    {
        if ( _pTimer )
        {
            CancelTimer();
            CloseThreadpoolTimer( _pTimer );
            _pTimer = NULL;
        }
    }

    HRESULT
    InitializeTimer(
        PTP_TIMER_CALLBACK   pfnCallback,
        VOID               * pContext,
        DWORD                dwInitialWait = 0,
        DWORD                dwPeriod = 0
        )
    {
        _pTimer = CreateThreadpoolTimer( pfnCallback,
                                         pContext,
                                         NULL );

        if ( !_pTimer )
        {
            return HRESULT_FROM_WIN32( GetLastError() );
        }

        if ( dwInitialWait )
        {
            SetTimer( dwInitialWait,
                      dwPeriod );
        }

        return S_OK;
    }

    VOID
    SetTimer(
        DWORD dwInitialWait,
        DWORD dwPeriod = 0
        )
    {
        FILETIME ftInitialWait;

        if ( dwInitialWait == 0 && dwPeriod == 0 )
        {
            //
            // Special case.  We are preventing new callbacks
            // from being queued.  Any existing callbacks in the
            // queue will still run.
            //
            // This effectively disables the timer.  It can be
            // re-enabled by setting non-zero initial wait or
            // period values.
            //
            if (_pTimer != NULL)
            {
                SetThreadpoolTimer(_pTimer, NULL, 0, 0);
            }

            return;
        }

        InitializeRelativeFileTime( &ftInitialWait, dwInitialWait );

        SetThreadpoolTimer( _pTimer,
                            &ftInitialWait,
                            dwPeriod,
                            0 );
    }

    VOID
    CancelTimer()
    {
        //
        // Disable the timer
        //
        if (fInCanel)
            return;

        fInCanel = TRUE;
        SetTimer( 0 );

        //
        // Wait until any callbacks queued prior to disabling
        // have completed.
        //
        if (_pTimer != NULL)
        {
            WaitForThreadpoolTimerCallbacks(_pTimer, TRUE);
        }

        fInCanel = FALSE;
    }

    static
    VOID
    CALLBACK
    TimerCallback(
        _In_ PTP_CALLBACK_INSTANCE ,
        _In_ PVOID Context,
        _In_ PTP_TIMER 
    )
    {
        STRU*                   pstruLogFilePath = (STRU*)Context;
        HANDLE                  hStdoutHandle = NULL;
        SECURITY_ATTRIBUTES     saAttr = { 0 };
        HRESULT                 hr = S_OK;

        saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
        saAttr.bInheritHandle = TRUE;
        saAttr.lpSecurityDescriptor = NULL;

        hStdoutHandle = CreateFileW(pstruLogFilePath->QueryStr(),
                                    FILE_READ_DATA,
                                    FILE_SHARE_WRITE,
                                    &saAttr,
                                    OPEN_ALWAYS,
                                    FILE_ATTRIBUTE_NORMAL,
                                    NULL);
        if (hStdoutHandle == INVALID_HANDLE_VALUE)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }

        CloseHandle(hStdoutHandle);
    }

private:

    VOID
    InitializeRelativeFileTime(
        FILETIME * pft,
        DWORD      dwMilliseconds
        )
    {
        LARGE_INTEGER li;

        //
        // The pftDueTime parameter expects the time to be
        // expressed as the number of 100 nanosecond intervals
        // times -1.
        //
        // To convert from milliseconds, we'll multiply by
        // -10000
        //

        li.QuadPart = (LONGLONG)dwMilliseconds * -10000;

        pft->dwHighDateTime = li.HighPart;
        pft->dwLowDateTime = li.LowPart;
    };

    TP_TIMER * _pTimer;
    BOOL       fInCanel;
};

class STELAPSED
{
public:

    STELAPSED()
        : _dwInitTime( 0 ),
          _dwInitTickCount( 0 ),
          _dwPerfCountsPerMillisecond( 0 ),
          _fUsingHighResolution( FALSE )
    {
        LARGE_INTEGER li;
        BOOL          fResult;

        _dwInitTickCount = GetTickCount64();

        fResult = QueryPerformanceFrequency( &li );

        if ( !fResult )
        {
            goto Finished;
        }

        _dwPerfCountsPerMillisecond = li.QuadPart / 1000;

        fResult = QueryPerformanceCounter( &li );

        if ( !fResult )
        {
            goto Finished;
        }

        _dwInitTime = li.QuadPart / _dwPerfCountsPerMillisecond;

        _fUsingHighResolution = TRUE;

Finished:

        return;
    }

    virtual
    ~STELAPSED()
    {
    }

    LONGLONG
    QueryElapsedTime()
    {
        LARGE_INTEGER li;

        if ( _fUsingHighResolution && QueryPerformanceCounter( &li ) )
        {
            DWORD64 dwCurrentTime = li.QuadPart / _dwPerfCountsPerMillisecond;

            if ( dwCurrentTime < _dwInitTime )
            {
                //
                // It's theoretically possible that QueryPerformanceCounter
                // may return slightly different values on different CPUs.
                // In this case, we don't want to return an unexpected value
                // so we'll return zero.  This is acceptable because
                // presumably such a case would only happen for a very short
                // time window.
                //
                // It would be possible to prevent this by ensuring processor
                // affinity for all calls to QueryPerformanceCounter, but that
                // would be undesirable in the general case because it could
                // introduce unnecessary context switches and potentially a
                // CPU bottleneck.
                //
                // Note that this issue also applies to callers doing rapid
                // calls to this function.  If a caller wants to mitigate
                // that, they could enforce the affinitization, or they
                // could implement a similar sanity check when comparing
                // returned values from this function.
                //

                return 0;
            }

            return dwCurrentTime - _dwInitTime;
        }

        return GetTickCount64() - _dwInitTickCount;
    }

    BOOL
    QueryUsingHighResolution()
    {
        return _fUsingHighResolution;
    }

private:

    DWORD64 _dwInitTime;
    DWORD64 _dwInitTickCount;
    DWORD64 _dwPerfCountsPerMillisecond;
    BOOL    _fUsingHighResolution;
};

#endif // _STTIMER_H
