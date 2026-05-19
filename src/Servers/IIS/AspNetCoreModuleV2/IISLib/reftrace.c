// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include <windows.h>
#include "reftrace.h"

PTRACE_LOG
CreateRefTraceLog(
    IN LONG LogSize,
    IN LONG ExtraBytesInHeader
    )
/*++

Routine Description:

    Creates a new (empty) ref count trace log buffer.

Arguments:

    LogSize - The number of entries in the log.

    ExtraBytesInHeader - The number of extra bytes to include in the
        log header. This is useful for adding application-specific
        data to the log.

Return Value:

    PTRACE_LOG - Pointer to the newly created log if successful,
        NULL otherwise.

--*/
{

    return CreateTraceLog(
               LogSize,
               ExtraBytesInHeader,
               sizeof(REF_TRACE_LOG_ENTRY)
               );

}   // CreateRefTraceLog

VOID
DestroyRefTraceLog(
    IN PTRACE_LOG Log
    )
/*++

Routine Description:

    Destroys a ref count trace log buffer created with CreateRefTraceLog().

Arguments:

    Log - The ref count trace log buffer to destroy.

Return Value:

    None.

--*/
{

    DestroyTraceLog( Log );

}   // DestroyRefTraceLog

//
// N.B. For RtlCaptureBacktrace() to work properly, the calling function
// *must* be __cdecl, and must have a "normal" stack frame. So, we decorate
// WriteRefTraceLog[Ex]() with the __cdecl modifier and disable the frame
// pointer omission (FPO) optimization.
//

//#pragma optimize( "y", off )    // disable frame pointer omission (FPO)
#pragma optimize( "", off )    // disable frame pointer omission (FPO)

LONG
__cdecl
WriteRefTraceLog(
    IN PTRACE_LOG Log,
    IN LONG NewRefCount,
    IN CONST VOID * Context
    )
/*++

Routine Description:

    Writes a new entry to the specified ref count trace log. The entry
    written contains the updated reference count and a stack backtrace
    leading up to the current caller.

Arguments:

    Log - The log to write to.

    NewRefCount - The updated reference count.

    Context - An uninterpreted context to associate with the log entry.

Return Value:

    Index of entry in log.

--*/
{

    return WriteRefTraceLogEx(
        Log,
        NewRefCount,
        Context,
        REF_TRACE_EMPTY_CONTEXT, // suppress use of optional extra contexts
        REF_TRACE_EMPTY_CONTEXT,
        REF_TRACE_EMPTY_CONTEXT
        );

}   // WriteRefTraceLog

LONG
__cdecl
WriteRefTraceLogEx(
    IN PTRACE_LOG Log,
    IN LONG NewRefCount,
    IN CONST VOID * Context,
    IN CONST VOID * Context1, // optional extra context
    IN CONST VOID * Context2, // optional extra context
    IN CONST VOID * Context3  // optional extra context
    )
/*++

Routine Description:

    Writes a new "extended" entry to the specified ref count trace log.
    The entry written contains the updated reference count, stack backtrace
    leading up to the current caller and extra context information.

Arguments:

    Log - The log to write to.

    NewRefCount - The updated reference count.

    Context  - An uninterpreted context to associate with the log entry.
    Context1 - An uninterpreted context to associate with the log entry.
    Context2 - An uninterpreted context to associate with the log entry.
    Context3 - An uninterpreted context to associate with the log entry.

    NOTE Context1/2/3 are "optional" in that the caller may suppress
    debug display of these values by passing REF_TRACE_EMPTY_CONTEXT
    for each of them.

Return Value:

    Index of entry in log.

--*/
{

    REF_TRACE_LOG_ENTRY entry{};
    ULONG hash = 0;
    DWORD cStackFramesSkipped = 0;

    //
    // Initialize the entry.
    //

    RtlZeroMemory(
        &entry,
        sizeof(entry)
        );

    //
    //  Set log entry members.
    //

    entry.NewRefCount = NewRefCount;
    entry.Context = Context;
    entry.Thread = GetCurrentThreadId();
    entry.Context1 = Context1;
    entry.Context2 = Context2;
    entry.Context3 = Context3;

    //
    // Capture the stack backtrace. Normally, we skip two stack frames:
    // one for this routine, and one for RtlCaptureBacktrace() itself.
    // For non-Ex callers who come in via WriteRefTraceLog,
    // we skip three stack frames.
    //

    if (    entry.Context1 == REF_TRACE_EMPTY_CONTEXT
         && entry.Context2 == REF_TRACE_EMPTY_CONTEXT
         && entry.Context3 == REF_TRACE_EMPTY_CONTEXT
         ) {

         cStackFramesSkipped = 2;

    } else {

         cStackFramesSkipped = 1;

    }

    RtlCaptureStackBackTrace(
        cStackFramesSkipped,
        REF_TRACE_LOG_STACK_DEPTH,
        entry.Stack,
        &hash
        );

    //
    // Write it to the log.
    //

    return WriteTraceLog(
        Log,
        &entry
        );

}   // WriteRefTraceLogEx

#pragma optimize( "", on )      // restore frame pointer omission (FPO)

