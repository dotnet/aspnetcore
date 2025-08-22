// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "winhttphelper.h"
#include "exceptions.h"

PFN_WINHTTP_WEBSOCKET_COMPLETE_UPGRADE
WINHTTP_HELPER::sm_pfnWinHttpWebSocketCompleteUpgrade;

PFN_WINHTTP_WEBSOCKET_SEND
WINHTTP_HELPER::sm_pfnWinHttpWebSocketSend;

PFN_WINHTTP_WEBSOCKET_RECEIVE
WINHTTP_HELPER::sm_pfnWinHttpWebSocketReceive;

PFN_WINHTTP_WEBSOCKET_SHUTDOWN
WINHTTP_HELPER::sm_pfnWinHttpWebSocketShutdown;

PFN_WINHTTP_WEBSOCKET_QUERY_CLOSE_STATUS
WINHTTP_HELPER::sm_pfnWinHttpWebSocketQueryCloseStatus;

//static
HRESULT
WINHTTP_HELPER::StaticInitialize()
{
    //
    // Initialize the function pointers for WinHttp Websocket API's.
    //
    if (!g_fWebSocketStaticInitialize)
    {
        return S_OK;
    }

    HMODULE  hWinHttp = GetModuleHandleA("winhttp.dll");
    RETURN_LAST_ERROR_IF (hWinHttp == nullptr);

    sm_pfnWinHttpWebSocketCompleteUpgrade = (PFN_WINHTTP_WEBSOCKET_COMPLETE_UPGRADE)
        GetProcAddress(hWinHttp, "WinHttpWebSocketCompleteUpgrade");
    RETURN_LAST_ERROR_IF (sm_pfnWinHttpWebSocketCompleteUpgrade == nullptr);

    sm_pfnWinHttpWebSocketQueryCloseStatus = (PFN_WINHTTP_WEBSOCKET_QUERY_CLOSE_STATUS)
        GetProcAddress(hWinHttp, "WinHttpWebSocketQueryCloseStatus");
    RETURN_LAST_ERROR_IF (sm_pfnWinHttpWebSocketQueryCloseStatus == nullptr);

    sm_pfnWinHttpWebSocketReceive = (PFN_WINHTTP_WEBSOCKET_RECEIVE)
        GetProcAddress(hWinHttp, "WinHttpWebSocketReceive");
    RETURN_LAST_ERROR_IF (sm_pfnWinHttpWebSocketReceive == nullptr);

    sm_pfnWinHttpWebSocketSend = (PFN_WINHTTP_WEBSOCKET_SEND)
        GetProcAddress(hWinHttp, "WinHttpWebSocketSend");
    RETURN_LAST_ERROR_IF (sm_pfnWinHttpWebSocketSend == nullptr);

    sm_pfnWinHttpWebSocketShutdown = (PFN_WINHTTP_WEBSOCKET_SHUTDOWN)
        GetProcAddress(hWinHttp, "WinHttpWebSocketShutdown");
    RETURN_LAST_ERROR_IF (sm_pfnWinHttpWebSocketShutdown == nullptr);

    return S_OK;
}


//static
VOID
WINHTTP_HELPER::GetFlagsFromBufferType(
    __in  WINHTTP_WEB_SOCKET_BUFFER_TYPE   BufferType,
    __out BOOL *                           pfUtf8Encoded,
    __out BOOL *                           pfFinalFragment,
    __out BOOL *                           pfClose
)
{
    *pfClose = FALSE;
    *pfFinalFragment = FALSE;
    *pfUtf8Encoded = FALSE;

    switch (BufferType)
    {
    case WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE:
        *pfUtf8Encoded = FALSE;
        *pfFinalFragment = TRUE;
        *pfClose = FALSE;

        break;

    case WINHTTP_WEB_SOCKET_BINARY_FRAGMENT_BUFFER_TYPE:
        *pfUtf8Encoded = FALSE;
        *pfFinalFragment = FALSE;
        *pfClose = FALSE;

        break;

    case WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE:
        *pfUtf8Encoded = TRUE;
        *pfFinalFragment = TRUE;
        *pfClose = FALSE;

        break;

    case WINHTTP_WEB_SOCKET_UTF8_FRAGMENT_BUFFER_TYPE:
        *pfUtf8Encoded = TRUE;
        *pfFinalFragment = FALSE;
        *pfClose = FALSE;

        break;

    case WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE:
        *pfUtf8Encoded = FALSE;
        *pfFinalFragment = FALSE;
        *pfClose = TRUE;

        break;
    }
}

//static
VOID
WINHTTP_HELPER::GetBufferTypeFromFlags(
    __in  BOOL                             fUtf8Encoded,
    __in  BOOL                             fFinalFragment,
    __in  BOOL                             fClose,
    __out WINHTTP_WEB_SOCKET_BUFFER_TYPE*  pBufferType
)
{
    if (fClose)
    {
        *pBufferType = WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE;
    }
    else
    if (fUtf8Encoded)
    {
        if (fFinalFragment)
        {
            *pBufferType = WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE;
        }
        else
        {
            *pBufferType = WINHTTP_WEB_SOCKET_UTF8_FRAGMENT_BUFFER_TYPE;
        }
    }
    else
    {
        if (fFinalFragment)
        {
            *pBufferType = WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE;
        }
        else
        {
            *pBufferType = WINHTTP_WEB_SOCKET_BINARY_FRAGMENT_BUFFER_TYPE;
        }
    }
}
