// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "hostfxroptions.h"
#include "hashtable.h"
#include "hashfn.h"
#include "aspnetcore_shim_config.h"
#include "iapplication.h"
#include "SRWSharedLock.h"

#define API_BUFFER_TOO_SMALL 0x80008098

typedef
HRESULT
(WINAPI * PFN_ASPNETCORE_CREATE_APPLICATION)(
    _In_  IHttpServer           *pServer,
    _In_  IHttpApplication      *pHttpApplication,
    _In_  APPLICATION_PARAMETER *pParameters,
    _In_  DWORD                  nParameters,
    _Out_ IAPPLICATION         **pApplication
    );

extern BOOL     g_fRecycleProcessCalled;

class APPLICATION_INFO
{
public:

    APPLICATION_INFO(_In_ IHttpServer &pServer) :
        m_pServer(pServer),
        m_cRefs(1),
        m_fValid(FALSE),
        m_pConfiguration(nullptr),
        m_pfnAspNetCoreCreateApplication(NULL)
    {
        InitializeSRWLock(&m_applicationLock);
    }

    PCWSTR
    QueryApplicationInfoKey()
    {
        return m_struInfoKey.QueryStr();
    }

    virtual
    ~APPLICATION_INFO();
    
    static 
    void
    StaticInitialize()
    {
        InitializeSRWLock(&s_requestHandlerLoadLock);
    }

    HRESULT
    Initialize(
        _In_ IHttpApplication    &pApplication
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

    BOOL
    IsValid()
    {
        return m_fValid;
    }

    VOID
    MarkValid()
    {
        m_fValid = TRUE;
    }

    ASPNETCORE_SHIM_CONFIG*
    QueryConfig()
    {
        return m_pConfiguration.get();
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
    HRESULT FindRequestHandlerAssembly(STRU& location);
    HRESULT FindNativeAssemblyFromGlobalLocation(PCWSTR libraryName, STRU* location);
    HRESULT FindNativeAssemblyFromHostfxr(HOSTFXR_OPTIONS* hostfxrOptions, PCWSTR libraryName, STRU* location);

    static DWORD WINAPI DoRecycleApplication(LPVOID lpParam);

    mutable LONG            m_cRefs;
    STRU                    m_struInfoKey;
    BOOL                    m_fValid;
    SRWLOCK                 m_applicationLock;
    IHttpServer            &m_pServer;
    PFN_ASPNETCORE_CREATE_APPLICATION      m_pfnAspNetCoreCreateApplication;
    
    std::unique_ptr<ASPNETCORE_SHIM_CONFIG> m_pConfiguration;
    std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER> m_pApplication;
    

    static const PCWSTR          s_pwzAspnetcoreInProcessRequestHandlerName;
    static const PCWSTR          s_pwzAspnetcoreOutOfProcessRequestHandlerName;

    static SRWLOCK      s_requestHandlerLoadLock;
    static bool         s_fAspnetcoreRHAssemblyLoaded;
    static bool         s_fAspnetcoreRHLoadedError;
    static HMODULE      s_hAspnetCoreRH;
    static PFN_ASPNETCORE_CREATE_APPLICATION  s_pfnAspNetCoreCreateApplication;
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
