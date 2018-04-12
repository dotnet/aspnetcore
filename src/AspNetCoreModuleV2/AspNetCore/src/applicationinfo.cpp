// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

APPLICATION_INFO::~APPLICATION_INFO()
{
    if (m_pAppOfflineHtm != NULL)
    {
        m_pAppOfflineHtm->DereferenceAppOfflineHtm();
        m_pAppOfflineHtm = NULL;
    }

    if (m_pFileWatcherEntry != NULL)
    {
        // Mark the entry as invalid,
        // StopMonitor will close the file handle and trigger a FCN
        // the entry will delete itself when processing this FCN 
        m_pFileWatcherEntry->MarkEntryInValid();
        m_pFileWatcherEntry->StopMonitor();
        m_pFileWatcherEntry->DereferenceFileWatcherEntry();
        m_pFileWatcherEntry = NULL;
    }

    if (m_pApplication != NULL)
    {
        // shutdown the application
        m_pApplication->ShutDown();
        m_pApplication->DereferenceApplication();
        m_pApplication = NULL;
    }

    // configuration should be dereferenced after application shutdown
    // since the former will use it during shutdown
    if (m_pConfiguration != NULL)
    {
        // Need to dereference the configuration instance
        m_pConfiguration->DereferenceConfiguration();
        m_pConfiguration = NULL;
    }
}

HRESULT
APPLICATION_INFO::Initialize(
    _In_ ASPNETCORE_CONFIG   *pConfiguration,
    _In_ FILE_WATCHER        *pFileWatcher
)
{
    HRESULT hr = S_OK;

    DBG_ASSERT(pConfiguration);
    DBG_ASSERT(pFileWatcher);

    m_pConfiguration = pConfiguration;

    // reference the configuration instance to prevent it will be not release
    // earlier in case of configuration change and shutdown
    m_pConfiguration->ReferenceConfiguration();

    hr = m_applicationInfoKey.Initialize(pConfiguration->QueryConfigPath()->QueryStr());
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (m_pFileWatcherEntry == NULL)
    {
        m_pFileWatcherEntry = new FILE_WATCHER_ENTRY(pFileWatcher);
        if (m_pFileWatcherEntry == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }
    }

    UpdateAppOfflineFileHandle();

Finished:
    return hr;
}

HRESULT
APPLICATION_INFO::StartMonitoringAppOffline()
{
    HRESULT hr = S_OK;
    if (m_pFileWatcherEntry != NULL)
    {
        hr = m_pFileWatcherEntry->Create(m_pConfiguration->QueryApplicationPhysicalPath()->QueryStr(), L"app_offline.htm", this, NULL);
    }
    return hr;
}

//
// Called by the file watcher when the app_offline.htm's file status has been changed.
// If it finds it, we will call recycle on the application.
//
VOID
APPLICATION_INFO::UpdateAppOfflineFileHandle()
{
    STRU strFilePath;
    UTILITY::ConvertPathToFullPath(L".\\app_offline.htm",
        m_pConfiguration->QueryApplicationPhysicalPath()->QueryStr(),
        &strFilePath);
    APP_OFFLINE_HTM *pOldAppOfflineHtm = NULL;
    APP_OFFLINE_HTM *pNewAppOfflineHtm = NULL;

    ReferenceApplicationInfo();

    if (INVALID_FILE_ATTRIBUTES == GetFileAttributes(strFilePath.QueryStr()) &&
        GetLastError() == ERROR_FILE_NOT_FOUND)
    {
        // Check if app offline was originally present.
        // if it was, log that app_offline has been dropped.
        if (m_fAppOfflineFound)
        {
            STACK_STRU(strEventMsg, 256);
            if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_REMOVED_MSG)))
            {
                UTILITY::LogEvent(g_hEventLog,
                    EVENTLOG_INFORMATION_TYPE,
                    ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_REMOVED,
                    strEventMsg.QueryStr());
            }
        }

        m_fAppOfflineFound = FALSE;
    }
    else
    {
        pNewAppOfflineHtm = new APP_OFFLINE_HTM(strFilePath.QueryStr());

        if (pNewAppOfflineHtm != NULL)
        {
            if (pNewAppOfflineHtm->Load())
            {
                //
                // loaded the new app_offline.htm
                //
                pOldAppOfflineHtm = (APP_OFFLINE_HTM *)InterlockedExchangePointer((VOID**)&m_pAppOfflineHtm, pNewAppOfflineHtm);

                if (pOldAppOfflineHtm != NULL)
                {
                    pOldAppOfflineHtm->DereferenceAppOfflineHtm();
                    pOldAppOfflineHtm = NULL;
                }
            }
            else
            {
                // ignored the new app_offline file because the file does not exist.
                pNewAppOfflineHtm->DereferenceAppOfflineHtm();
                pNewAppOfflineHtm = NULL;
            }
        }

        m_fAppOfflineFound = TRUE;

        // recycle the application
        if (m_pApplication != NULL)
        {
            STACK_STRU(strEventMsg, 256);
            if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_MSG,
                m_pApplication->QueryConfig()->QueryApplicationPath()->QueryStr())))
            {
                UTILITY::LogEvent(g_hEventLog,
                    EVENTLOG_INFORMATION_TYPE,
                    ASPNETCORE_EVENT_RECYCLE_APPOFFLINE,
                    strEventMsg.QueryStr());
            }

            RecycleApplication();
        }
    }

    DereferenceApplicationInfo();
}

