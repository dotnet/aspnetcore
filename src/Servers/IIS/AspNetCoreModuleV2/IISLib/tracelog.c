// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include <windows.h>
#include "tracelog.h"
#include <intsafe.h>


#define ALLOC_MEM(cb) (PVOID)LocalAlloc( LPTR, (cb) )
#define FREE_MEM(ptr) (VOID)LocalFree( (HLOCAL)(ptr) )


PTRACE_LOG
CreateTraceLog(
    IN LONG LogSize,
    IN LONG ExtraBytesInHeader,
    IN LONG EntrySize
    )
/*++

Routine Description:

    Creates a new (empty) trace log buffer.

Arguments:

    LogSize - The number of entries in the log.

    ExtraBytesInHeader - The number of extra bytes to include in the
        log header. This is useful for adding application-specific
        data to the log.

    EntrySize - The size (in bytes) of each entry.

Return Value:

    PTRACE_LOG - Pointer to the newly created log if successful,
        NULL otherwise.

--*/
{

    ULONG ulTotalSize = 0;
    ULONG ulLogSize = 0;
    ULONG ulEntrySize = 0;
    ULONG ulTmpResult = 0;
    ULONG ulExtraBytesInHeader = 0;
    PTRACE_LOG log = nullptr;
    HRESULT hr = S_OK;

    //
    // Sanity check the parameters.
    //

    //DBG_ASSERT( LogSize > 0 );
    //DBG_ASSERT( EntrySize > 0 );
    //DBG_ASSERT( ExtraBytesInHeader >= 0 );
    //DBG_ASSERT( ( EntrySize & 3 ) == 0 );

    //
    // converting to unsigned long. Since all these values are positive
    // so its safe to cast them to their unsigned equivalent directly.
    //
    ulLogSize            = (ULONG) LogSize;
    ulEntrySize          = (ULONG) EntrySize;
    ulExtraBytesInHeader = (ULONG) ExtraBytesInHeader;

    //
    // Check if the multiplication operation will overflow a LONG
    // ulTotalSize = LogSize * EntrySize;
    //
    hr = ULongMult( ulLogSize, ulEntrySize, &ulTotalSize );
    if ( FAILED(hr) )
    {
        SetLastError( ERROR_ARITHMETIC_OVERFLOW );
        return nullptr;
    }

    //
    // check for overflow in addition operation.
    // ulTmpResult = sizeof(TRACE_LOG) + ulExtraBytesInHeader
    //
    hr = ULongAdd( (ULONG) sizeof(TRACE_LOG), ulExtraBytesInHeader, &ulTmpResult );
    if ( FAILED(hr) )
    {
        SetLastError( ERROR_ARITHMETIC_OVERFLOW );
        return nullptr;
    }

    //
    // check for overflow in addition operation.
    // ulTotalSize = ulTotalSize + ulTmpResult;
    //
    hr = ULongAdd( ulTmpResult, ulTotalSize, &ulTotalSize );
    if ( FAILED(hr) )
    {
        SetLastError( ERROR_ARITHMETIC_OVERFLOW );
        return nullptr;
    }

    if ( ulTotalSize > (ULONG) 0x7FFFFFFF )
    {
        SetLastError( ERROR_ARITHMETIC_OVERFLOW );
        return nullptr;
    }

    //
    // Allocate & initialize the log structure.
    //

    log = (PTRACE_LOG)ALLOC_MEM( ulTotalSize );

    //
    // Initialize it.
    //

    if( log != nullptr ) {

        RtlZeroMemory( log, ulTotalSize );

        log->Signature = TRACE_LOG_SIGNATURE;
        log->LogSize = LogSize;
        log->NextEntry = -1;
        log->EntrySize = EntrySize;
        log->LogBuffer = (PUCHAR)( log + 1 ) + ExtraBytesInHeader;
    }

    return log;

}   // CreateTraceLog

VOID
DestroyTraceLog(
    IN PTRACE_LOG Log
    )
/*++

Routine Description:

    Destroys a trace log buffer created with CreateTraceLog().

Arguments:

    Log - The trace log buffer to destroy.

Return Value:

    None.

--*/
{
        if ( Log != nullptr ) {
        //DBG_ASSERT( Log->Signature == TRACE_LOG_SIGNATURE );

        Log->Signature = TRACE_LOG_SIGNATURE_X;
        FREE_MEM( Log );
    }

}   // DestroyTraceLog

LONG
WriteTraceLog(
    IN PTRACE_LOG Log,
    IN PVOID Entry
    )
/*++

Routine Description:

    Writes a new entry to the specified trace log.

Arguments:

    Log - The log to write to.

    Entry - Pointer to the data to write. This buffer is assumed to be
        Log->EntrySize bytes long.

Return Value:

    Index of entry in log.  This is useful for correlating the output
    of !inetdbg.ref to a particular point in the output debug stream

--*/
{

    PUCHAR target = nullptr;
    ULONG index = 0;

    //DBG_ASSERT( Log != NULL );
    //DBG_ASSERT( Log->Signature == TRACE_LOG_SIGNATURE );
    //DBG_ASSERT( Entry != NULL );

    //
    // Find the next slot, copy the entry to the slot.
    //

    index = ( (ULONG) InterlockedIncrement( &Log->NextEntry ) ) % (ULONG) Log->LogSize;

    //DBG_ASSERT( index < (ULONG) Log->LogSize );

    target = Log->LogBuffer + ( index * Log->EntrySize );

    RtlCopyMemory(
        target,
        Entry,
        Log->EntrySize
        );

    return index;
}   // WriteTraceLog

VOID
ResetTraceLog(
    IN PTRACE_LOG Log
    )
{

    //DBG_ASSERT( Log != NULL );
    //DBG_ASSERT( Log->Signature == TRACE_LOG_SIGNATURE );

    RtlZeroMemory(
        ( Log + 1 ),
        Log->LogSize * Log->EntrySize
        );

    Log->NextEntry = -1;

}   // ResetTraceLog
