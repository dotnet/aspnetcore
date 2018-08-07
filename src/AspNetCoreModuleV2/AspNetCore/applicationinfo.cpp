// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationinfo.h"

#include <array>
#include "proxymodule.h"
#include "hostfxr_utility.h"
#include "utility.h"
#include "debugutil.h"
#include "resources.h"
#include "SRWExclusiveLock.h"
#include "GlobalVersionUtility.h"
#include "exceptions.h"
#include "EventLog.h"
#include "HandleWrapper.h"
#include "ServerErrorApplication.h"
#include "AppOfflineApplication.h"

extern HINSTANCE    g_hModule;

const PCWSTR APPLICATION_INFO::s_pwzAspnetcoreInProcessRequestHandlerName = L"aspnetcorev2_inprocess.dll";
const PCWSTR APPLICATION_INFO::s_pwzAspnetcoreOutOfProcessRequestHandlerName = L"aspnetcorev2_outofprocess.dll";

SRWLOCK      APPLICATION_INFO::s_requestHandlerLoadLock {};
bool         APPLICATION_INFO::s_fAspnetcoreRHAssemblyLoaded = false;
bool         APPLICATION_INFO::s_fAspnetcoreRHLoadedError = false;
HMODULE      APPLICATION_INFO::s_hAspnetCoreRH = nullptr;

PFN_ASPNETCORE_CREATE_APPLICATION APPLICATION_INFO::s_pfnAspNetCoreCreateApplication = nullptr;

APPLICATION_INFO::~APPLICATION_INFO()
{
    ShutDownApplication();
}

HRESULT
APPLICATION_INFO::Initialize(
    _In_ IHttpApplication         &pApplication
)
{
    m_pConfiguration.reset(new ASPNETCORE_SHIM_CONFIG());
    RETURN_IF_FAILED(m_pConfiguration->Populate(&m_pServer, &pApplication));
    RETURN_IF_FAILED(m_struInfoKey.Copy(pApplication.GetApplicationId()));

    return S_OK;
}


HRESULT
APPLICATION_INFO::GetOrCreateApplication(
    IHttpContext *pHttpContext,
    std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER>& pApplication
)
{
    HRESULT             hr = S_OK;

    SRWExclusiveLock lock(m_applicationLock);

    auto& httpApplication = *pHttpContext->GetApplication();

    if (m_pApplication != nullptr)
    {
        if (m_pApplication->QueryStatus() == RECYCLED)
        {
            LOG_INFO("Application went offline");

            // Call to wait for application to complete stopping
            m_pApplication->Stop(/* fServerInitiated */ false);
            m_pApplication = nullptr;
        }
        else
        {
            // another thread created the application
            FINISHED(S_OK);
        }
    }

    if (AppOfflineApplication::ShouldBeStarted(httpApplication))
    {
        LOG_INFO("Detected app_offline file, creating polling application");
        m_pApplication.reset(new AppOfflineApplication(httpApplication));
    }
    else
    {
        STRU struExeLocation;
        FINISHED_IF_FAILED(FindRequestHandlerAssembly(struExeLocation));

        if (m_pfnAspNetCoreCreateApplication == NULL)
        {
            FINISHED(HRESULT_FROM_WIN32(ERROR_INVALID_FUNCTION));
        }

        std::array<APPLICATION_PARAMETER, 1> parameters {
            {"InProcessExeLocation", struExeLocation.QueryStr()}
        };

        LOG_INFO("Creating handler application");
        IAPPLICATION * newApplication;
        FINISHED_IF_FAILED(m_pfnAspNetCoreCreateApplication(
            &m_pServer,
            &httpApplication,
            parameters.data(),
            static_cast<DWORD>(parameters.size()),
            &newApplication));

        m_pApplication.reset(newApplication);
    }

Finished:

    if (FAILED(hr))
    {
        // Log the failure and update application info to not try again
        UTILITY::LogEventF(g_hEventLog,
            EVENTLOG_ERROR_TYPE,
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR,
            ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG,
            httpApplication.GetApplicationId(),
            hr);

        m_pApplication.reset(new ServerErrorApplication(httpApplication, hr));
    }

    if (m_pApplication)
    {
        pApplication = ReferenceApplication(m_pApplication.get());
    }

    return hr;
}

HRESULT
APPLICATION_INFO::FindRequestHandlerAssembly(STRU& location)
{
    HRESULT             hr = S_OK;
    PCWSTR              pstrHandlerDllName;
    STACK_STRU(struFileName, 256);

    if (s_fAspnetcoreRHLoadedError)
    {
        FINISHED(E_APPLICATION_ACTIVATION_EXEC_FAILURE);
    }
    else if (!s_fAspnetcoreRHAssemblyLoaded)
    {
        SRWExclusiveLock lock(s_requestHandlerLoadLock);
        if (s_fAspnetcoreRHLoadedError)
        {
            FINISHED(E_APPLICATION_ACTIVATION_EXEC_FAILURE);
        }
        if (s_fAspnetcoreRHAssemblyLoaded)
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
        s_hAspnetCoreRH = GetModuleHandle(pstrHandlerDllName);

        if (s_hAspnetCoreRH == NULL)
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

            LOG_INFOF("Loading request handler: %S", struFileName.QueryStr());

            s_hAspnetCoreRH = LoadLibraryW(struFileName.QueryStr());

            if (s_hAspnetCoreRH == NULL)
            {
                FINISHED(HRESULT_FROM_WIN32(GetLastError()));
            }
        }

        s_pfnAspNetCoreCreateApplication = (PFN_ASPNETCORE_CREATE_APPLICATION)
            GetProcAddress(s_hAspnetCoreRH, "CreateApplication");
        if (s_pfnAspNetCoreCreateApplication == NULL)
        {
            FINISHED(HRESULT_FROM_WIN32(GetLastError()));
        }

        s_fAspnetcoreRHAssemblyLoaded = TRUE;
    }

Finished:
    //
    // Question: we remember the load failure so that we will not try again.
    // User needs to check whether the fuction pointer is NULL
    //
    m_pfnAspNetCoreCreateApplication = s_pfnAspNetCoreCreateApplication;

    if (!s_fAspnetcoreRHLoadedError && FAILED(hr))
    {
        s_fAspnetcoreRHLoadedError = TRUE;
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

    DBG_ASSERT(struFilename != NULL);

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
    SRWExclusiveLock lock(m_applicationLock);

    if (m_pApplication)
    {
        const auto pApplication = m_pApplication.release();

        HandleWrapper<InvalidHandleTraits> hThread = CreateThread(
            NULL,       // default security attributes
            0,          // default stack size
            (LPTHREAD_START_ROUTINE)DoRecycleApplication,
            pApplication,       // thread function arguments
            0,          // default creation flags
            NULL);      // receive thread identifier
    }
}


DWORD WINAPI
APPLICATION_INFO::DoRecycleApplication(
    LPVOID lpParam)
{
    auto pApplication = std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER>(static_cast<IAPPLICATION*>(lpParam));

    if (pApplication)
    {
        // Recycle will call shutdown for out of process
        pApplication->Stop(/*fServerInitiated*/ false);
    }

    return 0;
}


VOID
APPLICATION_INFO::ShutDownApplication()
{
    SRWExclusiveLock lock(m_applicationLock);

    if (m_pApplication)
    {
        m_pApplication ->Stop(/* fServerInitiated */ true);
        m_pApplication = nullptr;
    }
}
