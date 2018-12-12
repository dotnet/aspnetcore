/*++

Copyright (c) 2014 Microsoft Corporation

Module Name:

    aspnetcore_msg.mc

Abstract:

    Asp.Net Core Module localizable messages.

--*/


#ifndef _ASPNETCORE_MSG_H_
#define _ASPNETCORE_MSG_H_

//
//  Values are 32 bit values laid out as follows:
//
//   3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
//   1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
//  +---+-+-+-----------------------+-------------------------------+
//  |Sev|C|R|     Facility          |               Code            |
//  +---+-+-+-----------------------+-------------------------------+
//
//  where
//
//      Sev - is the severity code
//
//          00 - Success
//          01 - Informational
//          10 - Warning
//          11 - Error
//
//      C - is the Customer code flag
//
//      R - is a reserved bit
//
//      Facility - is the facility code
//
//      Code - is the facility's status code
//
//
// Define the facility codes
//


//
// Define the severity codes
//


//
// MessageId: ASPNETCORE_EVENT_PROCESS_START_ERROR
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_PROCESS_START_ERROR ((DWORD)0x000003E8L)

//
// MessageId: ASPNETCORE_EVENT_PROCESS_START_SUCCESS
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_PROCESS_START_SUCCESS ((DWORD)0x000003E9L)

//
// MessageId: ASPNETCORE_EVENT_PROCESS_CRASH
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_PROCESS_CRASH   ((DWORD)0x000003EAL)

//
// MessageId: ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED ((DWORD)0x000003EBL)

//
// MessageId: ASPNETCORE_EVENT_CONFIG_ERROR
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_CONFIG_ERROR    ((DWORD)0x000003ECL)

//
// MessageId: ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE ((DWORD)0x000003EDL)

//
// MessageId: ASPNETCORE_EVENT_SENT_SHUTDOWN_HTTP_REQUEST
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_SENT_SHUTDOWN_HTTP_REQUEST ((DWORD)0x000003EEL)

//
// MessageId: ASPNETCORE_EVENT_RECYCLE_APPOFFLINE
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_RECYCLE_APPOFFLINE ((DWORD)0x000003F4L)


#endif     // _ASPNETCORE_MODULE_MSG_H_

