;/*++
;
; Copyright (c) .NET Foundation. All rights reserved.
; Licensed under the MIT License. See License.txt in the project root for license information.
;
;Module Name:
;
;    aspnetcore_msg.mc
;
;Abstract:
;
;    Asp.Net Core Module localizable messages.
;
;--*/
;
;
;#ifndef _ASPNETCORE_MSG_H_
;#define _ASPNETCORE_MSG_H_
;

SeverityNames=(Success=0x0
               Informational=0x1
               Warning=0x2
               Error=0x3
              )

MessageIdTypedef=DWORD

Messageid=1000
SymbolicName=ASPNETCORE_EVENT_PROCESS_START_ERROR
Language=English
%1
.

Messageid=1001
SymbolicName=ASPNETCORE_EVENT_PROCESS_START_SUCCESS
Language=English
%1
.

Messageid=1002
SymbolicName=ASPNETCORE_EVENT_PROCESS_CRASH
Language=English
%1
.

Messageid=1003
SymbolicName=ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED
Language=English
%1
.

Messageid=1004
SymbolicName=ASPNETCORE_EVENT_CONFIG_ERROR
Language=English
%1
.

Messageid=1005
SymbolicName=ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE
Language=English
%1
.

Messageid=1006
SymbolicName=ASPNETCORE_EVENT_SENT_SHUTDOWN_HTTP_REQUEST
Language=English
%1
.

Messageid=1007
SymbolicName=ASPNETCORE_EVENT_LOAD_CLR_FALIURE
Language=English
%1
.

Messageid=1008
SymbolicName=ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP
Language=English
%1
.

Messageid=1009
SymbolicName=ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR
Language=English
%1
.

Messageid=1010
SymbolicName=ASPNETCORE_EVENT_ADD_APPLICATION_ERROR
Language=English
%1
.

Messageid=1011
SymbolicName=ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT
Language=English
%1
.

Messageid=1012
SymbolicName=ASPNETCORE_EVENT_RECYCLE_APPOFFLINE
Language=English
%1
.

Messageid=1013
SymbolicName=ASPNETCORE_EVENT_MODULE_DISABLED
Language=English
%1
.

;
;#endif     // _ASPNETCORE_MODULE_MSG_H_
;
