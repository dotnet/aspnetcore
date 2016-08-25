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

Finished:

    return hr;
}