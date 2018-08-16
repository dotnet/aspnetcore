// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "applicationinfo.h"
#include "multisz.h"
#include "exceptions.h"

#define DEFAULT_HASH_BUCKETS 17

//
// This class will manage the lifecycle of all Asp.Net Core applciation
// It should be global singleton.
// Should always call GetInstance to get the object instance
//

struct CONFIG_CHANGE_CONTEXT
{
    PCWSTR   pstrPath;
    MULTISZ  MultiSz;
};

class APPLICATION_MANAGER
{
public:

    static
    APPLICATION_MANAGER*
    GetInstance()
    {
        assert(sm_pApplicationManager);
        return sm_pApplicationManager;
    }

    static
    VOID
    Cleanup(
        VOID
    )
    {
        if(sm_pApplicationManager != NULL)
        {
            delete sm_pApplicationManager;
            sm_pApplicationManager = NULL;
        }
    }

    static
    BOOL
    FindConfigChangedApplication(
        _In_ APPLICATION_INFO *     pEntry,
        _In_ PVOID                  pvContext
    );

    static
    VOID
    ShutdownApplication(
        _In_ APPLICATION_INFO *     pEntry,
        _In_ PVOID                  pvContext
    );

    HRESULT
    GetOrCreateApplicationInfo(
        _In_ IHttpContext*         pHttpContext,
        _Out_ APPLICATION_INFO **  ppApplicationInfo
    );

    HRESULT
    RecycleApplicationFromManager(
        _In_ LPCWSTR pszApplicationId
    );

    VOID
    ShutDown();

    ~APPLICATION_MANAGER()
    {

        if(m_pApplicationInfoHash != NULL)
        {
            m_pApplicationInfoHash->Clear();
            delete m_pApplicationInfoHash;
            m_pApplicationInfoHash = NULL;
        }
    }

    static HRESULT StaticInitialize(HMODULE hModule, IHttpServer& pHttpServer)
    {
        assert(!sm_pApplicationManager);
        sm_pApplicationManager = new APPLICATION_MANAGER(hModule, pHttpServer);
        RETURN_IF_FAILED(sm_pApplicationManager->Initialize());

        APPLICATION_INFO::StaticInitialize();
        return S_OK;
    }

    HRESULT Initialize()
    {
        if(m_pApplicationInfoHash == NULL)
        {
            try
            {
                m_pApplicationInfoHash = new APPLICATION_INFO_HASH();
            }
            CATCH_RETURN();
            RETURN_IF_FAILED(m_pApplicationInfoHash->Initialize(DEFAULT_HASH_BUCKETS));
        }
        return S_OK;
    }

private:
    APPLICATION_MANAGER(HMODULE hModule, IHttpServer& pHttpServer) :
                            m_pApplicationInfoHash(NULL),
                            m_fDebugInitialize(FALSE),
                            m_pHttpServer(pHttpServer),
                            m_handlerResolver(hModule, pHttpServer)
    {
        InitializeSRWLock(&m_srwLock);
    }

    APPLICATION_INFO_HASH      *m_pApplicationInfoHash;
    static APPLICATION_MANAGER *sm_pApplicationManager;
    SRWLOCK                     m_srwLock {};
    BOOL                        m_fDebugInitialize;
    IHttpServer                &m_pHttpServer;
    HandlerResolver             m_handlerResolver;
};
