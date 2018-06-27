// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationinfo.h"

#include "proxymodule.h"
#include "hostfxr_utility.h"
#include "utility.h"
#include "debugutil.h"
#include "resources.h"
#include "SRWExclusiveLock.h"
#include "GlobalVersionUtility.h"
#include "exceptions.h"

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
    FINISHED_IF_NULL_ALLOC(m_pConfiguration = new ASPNETCORE_SHIM_CONFIG());
    FINISHED_IF_FAILED(m_pConfiguration->Populate(m_pServer, pApplication));
    FINISHED_IF_FAILED(m_struInfoKey.Copy(pApplication->GetApplicationId()));
    FINISHED_IF_NULL_ALLOC(m_pFileWatcherEntry = new FILE_WATCHER_ENTRY(pFileWatcher));

    UpdateAppOfflineFileHandle();

Finished:
    return hr;
}

HRESULT
APPLICATION_INFO::StartMonitoringAppOffline()
{
    if (m_pFileWatcherEntry != NULL)
    {
        RETURN_IF_FAILED(m_pFileWatcherEntry->Create(m_pConfiguration->QueryApplicationPhysicalPath()->QueryStr(), L"app_offline.htm", this, NULL));
    }
    return S_OK;
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
    IAPPLICATION       *pApplication = NULL;
    STRU                struExeLocation;
    STRU                struHostFxrDllLocation;
    STACK_STRU(struFileName, 300);  // >MAX_PATH

    if (m_pApplication != NULL)
    {
        return S_OK;
    }

    // one optimization for failure scenario is to reduce the lock scope
    SRWExclusiveLock lock(m_srwLock);

    if (m_fDoneAppCreation)
    {
        // application is NULL and CreateApplication failed previously
        FINISHED(E_APPLICATION_ACTIVATION_EXEC_FAILURE);
    }
    else
    {
        if (m_pApplication != NULL)
        {
            // another thread created the applicaiton
            FINISHED(S_OK);
        }
        else if (m_fDoneAppCreation)
        {
            // previous CreateApplication failed
            FINISHED(E_APPLICATION_ACTIVATION_EXEC_FAILURE);
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

            m_fDoneAppCreation = TRUE;
            FINISHED_IF_FAILED(FindRequestHandlerAssembly(struExeLocation));

            if (m_pfnAspNetCoreCreateApplication == NULL)
            {
                FINISHED(HRESULT_FROM_WIN32(ERROR_INVALID_FUNCTION));
            }

            FINISHED_IF_FAILED(m_pfnAspNetCoreCreateApplication(m_pServer, pHttpContext->GetApplication(), &pApplication));
            pApplication->SetParameter(L"InProcessExeLocation", struExeLocation.QueryStr());

            m_pApplication = pApplication;
        }
    }

Finished:

    if (FAILED(hr))
    {
        // Log the failure and update application info to not try again
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_ERROR_TYPE,
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR,
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG,
            pHttpContext->GetApplication()->GetApplicationId(),
            hr);
    }

    return hr;
}

