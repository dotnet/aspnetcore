// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "applicationinfo.h"
#include "multisz.h"
#include "exceptions.h"
#include <unordered_map>

//
// This class will manage the lifecycle of all Asp.Net Core applciation
// It should be global singleton.
// Should always call GetInstance to get the object instance
//


class APPLICATION_MANAGER
{
public:

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
    
    APPLICATION_MANAGER(HMODULE hModule, IHttpServer& pHttpServer) :
                            m_pApplicationInfoHash(NULL),
                            m_fDebugInitialize(FALSE),
                            m_pHttpServer(pHttpServer),
                            m_handlerResolver(hModule, pHttpServer)
    {
        InitializeSRWLock(&m_srwLock);
    }

private:

    std::unordered_map<std::wstring, std::shared_ptr<APPLICATION_INFO>>      m_pApplicationInfoHash;
    SRWLOCK                     m_srwLock {};
    BOOL                        m_fDebugInitialize;
    IHttpServer                &m_pHttpServer;
    HandlerResolver             m_handlerResolver;
};