HRESULT
APPLICATION_INFO::EnsureApplicationCreated()
{
    HRESULT             hr = S_OK;
    BOOL                fLocked = FALSE;
    APPLICATION*        pApplication = NULL;
    STACK_STRU(struFileName, 300);  // >MAX_PATH
    STRU                struHostFxrDllLocation;

    if (m_pApplication != NULL)
    {
        goto Finished;
    }

    if (m_pApplication == NULL)
    {
        AcquireSRWLockExclusive(&m_srwLock);
        fLocked = TRUE;
        if (m_pApplication != NULL)
        {
            goto Finished;
        }

        //
        // in case of app offline, we don't want to create a new application now
        //
        if (!m_fAppOfflineFound)
        {

            // Move the request handler check inside of the lock
            // such that only one request finds and loads it.
            // FindRequestHandlerAssembly obtains a global lock, but after releasing the lock,
            // there is a period where we could call

            hr = FindRequestHandlerAssembly();
            if (FAILED(hr))
            {
                goto Finished;
            }

            if (m_pfnAspNetCoreCreateApplication == NULL)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_FUNCTION);
                goto Finished;
            }

            hr = m_pfnAspNetCoreCreateApplication(m_pServer, m_pConfiguration, &pApplication);
            if (FAILED(hr))
            {
                goto Finished;
            }
            m_pApplication = pApplication;
        }
    }

Finished:
    if (fLocked)
    {
        ReleaseSRWLockExclusive(&m_srwLock);
    }
    return hr;
}

HRESULT
APPLICATION_INFO::FindRequestHandlerAssembly()
{
    HRESULT             hr = S_OK;
    BOOL                fLocked = FALSE;
    STACK_STRU(struFileName, 256);

    if (g_fAspnetcoreRHLoadedError)
    {
        hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
        goto Finished;
    }
    else if (!g_fAspnetcoreRHAssemblyLoaded)
    {
        AcquireSRWLockExclusive(&g_srwLock);
        fLocked = TRUE;
        if (g_fAspnetcoreRHLoadedError)
        {
            hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
            goto Finished;
        }
        if (g_fAspnetcoreRHAssemblyLoaded)
        {
            goto Finished;
        }

        if (m_pConfiguration->QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
        {
            if (FAILED(hr = FindNativeAssemblyFromHostfxr(&struFileName)))
            {
                STACK_STRU(strEventMsg, 256);
                if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                    ASPNETCORE_EVENT_INPROCESS_RH_MISSING_MSG)))
                {
                    UTILITY::LogEvent(g_hEventLog,
                        EVENTLOG_INFORMATION_TYPE,
                        ASPNETCORE_EVENT_INPROCESS_RH_MISSING,
                        strEventMsg.QueryStr());
                }

                goto Finished;
            }
        }
        else
        {
            if (FAILED(hr = FindNativeAssemblyFromGlobalLocation(&struFileName)))
            {
                STACK_STRU(strEventMsg, 256);
                if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                    ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG)))
                {
                    UTILITY::LogEvent(g_hEventLog,
                        EVENTLOG_INFORMATION_TYPE,
                        ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING,
                        strEventMsg.QueryStr());
                }

                goto Finished;
            }
        }

        g_hAspnetCoreRH = LoadLibraryW(struFileName.QueryStr());
        if (g_hAspnetCoreRH == NULL)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }

        g_pfnAspNetCoreCreateApplication = (PFN_ASPNETCORE_CREATE_APPLICATION)
            GetProcAddress(g_hAspnetCoreRH, "CreateApplication");
        if (g_pfnAspNetCoreCreateApplication == NULL)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }

        g_pfnAspNetCoreCreateRequestHandler = (PFN_ASPNETCORE_CREATE_REQUEST_HANDLER)
            GetProcAddress(g_hAspnetCoreRH, "CreateRequestHandler");
        if (g_pfnAspNetCoreCreateRequestHandler == NULL)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }
        g_fAspnetcoreRHAssemblyLoaded = TRUE;
    }

