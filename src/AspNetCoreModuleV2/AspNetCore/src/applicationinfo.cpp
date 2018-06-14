// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationinfo.h"

#include "proxymodule.h"
#include "hostfxr_utility.h"
#include "utility.h"

const PCWSTR APPLICATION_INFO::s_pwzAspnetcoreInProcessRequestHandlerName = L"aspnetcorev2_inprocess.dll";
const PCWSTR APPLICATION_INFO::s_pwzAspnetcoreOutOfProcessRequestHandlerName = L"aspnetcorev2_outofprocess.dll";

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
        delete m_pConfiguration;
        m_pConfiguration = NULL;
    }
}

HRESULT
APPLICATION_INFO::Initialize(
    _In_ IHttpServer              *pServer,
    _In_ IHttpApplication         *pApplication,
    _In_ FILE_WATCHER             *pFileWatcher
)
{
    HRESULT hr = S_OK;

    DBG_ASSERT(pServer);
    DBG_ASSERT(pApplication);
    DBG_ASSERT(pFileWatcher);

    // todo: make sure Initialize should be called only once
    m_pServer = pServer;
    m_pConfiguration = new ASPNETCORE_SHIM_CONFIG();

    if (m_pConfiguration == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = m_pConfiguration->Populate(m_pServer, pApplication);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = m_struInfoKey.Copy(pApplication->GetApplicationId());
    if (FAILED(hr))
    {
        goto Finished;
    }

    m_pFileWatcherEntry = new FILE_WATCHER_ENTRY(pFileWatcher);
    if (m_pFileWatcherEntry == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
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

    if (INVALID_FILE_ATTRIBUTES == GetFileAttributes(strFilePath.QueryStr()))
    {
        // Check if app offline was originally present.
        // if it was, log that app_offline has been dropped.
        if (m_fAppOfflineFound)
        {
            UTILITY::LogEvent(g_hEventLog,
                EVENTLOG_INFORMATION_TYPE,
                ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_REMOVED,
                ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_REMOVED_MSG);
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
                m_pConfiguration->QueryApplicationPath()->QueryStr())))
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
APPLICATION_INFO::EnsureApplicationCreated(
    IHttpContext *pHttpContext
)
{
    HRESULT             hr = S_OK;
    BOOL                fLocked = FALSE;
    IAPPLICATION       *pApplication = NULL;
    STRU                struExeLocation;
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

            hr = FindRequestHandlerAssembly(struExeLocation);
            if (FAILED(hr))
            {
                goto Finished;
            }

            if (m_pfnAspNetCoreCreateApplication == NULL)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_FUNCTION);
                goto Finished;
            }

            hr = m_pfnAspNetCoreCreateApplication(m_pServer, pHttpContext, struExeLocation.QueryStr(), &pApplication);

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
APPLICATION_INFO::FindRequestHandlerAssembly(STRU& location)
{
    HRESULT             hr = S_OK;
    BOOL                fLocked = FALSE;
    PCWSTR              pstrHandlerDllName;
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
            pstrHandlerDllName = s_pwzAspnetcoreInProcessRequestHandlerName;
        }
        else
        {
            pstrHandlerDllName = s_pwzAspnetcoreOutOfProcessRequestHandlerName;
        }

        // Try to see if RH is already loaded
        g_hAspnetCoreRH = GetModuleHandle(pstrHandlerDllName);

        if (g_hAspnetCoreRH == NULL)
        {
            if (m_pConfiguration->QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
            {
                std::unique_ptr<HOSTFXR_OPTIONS> options;

                if (FAILED(hr = HOSTFXR_OPTIONS::Create(
                    NULL,
                    m_pConfiguration->QueryProcessPath()->QueryStr(),
                    m_pConfiguration->QueryApplicationPhysicalPath()->QueryStr(),
                    m_pConfiguration->QueryArguments()->QueryStr(),
                    g_hEventLog,
                    options)))
                {
                    goto Finished;
                }

                location.Copy(options->GetExeLocation());

                if (FAILED(hr = FindNativeAssemblyFromHostfxr(options.get(), pstrHandlerDllName, &struFileName)))
                {
                    UTILITY::LogEventF(g_hEventLog,
                            EVENTLOG_ERROR_TYPE,
                            ASPNETCORE_EVENT_INPROCESS_RH_MISSING,
                            ASPNETCORE_EVENT_INPROCESS_RH_MISSING_MSG,
                            struFileName.IsEmpty() ? s_pwzAspnetcoreInProcessRequestHandlerName : struFileName.QueryStr());

                    goto Finished;
                }
            }
            else
            {
                if (FAILED(hr = FindNativeAssemblyFromGlobalLocation(pstrHandlerDllName, &struFileName)))
                {
                    UTILITY::LogEventF(g_hEventLog,
                        EVENTLOG_ERROR_TYPE,
                        ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING,
                        ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG,
                        struFileName.IsEmpty() ? s_pwzAspnetcoreOutOfProcessRequestHandlerName : struFileName.QueryStr());

                    goto Finished;
                }
            }

            g_hAspnetCoreRH = LoadLibraryW(struFileName.QueryStr());

            if (g_hAspnetCoreRH == NULL)
            {
                hr = HRESULT_FROM_WIN32(GetLastError());
                goto Finished;
            }
        }

        g_pfnAspNetCoreCreateApplication = (PFN_ASPNETCORE_CREATE_APPLICATION)
            GetProcAddress(g_hAspnetCoreRH, "CreateApplication");
        if (g_pfnAspNetCoreCreateApplication == NULL)
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
APPLICATION_INFO::FindNativeAssemblyFromGlobalLocation(
    PCWSTR pstrHandlerDllName,
    STRU* struFilename
)
{
    HRESULT hr = S_OK;

    try
    {
        std::wstring modulePath = GlobalVersionUtility::GetModuleName(g_hModule);

        modulePath = GlobalVersionUtility::RemoveFileNameFromFolderPath(modulePath);

        std::wstring retval = GlobalVersionUtility::GetGlobalRequestHandlerPath(modulePath.c_str(),
            m_pConfiguration->QueryHandlerVersion()->QueryStr(),
            pstrHandlerDllName
        );

        if (FAILED(hr = struFilename->Copy(retval.c_str())))
        {
            return hr;
        }
    }
    catch (std::exception& e)
    {
        STRU struEvent;
        if (SUCCEEDED(struEvent.Copy(ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG))
            && SUCCEEDED(struEvent.AppendA(e.what())))
        {
            UTILITY::LogEvent(g_hEventLog,
                EVENTLOG_INFORMATION_TYPE,
                ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING,
                struEvent.QueryStr());
        }
       
        hr = E_FAIL;
    }
    catch (...)
    {
        hr = E_FAIL;
    }

    return hr;
}

//
// Tries to find aspnetcorerh.dll from the application
// Calls into hostfxr.dll to find it.
// Will leave hostfxr.dll loaded as it will be used again to call hostfxr_main.
//
HRESULT
APPLICATION_INFO::FindNativeAssemblyFromHostfxr(
    HOSTFXR_OPTIONS* hostfxrOptions,
    PCWSTR libraryName,
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

    hmHostFxrDll = LoadLibraryW(hostfxrOptions->GetHostFxrLocation());

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
            hostfxrOptions->GetArgc(),
            hostfxrOptions->GetArgv(),
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

        if (FAILED(hr = struNativeDllLocation.Append(libraryName)))
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
    IAPPLICATION* pApplication = NULL;
    HANDLE       hThread = INVALID_HANDLE_VALUE;
    BOOL         fLockAcquired = FALSE;

    if (m_pApplication != NULL)
    {
        AcquireSRWLockExclusive(&m_srwLock);
        fLockAcquired = TRUE;
        if (m_pApplication != NULL)
        {
            pApplication = m_pApplication;
            if (m_pConfiguration->QueryHostingModel() == HOSTING_OUT_PROCESS)
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
        else
        {
            if (m_pConfiguration->QueryHostingModel() == HOSTING_IN_PROCESS)
            {
                // In process application failed to start for whatever reason, need to recycle the work process
                m_pServer->RecycleProcess(L"AspNetCore InProcess Recycle Process on Demand");
            }

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
    IAPPLICATION* pApplication = static_cast<IAPPLICATION*>(lpParam);

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
    IAPPLICATION* pApplication = NULL;
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
