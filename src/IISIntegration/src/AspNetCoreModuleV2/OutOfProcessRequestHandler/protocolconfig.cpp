// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "protocolconfig.h"
#include "exceptions.h"

HRESULT
PROTOCOL_CONFIG::Initialize()
{
    m_fKeepAlive = TRUE;
    m_msTimeout = 120000;
    m_fPreserveHostHeader = TRUE;
    m_fReverseRewriteHeaders = FALSE;

    RETURN_IF_FAILED(m_strXForwardedForName.CopyW(L"X-Forwarded-For"));
    RETURN_IF_FAILED(m_strSslHeaderName.CopyW(L"X-Forwarded-Proto"));
    RETURN_IF_FAILED(m_strClientCertName.CopyW(L"MS-ASPNETCORE-CLIENTCERT"));

    m_fIncludePortInXForwardedFor = TRUE;
    m_dwMinResponseBuffer = 0; // no response buffering
    m_dwResponseBufferLimit = 4096*1024;
    m_dwMaxResponseHeaderSize = 65536;
    return S_OK;
}

VOID
PROTOCOL_CONFIG::OverrideConfig(
    REQUESTHANDLER_CONFIG *pAspNetCoreConfig
)
{
    m_msTimeout = pAspNetCoreConfig->QueryRequestTimeoutInMS();
}
