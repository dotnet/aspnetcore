// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "forwarderconnection.h"
#include "exceptions.h"

FORWARDER_CONNECTION::FORWARDER_CONNECTION(
    VOID
) : m_cRefs (1),
    m_hConnection (nullptr)
{
}

HRESULT
FORWARDER_CONNECTION::Initialize(
    DWORD   dwPort
)
{
    RETURN_IF_FAILED(m_ConnectionKey.Initialize( dwPort ));
    m_hConnection = WinHttpConnect(g_hWinhttpSession,
                                   L"127.0.0.1",
                                   (USHORT) dwPort,
                                   0);
    RETURN_LAST_ERROR_IF_NULL(m_hConnection);
    //
    // Since WinHttp will not emit WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING
    // when closing WebSocket handle on Win8. Register callback at Connect level as a workaround
    //
    RETURN_LAST_ERROR_IF (WinHttpSetStatusCallback(m_hConnection,
                                 FORWARDING_HANDLER::OnWinHttpCompletion,
                                 WINHTTP_CALLBACK_FLAG_HANDLES,
                                 NULL) == WINHTTP_INVALID_STATUS_CALLBACK);
    return S_OK;
}
