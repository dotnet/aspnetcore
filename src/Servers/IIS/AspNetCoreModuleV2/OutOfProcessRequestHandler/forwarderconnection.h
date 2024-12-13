// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

//
// The key used for hash-table lookups, consists of the port on which the http process is created.
//
class FORWARDER_CONNECTION_KEY
{
public:

    FORWARDER_CONNECTION_KEY(
        VOID
    )
    {
    }

    HRESULT
    Initialize(
        _In_ DWORD  dwPort
    )
    {
        m_dwPort = dwPort;
        return S_OK;
    }

    BOOL
    GetIsEqual(
        const FORWARDER_CONNECTION_KEY * key2
    ) const
    {
        return m_dwPort == key2->m_dwPort;
    }

    DWORD CalcKeyHash() const
    {
        // TODO: Review hash distribution.
        return Hash(m_dwPort);
    }

private:

    DWORD      m_dwPort;
};

class FORWARDER_CONNECTION
{
public:

    FORWARDER_CONNECTION(
        VOID
    );

    HRESULT
    Initialize(
        DWORD   dwPort
    );

    HINTERNET
    QueryHandle() const
    {
        return m_hConnection;
    }

    VOID
    ReferenceForwarderConnection() const
    {
        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceForwarderConnection() const
    {
        if (InterlockedDecrement(&m_cRefs) == 0)
        {
            delete this;
        }
    }

    FORWARDER_CONNECTION_KEY *
    QueryConnectionKey()
    {
        return &m_ConnectionKey;
    }

private:

    ~FORWARDER_CONNECTION()
    {
        if (m_hConnection != nullptr)
        {
            WinHttpCloseHandle(m_hConnection);
            m_hConnection = nullptr;
        }
    }

    mutable LONG                m_cRefs;
    FORWARDER_CONNECTION_KEY    m_ConnectionKey;
    HINTERNET                   m_hConnection;
};

class FORWARDER_CONNECTION_HASH :
    public HASH_TABLE<FORWARDER_CONNECTION, FORWARDER_CONNECTION_KEY *>
{

public:

    FORWARDER_CONNECTION_HASH()
    {}

    FORWARDER_CONNECTION_KEY *
    ExtractKey(
        FORWARDER_CONNECTION *pConnection
    )
    {
        return pConnection->QueryConnectionKey();
    }

    DWORD
    CalcKeyHash(
        FORWARDER_CONNECTION_KEY *key
    )
    {
        return key->CalcKeyHash();
    }

    BOOL
    EqualKeys(
        FORWARDER_CONNECTION_KEY *key1,
        FORWARDER_CONNECTION_KEY *key2
    )
    {
        return key1->GetIsEqual(key2);
    }

    VOID
    ReferenceRecord(
        FORWARDER_CONNECTION *pConnection
    )
    {
        pConnection->ReferenceForwarderConnection();
    }

    VOID
    DereferenceRecord(
        FORWARDER_CONNECTION *pConnection
    )
    {
        pConnection->DereferenceForwarderConnection();
    }

private:

    FORWARDER_CONNECTION_HASH(const FORWARDER_CONNECTION_HASH &);
    void operator=(const FORWARDER_CONNECTION_HASH &);
};