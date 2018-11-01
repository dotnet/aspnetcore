// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#define IDS_INVALID_PROPERTY        1000
#define IDS_SERVER_ERROR            1001

#define ASPNETCORE_EVENT_MSG_BUFFER_SIZE 256
#define ASPNETCORE_EVENT_PROCESS_START_SUCCESS_MSG           L"Application '%s' started process '%d' successfully and is listening on port '%d'."
#define ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED_MSG       L"Maximum rapid fail count per minute of '%d' exceeded."
#define ASPNETCORE_EVENT_PROCESS_START_INTERNAL_ERROR_MSG    L"Application '%s' failed to parse processPath and arguments due to internal error, ErrorCode = '0x%x'."
#define ASPNETCORE_EVENT_PROCESS_START_POSTCREATE_ERROR_MSG  L"Application '%s' with physical root '%s' created process with commandline '%s'but failed to get its status, ErrorCode = '0x%x'."
#define ASPNETCORE_EVENT_PROCESS_START_ERROR_MSG             L"Application '%s' with physical root '%s' failed to start process with commandline '%s', ErrorCode = '0x%x' : %x."
#define ASPNETCORE_EVENT_PROCESS_START_WRONGPORT_ERROR_MSG   L"Application '%s' with physical root '%s' created process with commandline '%s' but failed to listen on the given port '%d'"
#define ASPNETCORE_EVENT_PROCESS_START_NOTREADY_ERROR_MSG    L"Application '%s' with physical root '%s' created process with commandline '%s' but either crashed or did not reponse or did not listen on the given port '%d', ErrorCode = '0x%x'"
#define ASPNETCORE_EVENT_INVALID_STDOUT_LOG_FILE_MSG         L"Warning: Could not create stdoutLogFile %s, ErrorCode = %d."
#define ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE_MSG       L"Failed to gracefully shutdown process '%d'."
#define ASPNETCORE_EVENT_SENT_SHUTDOWN_HTTP_REQUEST_MSG      L"Sent shutdown HTTP message to process '%d' and received http status '%d'."
#define ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_MSG              L"App_offline file '%s' was detected."
#define ASPNETCORE_EVENT_PROCESS_SHUTDOWN_MSG                L"Application '%s' with physical root '%s' shut down process with Id '%d' listening on port '%d'"
