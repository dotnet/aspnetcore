// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#ifndef _DBGUTIL_H_
#define _DBGUTIL_H_

#include <crtdbg.h>

//
// TODO 
//      Using _CrtDbg implementation. If hooking is desired
//      wrappers should be provided here so that we can reimplement
//      if neecessary.
// 
//      IF_DEBUG/DEBUG FLAGS
//
//      registry configuration
//

//
// Debug error levels for DEBUG_FLAGS_VAR.
//

#define DEBUG_FLAG_INFO     0x00000001
#define DEBUG_FLAG_WARN     0x00000002
#define DEBUG_FLAG_ERROR    0x00000004

//
// Predefined error level values. These are backwards from the
// windows definitions.
//

#define DEBUG_FLAGS_INFO    (DEBUG_FLAG_ERROR | DEBUG_FLAG_WARN | DEBUG_FLAG_INFO)
#define DEBUG_FLAGS_WARN    (DEBUG_FLAG_ERROR | DEBUG_FLAG_WARN)
#define DEBUG_FLAGS_ERROR   (DEBUG_FLAG_ERROR)
#define DEBUG_FLAGS_ANY     (DEBUG_FLAG_INFO | DEBUG_FLAG_WARN | DEBUG_FLAG_ERROR)

//
// Global variables to control tracing. Generally per module
//

#ifndef DEBUG_FLAGS_VAR
#define DEBUG_FLAGS_VAR g_dwDebugFlags
#endif

#ifndef DEBUG_LABEL_VAR
#define DEBUG_LABEL_VAR g_szDebugLabel
#endif

extern PCSTR DEBUG_LABEL_VAR;
extern DWORD DEBUG_FLAGS_VAR;

//
// Module should make this declaration globally.
//

#define DECLARE_DEBUG_PRINT_OBJECT( _pszLabel_ )                \
    PCSTR DEBUG_LABEL_VAR = _pszLabel_;                 \
    DWORD DEBUG_FLAGS_VAR = DEBUG_FLAGS_ANY;            \

#define DECLARE_DEBUG_PRINT_OBJECT2( _pszLabel_, _dwLevel_ )    \
    PCSTR DEBUG_LABEL_VAR = _pszLabel_;                 \
    DWORD DEBUG_FLAGS_VAR = _dwLevel_;                  \

//
// This doesn't do anything now. Should be safe to call in dll main.
//

#define CREATE_DEBUG_PRINT_OBJECT

//
// Trace macros
//

#define DBG_CONTEXT     _CRT_WARN, __FILE__, __LINE__, DEBUG_LABEL_VAR

#ifdef DEBUG
#define DBGINFO(args)   \
{if (DEBUG_FLAGS_VAR & DEBUG_FLAG_INFO) { _CrtDbgReport args; }}
#define DBGWARN(args)   \
{if (DEBUG_FLAGS_VAR & DEBUG_FLAG_WARN) { _CrtDbgReport args; }}
#define DBGERROR(args)  \
{if (DEBUG_FLAGS_VAR & DEBUG_FLAG_ERROR) { _CrtDbgReport args; }}
#else
#define DBGINFO
#define DBGWARN
#define DBGERROR
#endif

#define DBGPRINTF           DBGINFO

//
// Simple error traces
//

#define DBGERROR_HR( _hr_ ) \
    DBGERROR((DBG_CONTEXT, "hr=0x%x\n", _hr_))

#define DBGERROR_STATUS( _status_ ) \
    DBGERROR((DBG_CONTEXT, "status=%d\n", _status_))

#endif
