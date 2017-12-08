// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

//
// The key used for hash-table lookups, consists of the port on which the http process is created.
//
class APPLICATION_KEY
{
public:

    APPLICATION_KEY(
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
        const APPLICATION_KEY * key2
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

class APP_OFFLINE_HTM
{
public:
    APP_OFFLINE_HTM(LPCWSTR pszPath) : m_cRefs(1)
    {
        m_Path.Copy( pszPath );
    }

    VOID
    ReferenceAppOfflineHtm() const
    {
        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceAppOfflineHtm() const
    {
        if (InterlockedDecrement(&m_cRefs) == 0)
        {
            delete this;
        }
    }

    BOOL 
    Load(
        VOID
    )
    {
        BOOL            fResult = TRUE;
        LARGE_INTEGER   li = {0};
        CHAR           *pszBuff = NULL;
        HANDLE         handle = INVALID_HANDLE_VALUE;

        handle = CreateFile( m_Path.QueryStr(),
                                     GENERIC_READ,
                                     FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                                     NULL,
                                     OPEN_EXISTING,
                                     FILE_ATTRIBUTE_NORMAL,
                                     NULL );

        if( handle == INVALID_HANDLE_VALUE )
        {
            if ( GetLastError() == ERROR_FILE_NOT_FOUND )
            {
                fResult = FALSE;
            }

            // This Load() member function is supposed be called only when the change notification event of file creation or file modification happens.
            // If file is currenlty locked exclusively by other processes, we might get INVALID_HANDLE_VALUE even though the file exists. In that case, we should return TRUE here.
            goto Finished;
        }

        if(!GetFileSizeEx( handle, &li ))
        {
            goto Finished;
        }

        if( li.HighPart != 0 )
        {
            // > 4gb file size not supported
            // todo: log a warning at event log
            goto Finished;
        }

        DWORD bytesRead = 0;

        if(li.LowPart > 0)
        {
            pszBuff = new CHAR[ li.LowPart + 1 ];

            if( ReadFile( handle, pszBuff, li.LowPart, &bytesRead, NULL ) )
            {
                m_Contents.Copy( pszBuff, bytesRead );
            }
        }

Finished:
        if( handle != INVALID_HANDLE_VALUE )
        {
            CloseHandle(handle);
            handle = INVALID_HANDLE_VALUE;
        }

        if( pszBuff != NULL )
        {
            delete[] pszBuff;
            pszBuff = NULL;
        }

        return fResult;
    }

    mutable LONG        m_cRefs;
    STRA                m_Contents;
    STRU                m_Path;
};

class APPLICATION_MANAGER;

class APPLICATION
{
public:

    APPLICATION() : m_pApplicationManager(NULL), m_cRefs(1),
        m_fAppOfflineFound(FALSE), m_pAppOfflineHtm(NULL),
        m_pFileWatcherEntry(NULL), m_pConfiguration(NULL)
    {
        InitializeSRWLock(&m_srwLock);
    }

    APPLICATION_KEY *
    QueryApplicationKey()
    {
        return &m_applicationKey;
    }

    virtual
    ~APPLICATION();

    virtual
    HRESULT
    Initialize(
        _In_ APPLICATION_MANAGER *pApplicationManager,
        _In_ ASPNETCORE_CONFIG   *pConfiguration) = 0;

    VOID
    ReferenceApplication() const
    {
        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceApplication() const
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

    virtual
    VOID
    OnAppOfflineHandleChange() = 0;

    VOID
    UpdateAppOfflineFileHandle();

    HRESULT
    StartMonitoringAppOffline();

    ASPNETCORE_CONFIG*
    QueryConfig()
    {
        return m_pConfiguration;
    }

    virtual
    REQUEST_NOTIFICATION_STATUS
    ExecuteRequest(
        _In_ IHttpContext* pHttpContext
    ) = 0;

protected:

    mutable LONG            m_cRefs;
    APPLICATION_KEY         m_applicationKey;
    APPLICATION_MANAGER    *m_pApplicationManager;
    BOOL                    m_fAppOfflineFound;
    APP_OFFLINE_HTM        *m_pAppOfflineHtm;
    FILE_WATCHER_ENTRY     *m_pFileWatcherEntry;
    ASPNETCORE_CONFIG      *m_pConfiguration;
    SRWLOCK                 m_srwLock;

};

class APPLICATION_HASH :
    public HASH_TABLE<APPLICATION, APPLICATION_KEY *>
{

public:

    APPLICATION_HASH()
    {}

    APPLICATION_KEY *
    ExtractKey(
        APPLICATION *pApplication
    )
    {
        return pApplication->QueryApplicationKey();
    }

    DWORD
    CalcKeyHash(
        APPLICATION_KEY *key
    )
    {
        return key->CalcKeyHash();
    }

    BOOL
    EqualKeys(
        APPLICATION_KEY *key1,
        APPLICATION_KEY *key2
    )
    {
        return key1->GetIsEqual(key2);
    }

    VOID
    ReferenceRecord(
        APPLICATION *pApplication
    )
    {
        pApplication->ReferenceApplication();
    }

    VOID
    DereferenceRecord(
        APPLICATION *pApplication
    )
    {
        pApplication->DereferenceApplication();
    }

private:

    APPLICATION_HASH(const APPLICATION_HASH &);
    void operator=(const APPLICATION_HASH &);
};