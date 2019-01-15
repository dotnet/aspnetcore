// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

FORWARDER_CONNECTION::FORWARDER_CONNECTION(
    VOID
) : m_cRefs (1),
    m_hConnection (NULL)
{    
}

HRESULT
FORWARDER_CONNECTION::Initialize(
    DWORD   dwPort
)
{
    HRESULT hr = S_OK;

    hr = m_ConnectionKey.Initialize( dwPort );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

    m_hConnection = WinHttpConnect(FORWARDING_HANDLER::sm_hSession,
                                   L"127.0.0.1",
                                   (USHORT) dwPort,
                                   0);
    if (m_hConnection == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    //
    // Since WinHttp will not emit WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING
    // when closing WebSocket handle on Win8. Register callback at Connect level as a workaround
    //
    if (WinHttpSetStatusCallback(m_hConnection,
                                 FORWARDING_HANDLER::OnWinHttpCompletion,
                                 WINHTTP_CALLBACK_FLAG_HANDLES,
                                 NULL) == WINHTTP_INVALID_STATUS_CALLBACK)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

Finished:

    return hr;
}