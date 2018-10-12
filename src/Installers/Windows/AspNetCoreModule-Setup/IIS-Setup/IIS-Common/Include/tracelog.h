// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _TRACELOG_H_
#define _TRACELOG_H_


#if defined(__cplusplus)
extern "C" {
#endif  // __cplusplus


typedef struct _TRACE_LOG {

    //
    // Signature.
    //

    LONG Signature;

    //
    // The total number of entries available in the log.
    //

    LONG LogSize;

    //
    // The index of the next entry to use.
    //

    LONG NextEntry;

    //
    // The byte size of each entry.
    //

    LONG EntrySize;

    //
    // Pointer to the start of the circular buffer.
    //

    PUCHAR LogBuffer;

    //
    // The extra header bytes and actual log entries go here.
    //
    // BYTE ExtraHeaderBytes[ExtraBytesInHeader];
    // BYTE Entries[LogSize][EntrySize];
    //

} TRACE_LOG, *PTRACE_LOG;


//
// Log header signature.
//

#define TRACE_LOG_SIGNATURE   ((DWORD)'gOlT')
#define TRACE_LOG_SIGNATURE_X ((DWORD)'golX')


//
// This macro maps a TRACE_LOG pointer to a pointer to the 'extra'
// data associated with the log.
//

#define TRACE_LOG_TO_EXTRA_DATA(log)    (PVOID)( (log) + 1 )


//
// Manipulators.
//

PTRACE_LOG
CreateTraceLog(
    IN LONG LogSize,
    IN LONG ExtraBytesInHeader,
    IN LONG EntrySize
    );

VOID
DestroyTraceLog(
    IN PTRACE_LOG Log
    );

LONG
WriteTraceLog(
    IN PTRACE_LOG Log,
    IN PVOID Entry
    );

VOID
ResetTraceLog(
    IN PTRACE_LOG Log
    );


#if defined(__cplusplus)
}   // extern "C"
#endif  // __cplusplus


#endif  // _TRACELOG_H_

