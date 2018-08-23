// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "applicationinfo.h"
#include "multisz.h"
#include "exceptions.h"
#include <unordered_map>

#define DEFAULT_HASH_BUCKETS 17

//
// This class will manage the lifecycle of all Asp.Net Core applciation
// It should be global singleton.
// Should always call GetInstance to get the object instance
//


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
    Cleanup()
    {
        if(sm_pApplicationManager != NULL)
        {
            delete sm_pApplicationManager;
            sm_pApplicationManager = NULL;
        }
    }

    HRESULT
    GetOrCreateApplicationInfo(
        _In_ IHttpContext& pHttpContext,
        _Out_ std::shared_ptr<APPLICATION_INFO>&  ppApplicationInfo
    );

    HRESULT
    RecycleApplicationFromManager(
        _In_ LPCWSTR pszApplicationId
    );

    VOID
    ShutDown();

    static HRESULT StaticInitialize(HMODULE hModule, IHttpServer& pHttpServer)
    {
        assert(!sm_pApplicationManager);
        sm_pApplicationManager = new APPLICATION_MANAGER(hModule, pHttpServer);
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

    std::unordered_map<std::wstring, std::shared_ptr<APPLICATION_INFO>>      m_pApplicationInfoHash;
    static APPLICATION_MANAGER *sm_pApplicationManager;
    SRWLOCK                     m_srwLock {};
    BOOL                        m_fDebugInitialize;
    IHttpServer                &m_pHttpServer;
    HandlerResolver             m_handlerResolver;
};
