// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#define API_BUFFER_TOO_SMALL 0x80008098

typedef
HRESULT
(WINAPI * PFN_ASPNETCORE_CREATE_APPLICATION)(
    _In_  IHttpServer        *pServer,
    _In_  ASPNETCORE_CONFIG  *pConfig,
    _Out_ APPLICATION       **pApplication
    );

typedef
HRESULT
(WINAPI * PFN_ASPNETCORE_CREATE_REQUEST_HANDLER)(
    _In_  IHttpContext       *pHttpContext,
    _In_  HTTP_MODULE_ID     *pModuleId,
    _In_  APPLICATION        *pApplication,
    _Out_ REQUEST_HANDLER   **pRequestHandler
    );
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
        m_pfnAspNetCoreCreateApplication(NULL),
        m_pfnAspNetCoreCreateRequestHandler(NULL)
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
        _In_ ASPNETCORE_CONFIG   *pConfiguration,
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

    ASPNETCORE_CONFIG*
    QueryConfig()
    {
        return m_pConfiguration;
    }

    APPLICATION*
    QueryApplication()
    {
        return m_pApplication;
    }

    HRESULT
    EnsureApplicationCreated();

    PFN_ASPNETCORE_CREATE_REQUEST_HANDLER
    QueryCreateRequestHandler()
    {
        return m_pfnAspNetCoreCreateRequestHandler;
    }

private:
    HRESULT FindRequestHandlerAssembly();
    HRESULT FindNativeAssemblyFromGlobalLocation(STRU* struFilename);
    HRESULT FindNativeAssemblyFromHostfxr(STRU* struFilename);

    mutable LONG            m_cRefs;
    APPLICATION_INFO_KEY    m_applicationInfoKey;
    BOOL                    m_fAppOfflineFound;
    APP_OFFLINE_HTM        *m_pAppOfflineHtm;
    FILE_WATCHER_ENTRY     *m_pFileWatcherEntry;
    ASPNETCORE_CONFIG      *m_pConfiguration;
    APPLICATION            *m_pApplication;
    SRWLOCK                 m_srwLock;
    IHttpServer            *m_pServer;
    PFN_ASPNETCORE_CREATE_APPLICATION      m_pfnAspNetCoreCreateApplication;
    PFN_ASPNETCORE_CREATE_REQUEST_HANDLER  m_pfnAspNetCoreCreateRequestHandler;
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

private:

    APPLICATION_INFO_HASH(const APPLICATION_INFO_HASH &);
    void operator=(const APPLICATION_INFO_HASH &);
};
