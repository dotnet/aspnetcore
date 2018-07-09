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
    GetInstance(
        VOID
    )
    {
        if ( sm_pApplicationManager == NULL )
        {
            sm_pApplicationManager = new APPLICATION_MANAGER();
        }

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
        _In_ IHttpServer*          pServer,
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
    //
    // we currently limit the size of m_pstrErrorInfo to 5000, be careful if you want to change its payload
    //
    APPLICATION_MANAGER() : m_pApplicationInfoHash(NULL),
                            m_hostingModel(HOSTING_UNKNOWN)
    {
        InitializeSRWLock(&m_srwLock);
    }

    APPLICATION_INFO_HASH      *m_pApplicationInfoHash;
    static APPLICATION_MANAGER *sm_pApplicationManager;
    SRWLOCK                     m_srwLock;
    APP_HOSTING_MODEL          m_hostingModel;
};
