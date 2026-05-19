// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

typedef
HINTERNET
(WINAPI * PFN_WINHTTP_WEBSOCKET_COMPLETE_UPGRADE)(
    _In_ HINTERNET hRequest,
    DWORD_PTR pContext
);


typedef
DWORD
(WINAPI * PFN_WINHTTP_WEBSOCKET_SEND)(
    _In_ HINTERNET hWebSocket,
    _In_ WINHTTP_WEB_SOCKET_BUFFER_TYPE eBufferType,
    _In_reads_opt_(dwBufferLength) PVOID pvBuffer,
    _In_ DWORD dwBufferLength
);

typedef
DWORD
(WINAPI * PFN_WINHTTP_WEBSOCKET_RECEIVE)(
    _In_ HINTERNET hWebSocket,
    _Out_writes_bytes_to_(dwBufferLength, *pdwBytesRead) PVOID pvBuffer,
    _In_ DWORD dwBufferLength,
    _Out_range_(0, dwBufferLength) DWORD *pdwBytesRead,
    _Out_ WINHTTP_WEB_SOCKET_BUFFER_TYPE *peBufferType
);

typedef
DWORD
(WINAPI * PFN_WINHTTP_WEBSOCKET_SHUTDOWN)(
    _In_ HINTERNET hWebSocket,
    _In_ USHORT usStatus,
    _In_reads_bytes_opt_(dwReasonLength) PVOID pvReason,
    _In_range_(0, WINHTTP_WEB_SOCKET_MAX_CLOSE_REASON_LENGTH) DWORD dwReasonLength
);

typedef
DWORD
(WINAPI * PFN_WINHTTP_WEBSOCKET_QUERY_CLOSE_STATUS)(
    _In_ HINTERNET hWebSocket,
    _Out_ USHORT *pusStatus,
    _Out_writes_bytes_to_opt_(dwReasonLength, *pdwReasonLengthConsumed) PVOID pvReason,
    _In_range_(0, WINHTTP_WEB_SOCKET_MAX_CLOSE_REASON_LENGTH) DWORD dwReasonLength,
    _Out_range_(0, WINHTTP_WEB_SOCKET_MAX_CLOSE_REASON_LENGTH) DWORD *pdwReasonLengthConsumed
);

class WINHTTP_HELPER
{
public:
    static
    HRESULT
    StaticInitialize();

    static
    VOID
    GetFlagsFromBufferType(
        __in  WINHTTP_WEB_SOCKET_BUFFER_TYPE   BufferType,
        __out BOOL *                           pfUtf8Encoded,
        __out BOOL *                           pfFinalFragment,
        __out BOOL *                           pfClose
    );

    static
    VOID
    GetBufferTypeFromFlags(
        __in  BOOL                             fUtf8Encoded,
        __in  BOOL                             fFinalFragment,
        __in  BOOL                             fClose,
        __out WINHTTP_WEB_SOCKET_BUFFER_TYPE*  pBufferType
    );

    static
    PFN_WINHTTP_WEBSOCKET_COMPLETE_UPGRADE      sm_pfnWinHttpWebSocketCompleteUpgrade;

    static
    PFN_WINHTTP_WEBSOCKET_SEND                  sm_pfnWinHttpWebSocketSend;

    static
    PFN_WINHTTP_WEBSOCKET_RECEIVE               sm_pfnWinHttpWebSocketReceive;

    static
    PFN_WINHTTP_WEBSOCKET_SHUTDOWN              sm_pfnWinHttpWebSocketShutdown;

    static
    PFN_WINHTTP_WEBSOCKET_QUERY_CLOSE_STATUS    sm_pfnWinHttpWebSocketQueryCloseStatus;
};