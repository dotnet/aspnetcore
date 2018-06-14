// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "precomp.hxx"

#include "hostfxroptions.h"
#include "appoffline.h"
#include "filewatcher.h"
#include "hashtable.h"
#include "hashfn.h"
#include "aspnetcore_shim_config.h"
#include "iapplication.h"

#define API_BUFFER_TOO_SMALL 0x80008098

typedef
HRESULT
(WINAPI * PFN_ASPNETCORE_CREATE_APPLICATION)(
    _In_  IHttpServer    *pServer,
    _In_  IHttpContext   *pHttpContext,
    _In_  PCWSTR          pwzExeLocation, // TODO remove both pwzExeLocation and pHttpContext from this api
    _Out_ IAPPLICATION  **pApplication
    );

extern BOOL     g_fRecycleProcessCalled;
extern PFN_ASPNETCORE_CREATE_APPLICATION      g_pfnAspNetCoreCreateApplication;

class APPLICATION_INFO
{
public:

    APPLICATION_INFO() :
        m_pServer(NULL),
        m_cRefs(1),
        m_fAppOfflineFound(FALSE),
        m_fAllowStart(FALSE),
        m_pAppOfflineHtm(NULL),
        m_pFileWatcherEntry(NULL),
        m_pConfiguration(NULL),
        m_pfnAspNetCoreCreateApplication(NULL)
    {
        InitializeSRWLock(&m_srwLock);
    }

    PCWSTR
    QueryApplicationInfoKey()
    {
        return m_struInfoKey.QueryStr();
    }

    virtual
    ~APPLICATION_INFO();

    HRESULT
    Initialize(
        _In_ IHttpServer         *pServer,
        _In_ IHttpApplication    *pApplication,
        _In_ FILE_WATCHER        *pFileWatcher
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

    APP_OFFLINE_HTM* QueryAppOfflineHtm()
    {
        return m_pAppOfflineHtm;
    }

    BOOL
    AppOfflineFound()
    {
        return m_fAppOfflineFound;
    }

    BOOL QueryAllowStart()
    {
        return m_fAllowStart;
    }

    VOID
    UpdateAllowStartStatus(BOOL fAllowed)
    {
        // no lock, as no expectation for concurrent accesses
        m_fAllowStart = fAllowed;
    }

    VOID
    UpdateAppOfflineFileHandle();

    HRESULT
    StartMonitoringAppOffline();

    ASPNETCORE_SHIM_CONFIG*
    QueryConfig()
    {
        return m_pConfiguration;
    }


    //
    // ExtractApplication will increase the reference counter of the application
    // Caller is responsible for dereference the application.
    // Otherwise memory leak
    //
    VOID
    ExtractApplication(IAPPLICATION** ppApplication)
    {
        AcquireSRWLockShared(&m_srwLock);
        if (m_pApplication != NULL)
        {
            m_pApplication->ReferenceApplication();
        }
        *ppApplication = m_pApplication;
        ReleaseSRWLockShared(&m_srwLock);
    }

    VOID
    RecycleApplication();

    VOID
    ShutDownApplication();

    HRESULT
    EnsureApplicationCreated(
        IHttpContext *pHttpContext
    );

private:
    HRESULT FindRequestHandlerAssembly(STRU& location);
    HRESULT FindNativeAssemblyFromGlobalLocation(PCWSTR libraryName, STRU* location);
    HRESULT FindNativeAssemblyFromHostfxr(HOSTFXR_OPTIONS* hostfxrOptions, PCWSTR libraryName, STRU* location);

    static VOID DoRecycleApplication(LPVOID lpParam);

    mutable LONG            m_cRefs;
    STRU                    m_struInfoKey;
    BOOL                    m_fAppOfflineFound;
    BOOL                    m_fAllowStart; // Flag indicates whether there is (configuration) error blocking application from starting
    APP_OFFLINE_HTM        *m_pAppOfflineHtm;
    FILE_WATCHER_ENTRY     *m_pFileWatcherEntry;
    ASPNETCORE_SHIM_CONFIG *m_pConfiguration;
    IAPPLICATION           *m_pApplication;
    SRWLOCK                 m_srwLock;
    IHttpServer            *m_pServer;
    PFN_ASPNETCORE_CREATE_APPLICATION      m_pfnAspNetCoreCreateApplication;

    static const PCWSTR          s_pwzAspnetcoreInProcessRequestHandlerName;
    static const PCWSTR          s_pwzAspnetcoreOutOfProcessRequestHandlerName;
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
