// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#ifndef _REFTRACE_H_
#define _REFTRACE_H_


#if defined(__cplusplus)
extern "C" {
#endif  // __cplusplus

#include <Windows.h>
#include "tracelog.h"

//
// This is the number of stack backtrace values captured in each
// trace log entry. This value is chosen to make the log entry
// exactly twelve dwords long, making it a bit easier to interpret
// from within the debugger without the debugger extension.
//

#define REF_TRACE_LOG_STACK_DEPTH   9

// No-op value for the Context1,2,3 parameters of WriteRefTraceLogEx
//#define REF_TRACE_EMPTY_CONTEXT ((PVOID) -1)
#define REF_TRACE_EMPTY_CONTEXT nullptr


//
// This defines the entry written to the trace log.
//

typedef struct _REF_TRACE_LOG_ENTRY {

    LONG NewRefCount;
    CONST VOID * Context;
    CONST VOID * Context1;
    CONST VOID * Context2;
    CONST VOID * Context3;
    DWORD Thread;
    PVOID Stack[REF_TRACE_LOG_STACK_DEPTH];

} REF_TRACE_LOG_ENTRY, *PREF_TRACE_LOG_ENTRY;


//
// Manipulators.
//

PTRACE_LOG
CreateRefTraceLog(
    IN LONG LogSize,
    IN LONG ExtraBytesInHeader
    );

VOID
DestroyRefTraceLog(
    IN PTRACE_LOG Log
    );

LONG
__cdecl
WriteRefTraceLog(
    IN PTRACE_LOG Log,
    IN LONG NewRefCount,
    IN CONST VOID * Context
    );

LONG
__cdecl
WriteRefTraceLogEx(
    IN PTRACE_LOG Log,
    IN LONG NewRefCount,
    IN CONST VOID * Context,
    IN CONST VOID * Context1,
    IN CONST VOID * Context2,
    IN CONST VOID * Context3
    );


#if defined(__cplusplus)
}   // extern "C"
#endif  // __cplusplus


#endif  // _REFTRACE_H_

