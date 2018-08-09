// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "hostfxroptions.h"
#include "hashtable.h"
#include "hashfn.h"
#include "aspnetcore_shim_config.h"
#include "iapplication.h"
#include "SRWSharedLock.h"
#include "HandlerResolver.h"

#define API_BUFFER_TOO_SMALL 0x80008098

extern BOOL     g_fRecycleProcessCalled;

class APPLICATION_INFO
{
public:

    APPLICATION_INFO(IHttpServer &pServer) :
        m_pServer(pServer),
        m_cRefs(1),
        m_handlerResolver(nullptr)
    {
        InitializeSRWLock(&m_applicationLock);
    }

    PCWSTR
    QueryApplicationInfoKey()
    {
        return m_struInfoKey.QueryStr();
    }

    STRU*
    QueryConfigPath()
    {
        return &m_struConfigPath;
    }

    virtual
    ~APPLICATION_INFO();

    static
    void
    StaticInitialize()
    {
    }

    HRESULT
    Initialize(
        IHttpApplication    &pApplication,
        HandlerResolver     *pHandlerResolver
    );

    VOID
    ReferenceApplicationInfo() const
    {
        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceApplicationInfo() const
    {
        if (InterlockedDecrement(&m_cRefs) == 0)
        {
            delete this;
        }
    }

    VOID
    RecycleApplication();

    VOID
    ShutDownApplication();

    HRESULT
    GetOrCreateApplication(
        IHttpContext *pHttpContext,
        std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER>& pApplication
    );

private:

    static DWORD WINAPI DoRecycleApplication(LPVOID lpParam);

    mutable LONG            m_cRefs;
    STRU                    m_struConfigPath;
    STRU                    m_struInfoKey;
    SRWLOCK                 m_applicationLock;
    IHttpServer            &m_pServer;
    HandlerResolver        *m_handlerResolver;

    std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER> m_pApplication;
};

class APPLICATION_INFO_HASH :
    public HASH_TABLE<APPLICATION_INFO, PCWSTR>
{

public:

    APPLICATION_INFO_HASH()
    {}

    PCWSTR
    ExtractKey(
        APPLICATION_INFO *pApplicationInfo
    )
    {
        return pApplicationInfo->QueryApplicationInfoKey();
    }

    DWORD
    CalcKeyHash(
        PCWSTR  pszApplicationId
    )
    {
        return HashStringNoCase(pszApplicationId);
    }

    BOOL
    EqualKeys(
        PCWSTR pszKey1,
        PCWSTR pszKey2
    )
    {
        return CompareStringOrdinal(pszKey1,
            -1,
            pszKey2,
            -1,
            TRUE) == CSTR_EQUAL;
    }

    VOID
    ReferenceRecord(
        APPLICATION_INFO *pApplicationInfo
    )
    {
        pApplicationInfo->ReferenceApplicationInfo();
    }

    VOID
    DereferenceRecord(
        APPLICATION_INFO *pApplicationInfo
    )
    {
        pApplicationInfo->DereferenceApplicationInfo();
    }

    static
    VOID
    ReferenceCopyToTable(
        APPLICATION_INFO *        pEntry,
        PVOID                     pvData
    )
    {
        APPLICATION_INFO_HASH *pHash = static_cast<APPLICATION_INFO_HASH *>(pvData);
        DBG_ASSERT(pHash);
        pHash->InsertRecord(pEntry);
    }

private:

    APPLICATION_INFO_HASH(const APPLICATION_INFO_HASH &);
    void operator=(const APPLICATION_INFO_HASH &);
};
