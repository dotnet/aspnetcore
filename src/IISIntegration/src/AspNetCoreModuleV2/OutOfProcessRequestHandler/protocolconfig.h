// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class PROTOCOL_CONFIG
{
 public:

    PROTOCOL_CONFIG()
    {
    }

    HRESULT
    Initialize();

    VOID
    OverrideConfig(
        REQUESTHANDLER_CONFIG *pAspNetCoreConfig
    );

    BOOL
    QueryDoKeepAlive() const
    {
        return m_fKeepAlive;
    }

    DWORD
    QueryTimeout() const
    {
        return m_msTimeout;
    }

    BOOL
    QueryPreserveHostHeader() const
    {
        return m_fPreserveHostHeader;
    }

    BOOL
    QueryReverseRewriteHeaders() const
    {
        return m_fReverseRewriteHeaders;
    }

    const STRA *
    QueryXForwardedForName() const
    {
        return &m_strXForwardedForName;
    }

    BOOL
    QueryIncludePortInXForwardedFor() const
    {
        return m_fIncludePortInXForwardedFor;
    }

    DWORD
    QueryMinResponseBuffer() const
    {
        return m_dwMinResponseBuffer;
    }

    DWORD
    QueryResponseBufferLimit() const
    {
        return m_dwResponseBufferLimit;
    }

    DWORD
    QueryMaxResponseHeaderSize() const
    {
        return m_dwMaxResponseHeaderSize;
    }

    const STRA*
    QuerySslHeaderName() const
    {
        return &m_strSslHeaderName;
    }

    const STRA *
    QueryClientCertName() const
    {
        return &m_strClientCertName;
    }

 private:
    
    BOOL            m_fKeepAlive;
    BOOL            m_fPreserveHostHeader;
    BOOL            m_fReverseRewriteHeaders;
    BOOL            m_fIncludePortInXForwardedFor;

    DWORD           m_msTimeout;
    DWORD           m_dwMinResponseBuffer;
    DWORD           m_dwResponseBufferLimit;
    DWORD           m_dwMaxResponseHeaderSize;

    STRA            m_strXForwardedForName;
    STRA            m_strSslHeaderName;
    STRA            m_strClientCertName;
};