HRESULT
APPLICATION_INFO::FindRequestHandlerAssembly(STRU& location)
{
    HRESULT             hr = S_OK;
    PCWSTR              pstrHandlerDllName;
    STACK_STRU(struFileName, 256);

    if (g_fAspnetcoreRHLoadedError)
    {
        FINISHED(E_APPLICATION_ACTIVATION_EXEC_FAILURE);
    }
    else if (!g_fAspnetcoreRHAssemblyLoaded)
    {
        SRWExclusiveLock lock(g_srwLock);

        if (g_fAspnetcoreRHLoadedError)
        {
            FINISHED(E_APPLICATION_ACTIVATION_EXEC_FAILURE);
        }
        if (g_fAspnetcoreRHAssemblyLoaded)
        {
            FINISHED(S_OK);
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

                FINISHED_IF_FAILED(HOSTFXR_OPTIONS::Create(
                    NULL,
                    m_pConfiguration->QueryProcessPath()->QueryStr(),
                    m_pConfiguration->QueryApplicationPhysicalPath()->QueryStr(),
                    m_pConfiguration->QueryArguments()->QueryStr(),
                    g_hEventLog,
                    options));

                FINISHED_IF_FAILED(location.Copy(options->GetExeLocation()));

                if (FAILED_LOG(hr = FindNativeAssemblyFromHostfxr(options.get(), pstrHandlerDllName, &struFileName)))
                {
                    UTILITY::LogEventF(g_hEventLog,
                            EVENTLOG_ERROR_TYPE,
                            ASPNETCORE_EVENT_INPROCESS_RH_MISSING,
                            ASPNETCORE_EVENT_INPROCESS_RH_MISSING_MSG,
                            struFileName.IsEmpty() ? s_pwzAspnetcoreInProcessRequestHandlerName : struFileName.QueryStr());

                    FINISHED(hr);
                }
            }
            else
            {
                if (FAILED_LOG(hr = FindNativeAssemblyFromGlobalLocation(pstrHandlerDllName, &struFileName)))
                {
                    UTILITY::LogEventF(g_hEventLog,
                        EVENTLOG_ERROR_TYPE,
                        ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING,
                        ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG,
                        struFileName.IsEmpty() ? s_pwzAspnetcoreOutOfProcessRequestHandlerName : struFileName.QueryStr());

                    FINISHED(hr);
                }
            }

            WLOG_INFOF(L"Loading request handler: %s", struFileName.QueryStr());

            g_hAspnetCoreRH = LoadLibraryW(struFileName.QueryStr());

            if (g_hAspnetCoreRH == NULL)
            {
                FINISHED(HRESULT_FROM_WIN32(GetLastError()));
            }
        }

        g_pfnAspNetCoreCreateApplication = (PFN_ASPNETCORE_CREATE_APPLICATION)
            GetProcAddress(g_hAspnetCoreRH, "CreateApplication");
        if (g_pfnAspNetCoreCreateApplication == NULL)
        {
            FINISHED(HRESULT_FROM_WIN32(GetLastError()));
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

        RETURN_IF_FAILED(struFilename->Copy(retval.c_str()));
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

    FINISHED_LAST_ERROR_IF_NULL(hmHostFxrDll = LoadLibraryW(hostfxrOptions->GetHostFxrLocation()));

    hostfxr_get_native_search_directories_fn pFnHostFxrSearchDirectories = (hostfxr_get_native_search_directories_fn)
        GetProcAddress(hmHostFxrDll, "hostfxr_get_native_search_directories");

    if (pFnHostFxrSearchDirectories == NULL)
    {
        // Host fxr version is incorrect (need a higher version).
        // TODO log error
        FINISHED(E_FAIL);
    }

    FINISHED_IF_FAILED(hr = struNativeSearchPaths.Resize(dwBufferSize));

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

            FINISHED_IF_FAILED(struNativeSearchPaths.Resize(dwBufferSize));
        }
        else
        {
            // Log "Error finding native search directories from aspnetcore application.
            FINISHED(E_FAIL);
        }
    }

    FINISHED_IF_FAILED(struNativeSearchPaths.SyncWithBuffer());

    fFound = FALSE;

    // The native search directories are semicolon delimited.
    // Split on semicolons, append aspnetcorerh.dll, and check if the file exists.
    while ((intIndex = struNativeSearchPaths.IndexOf(L";", intPrevIndex)) != -1)
    {
        FINISHED_IF_FAILED(struNativeDllLocation.Copy(&struNativeSearchPaths.QueryStr()[intPrevIndex], intIndex - intPrevIndex));

        if (!struNativeDllLocation.EndsWith(L"\\"))
        {
            FINISHED_IF_FAILED(struNativeDllLocation.Append(L"\\"));
        }

        FINISHED_IF_FAILED(struNativeDllLocation.Append(libraryName));

        if (UTILITY::CheckIfFileExists(struNativeDllLocation.QueryStr()))
        {
            FINISHED_IF_FAILED(struFilename->Copy(struNativeDllLocation));
            fFound = TRUE;
            break;
        }

        intPrevIndex = intIndex + 1;
    }

    if (!fFound)
    {
        FINISHED(E_FAIL);
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
    IAPPLICATION* pApplication;
    HANDLE       hThread = INVALID_HANDLE_VALUE;

    if (m_pApplication != NULL)
    {
        SRWExclusiveLock lock(m_srwLock);

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

    // pApplication can be NULL due to app_offline
    if (m_pApplication != NULL)
    {
        SRWExclusiveLock lock(m_srwLock);

        if (m_pApplication != NULL)
        {
            pApplication = m_pApplication;

            // Set m_pApplication to NULL first to prevent anyone from using it
            m_pApplication = NULL;
            pApplication->ShutDown();
            pApplication->DereferenceApplication();
        }
    }
}
