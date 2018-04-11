/*++

 Copyright (c) .NET Foundation. All rights reserved.
 Licensed under the MIT License. See License.txt in the project root for license information.

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
// MessageId: ASPNETCORE_EVENT_LOAD_CLR_FALIURE
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_LOAD_CLR_FALIURE ((DWORD)0x000003EFL)

//
// MessageId: ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP ((DWORD)0x000003F0L)

//
// MessageId: ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR ((DWORD)0x000003F1L)

//
// MessageId: ASPNETCORE_EVENT_ADD_APPLICATION_ERROR
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_ADD_APPLICATION_ERROR ((DWORD)0x000003F2L)

//
// MessageId: ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT ((DWORD)0x000003F3L)

//
// MessageId: ASPNETCORE_EVENT_RECYCLE_APPOFFLINE
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_RECYCLE_APPOFFLINE ((DWORD)0x000003F4L)

//
// MessageId: ASPNETCORE_EVENT_MODULE_DISABLED
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_MODULE_DISABLED ((DWORD)0x000003F5L)

//
// MessageId: ASPNETCORE_EVENT_INPROCESS_FULL_FRAMEWORK_APP
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_INPROCESS_FULL_FRAMEWORK_APP ((DWORD)0x000003F6L)

//
// MessageId: ASPNETCORE_EVENT_PORTABLE_APP_DOTNET_MISSING
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_PORTABLE_APP_DOTNET_MISSING ((DWORD)0x000003F7L)

//
// MessageId: ASPNETCORE_EVENT_HOSTFXR_DIRECTORY_NOT_FOUND
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_HOSTFXR_DIRECTORY_NOT_FOUND ((DWORD)0x000003F8L)

//
// MessageId: ASPNETCORE_EVENT_HOSTFXR_DLL_NOT_FOUND
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_HOSTFXR_DLL_NOT_FOUND ((DWORD)0x000003F9L)

//
// MessageId: ASPNETCORE_EVENT_INPROCESS_THREAD_EXCEPTION
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_INPROCESS_THREAD_EXCEPTION ((DWORD)0x000003FAL)

//
// MessageId: ASPNETCORE_EVENT_APPLICATION_EXE_NOT_FOUND
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_APPLICATION_EXE_NOT_FOUND ((DWORD)0x000003FBL)

//
// MessageId: ASPNETCORE_EVENT_PROCESS_START_FAILURE
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_PROCESS_START_FAILURE ((DWORD)0x000003FCL)

//
// MessageId: ASPNETCORE_EVENT_RECYCLE_CONFIGURATION
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_RECYCLE_CONFIGURATION ((DWORD)0x000003FDL)

//
// MessageId: ASPNETCORE_EVENT_RECYCLE_APP_FAILURE
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_RECYCLE_APP_FAILURE ((DWORD)0x000003FEL)

//
// MessageId: ASPNETCORE_EVENT_APP_IN_SHUTDOWN
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_APP_IN_SHUTDOWN ((DWORD)0x000003FFL)

//
// MessageId: ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_REMOVED
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_REMOVED ((DWORD)0x00000400L)

//
// MessageId: ASPNETCORE_EVENT_GENERAL_INFO_MSG
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_GENERAL_INFO_MSG ((DWORD)0x00000401L)

//
// MessageId: ASPNETCORE_EVENT_GENERAL_WARNING_MSG
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_GENERAL_WARNING_MSG ((DWORD)0x00000402L)

//
// MessageId: ASPNETCORE_EVENT_GENERAL_ERROR_MSG
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_GENERAL_ERROR_MSG ((DWORD)0x00000403L)

//
// MessageId: ASPNETCORE_EVENT_INPROCESS_RH_MISSING
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_INPROCESS_RH_MISSING ((DWORD)0x00000404L)

//
// MessageId: ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING
//
// MessageText:
//
// %1
//
#define ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING ((DWORD)0x00000405L)


#endif     // _ASPNETCORE_MODULE_MSG_H_

