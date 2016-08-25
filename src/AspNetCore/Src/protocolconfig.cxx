// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

HRESULT
PROTOCOL_CONFIG::Initialize()
{
    HRESULT hr;
    STRU strTemp;

    m_fKeepAlive = TRUE;
    m_msTimeout = 120000;
    m_fPreserveHostHeader = TRUE;
    m_fReverseRewriteHeaders = FALSE;

    if (FAILED(hr = m_strXForwardedForName.CopyW(L"X-Forwarded-For")))
    {
        goto Finished;
    }

    if (FAILED(hr = m_strSslHeaderName.CopyW(L"X-Forwarded-Proto")))
    {
        goto Finished;
    }

    if (FAILED(hr = m_strClientCertName.CopyW(L"MS-ASPNETCORE-CLIENTCERT")))
    {
        goto Finished;
    }

    m_fIncludePortInXForwardedFor = TRUE;
    m_dwMinResponseBuffer = 0; // no response buffering
    m_dwResponseBufferLimit = 4096*1024;
    m_dwMaxResponseHeaderSize = 65536;

Finished:

    return hr;
}

VOID
PROTOCOL_CONFIG::OverrideConfig(
    ASPNETCORE_CONFIG *pAspNetCoreConfig
)
{
    m_msTimeout = pAspNetCoreConfig->QueryRequestTimeoutInMS();
}