Finished:
    //
    // Question: we remember the load failure so that we will not try again.
    // User needs to check whether the fuction pointer is NULL 
    //
    m_pfnAspNetCoreCreateApplication = g_pfnAspNetCoreCreateApplication;
    m_pfnAspNetCoreCreateRequestHandler = g_pfnAspNetCoreCreateRequestHandler;
    if (!g_fAspnetcoreRHLoadedError && FAILED(hr))
    {
        g_fAspnetcoreRHLoadedError = TRUE;
    }

    if (fLocked)
    {
        ReleaseSRWLockExclusive(&g_srwLock);
    }
    return hr;
}

HRESULT
APPLICATION_INFO::FindNativeAssemblyFromGlobalLocation(STRU* struFilename)
{
    HRESULT hr = S_OK;
    DWORD dwSize = MAX_PATH;
    BOOL  fDone = FALSE;
    DWORD dwPosition = 0;

    // Though we could call LoadLibrary(L"aspnetcorerh.dll") relying the OS to solve
    // the path (the targeted dll is the same folder of w3wp.exe/iisexpress)
    // let's still load with full path to avoid security issue
    if (FAILED(hr = struFilename->Resize(dwSize + 20)))
    {
        goto Finished;
    }

    while (!fDone)
    {
        DWORD dwReturnedSize = GetModuleFileNameW(g_hModule, struFilename->QueryStr(), dwSize);
        if (dwReturnedSize == 0)
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            fDone = TRUE;
            goto Finished;
        }
        else if ((dwReturnedSize == dwSize) && (GetLastError() == ERROR_INSUFFICIENT_BUFFER))
        {
            dwSize *= 2; // smaller buffer. increase the buffer and retry
            if (FAILED(hr = struFilename->Resize(dwSize + 20))) // + 20 for aspnetcorerh.dll
            {
                goto Finished;
            }
        }
        else
        {
            fDone = TRUE;
        }
    }

    if (FAILED(hr = struFilename->SyncWithBuffer()))
    {
        goto Finished;
    }
    dwPosition = struFilename->LastIndexOf(L'\\', 0);
    struFilename->QueryStr()[dwPosition] = L'\0';

    if (FAILED(hr = struFilename->SyncWithBuffer()) ||
        FAILED(hr = struFilename->Append(L"\\")) ||
        FAILED(hr = struFilename->Append(g_pwzAspnetcoreRequestHandlerName)))
    {
        goto Finished;
    }

Finished:
    return hr;
}

