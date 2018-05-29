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

//
// The key used for hash-table lookups, consists of the port on which the http process is created.
//
class APPLICATION_INFO_KEY
{
public:

    APPLICATION_INFO_KEY(
        VOID
    ) : INLINE_STRU_INIT(m_struKey)
    {
    }

    HRESULT
        Initialize(
            _In_ LPCWSTR pszKey
        )
    {
        return m_struKey.Copy(pszKey);
    }

    BOOL
        GetIsEqual(
            const APPLICATION_INFO_KEY * key2
        ) const
    {
        return m_struKey.Equals(key2->m_struKey);
    }

    DWORD CalcKeyHash() const
    {
        return Hash(m_struKey.QueryStr());
    }

private:

    INLINE_STRU(m_struKey, 1024);
};


class APPLICATION_INFO
{
public:

    APPLICATION_INFO(IHttpServer *pServer) :
        m_pServer(pServer),
        m_cRefs(1), m_fAppOfflineFound(FALSE),
        m_pAppOfflineHtm(NULL), m_pFileWatcherEntry(NULL),
        m_pConfiguration(NULL),
        m_pfnAspNetCoreCreateApplication(NULL)
    {
        InitializeSRWLock(&m_srwLock);
    }

    APPLICATION_INFO_KEY *
    QueryApplicationInfoKey()
    {
        return &m_applicationInfoKey;
    }

    virtual
    ~APPLICATION_INFO();

    HRESULT
    Initialize(
        _In_ ASPNETCORE_SHIM_CONFIG   *pConfiguration,
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
    APPLICATION_INFO_KEY    m_applicationInfoKey;
    BOOL                    m_fAppOfflineFound;
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
    public HASH_TABLE<APPLICATION_INFO, APPLICATION_INFO_KEY *>
{

public:

    APPLICATION_INFO_HASH()
    {}

    APPLICATION_INFO_KEY *
    ExtractKey(
        APPLICATION_INFO *pApplicationInfo
    )
    {
        return pApplicationInfo->QueryApplicationInfoKey();
    }

    DWORD
    CalcKeyHash(
        APPLICATION_INFO_KEY *key
    )
    {
        return key->CalcKeyHash();
    }

    BOOL
    EqualKeys(
        APPLICATION_INFO_KEY *key1,
        APPLICATION_INFO_KEY *key2
    )
    {
        return key1->GetIsEqual(key2);
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