// 
// Tries to find aspnetcorerh.dll from the application
// Calls into hostfxr.dll to find it.
// Will leave hostfxr.dll loaded as it will be used again to call hostfxr_main.
// 
HRESULT
APPLICATION_INFO::FindNativeAssemblyFromHostfxr(
    STRU* struFilename
)
{
    HRESULT     hr = S_OK;
    STRU        struApplicationFullPath;
    STRU        struNativeSearchPaths;
    STRU        struNativeDllLocation;
    HMODULE     hmHostFxrDll = NULL;
    INT         intHostFxrExitCode = 0;
    INT         intIndex = -1;
    INT         intPrevIndex = 0;
    BOOL        fFound = FALSE;
    DWORD       dwBufferSize = 1024 * 10;
    DWORD       dwRequiredBufferSize = 0;

    DBG_ASSERT(struFileName != NULL);

    hmHostFxrDll = LoadLibraryW(m_pConfiguration->QueryHostFxrFullPath());

    if (hmHostFxrDll == NULL)
    {
        // Could not load hostfxr
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    hostfxr_get_native_search_directories_fn pFnHostFxrSearchDirectories = (hostfxr_get_native_search_directories_fn)
        GetProcAddress(hmHostFxrDll, "hostfxr_get_native_search_directories");

    if (pFnHostFxrSearchDirectories == NULL)
    {
        // Host fxr version is incorrect (need a higher version).
        // TODO log error 
        hr = E_FAIL;
        goto Finished;
    }

    if (FAILED(hr = struNativeSearchPaths.Resize(dwBufferSize)))
    {
        goto Finished;
    }

    while (TRUE)
    {
        intHostFxrExitCode = pFnHostFxrSearchDirectories(
            m_pConfiguration->QueryHostFxrArgCount(),
            m_pConfiguration->QueryHostFxrArguments(),
            struNativeSearchPaths.QueryStr(),
            dwBufferSize,
            &dwRequiredBufferSize
        );

        if (intHostFxrExitCode == 0)
        {
            break;
        }
        else if (dwRequiredBufferSize > dwBufferSize)
        {
            dwBufferSize = dwRequiredBufferSize + 1; // for null terminator

            if (FAILED(hr = struNativeSearchPaths.Resize(dwBufferSize)))
            {
                goto Finished;
            }
        }
        else
        {
            hr = E_FAIL;
            // Log "Error finding native search directories from aspnetcore application.
            goto Finished;
        }
    }

    if (FAILED(hr = struNativeSearchPaths.SyncWithBuffer()))
    {
        goto Finished;
    }

    fFound = FALSE;

    // The native search directories are semicolon delimited.
    // Split on semicolons, append aspnetcorerh.dll, and check if the file exists.
    while ((intIndex = struNativeSearchPaths.IndexOf(L";", intPrevIndex)) != -1)
    {
        if (FAILED(hr = struNativeDllLocation.Copy(&struNativeSearchPaths.QueryStr()[intPrevIndex], intIndex - intPrevIndex)))
        {
            goto Finished;
        }

        if (!struNativeDllLocation.EndsWith(L"\\"))
        {
            if (FAILED(hr = struNativeDllLocation.Append(L"\\")))
            {
                goto Finished;
            }
        }

        if (FAILED(hr = struNativeDllLocation.Append(g_pwzAspnetcoreRequestHandlerName)))
        {
            goto Finished;
        }

        if (UTILITY::CheckIfFileExists(struNativeDllLocation.QueryStr()))
        {
            if (FAILED(hr = struFilename->Copy(struNativeDllLocation)))
            {
                goto Finished;
            }
            fFound = TRUE;
            break;
        }

        intPrevIndex = intIndex + 1;
    }

    if (!fFound)
    {
        hr = E_FAIL;
        goto Finished;
    }

Finished:
    if (FAILED(hr) && hmHostFxrDll != NULL)
    {
        FreeLibrary(hmHostFxrDll);
    }
    return hr;
}

VOID
APPLICATION_INFO::RecycleApplication()
{
    APPLICATION* pApplication = NULL;
    HANDLE       hThread = INVALID_HANDLE_VALUE;
    BOOL         fLockAcquired = FALSE;

    if (m_pApplication != NULL)
    {
        AcquireSRWLockExclusive(&m_srwLock);
        fLockAcquired = TRUE;
        if (m_pApplication != NULL)
        {
            pApplication = m_pApplication;
            if (pApplication->QueryConfig()->QueryHostingModel() == HOSTING_OUT_PROCESS)
            {
                //
                // For inprocess, need to set m_pApplication to NULL first to
                // avoid mapping new request to the recycled application.
                // Outofprocess application instance will be created for new request
                // For inprocess, as recycle will lead to shutdown later, leave m_pApplication
                // to not block incoming requests till worker process shutdown
                //
                m_pApplication = NULL;
            }
            else
            {
                //
                // For inprocess, need hold the application till shutdown is called
                // Bump the reference counter as DoRecycleApplication will do dereference
                //
                pApplication->ReferenceApplication();
            }

            hThread = CreateThread(
                NULL,       // default security attributes
                0,          // default stack size
                (LPTHREAD_START_ROUTINE)DoRecycleApplication,
                pApplication,       // thread function arguments
                0,          // default creation flags
                NULL);      // receive thread identifier
        }

        if (hThread == NULL)
        {
            if (!g_fRecycleProcessCalled)
            {
                g_fRecycleProcessCalled = TRUE;
                g_pHttpServer->RecycleProcess(L"On Demand by AspNetCore Module for recycle application failure");
            }
        }
        else 
        {
            // Closing a thread handle does not terminate the associated thread or remove the thread object.
            CloseHandle(hThread);
        }

        if (fLockAcquired)
        {
            ReleaseSRWLockExclusive(&m_srwLock);
        }
    }
}


VOID
APPLICATION_INFO::DoRecycleApplication(
    LPVOID lpParam)
{
    APPLICATION* pApplication = static_cast<APPLICATION*>(lpParam);

    // No lock required

    if (pApplication != NULL)
    {
        // Recycle will call shutdown for out of process
        pApplication->Recycle();

        // Decrement the ref count as we reference it in RecycleApplication.
        pApplication->DereferenceApplication();
    }
}


VOID
APPLICATION_INFO::ShutDownApplication()
{
    APPLICATION* pApplication = NULL;
    BOOL         fLockAcquired = FALSE;

    // pApplication can be NULL due to app_offline
    if (m_pApplication != NULL)
    {
        AcquireSRWLockExclusive(&m_srwLock);
        fLockAcquired = TRUE;
        if (m_pApplication != NULL)
        {
            pApplication = m_pApplication;

            // Set m_pApplication to NULL first to prevent anyone from using it
            m_pApplication = NULL;
            pApplication->ShutDown();
            pApplication->DereferenceApplication();
        }

        if (fLockAcquired)
        {
            ReleaseSRWLockExclusive(&m_srwLock);
        }
    }
}
