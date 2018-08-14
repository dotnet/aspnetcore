// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "HandlerResolver.h"
#include "exceptions.h"
#include "SRWExclusiveLock.h"
#include "applicationinfo.h"
#include "EventLog.h"
#include "hostfxr_utility.h"
#include "GlobalVersionUtility.h"
#include "HandleWrapper.h"
#include "file_utility.h"

const PCWSTR HandlerResolver::s_pwzAspnetcoreInProcessRequestHandlerName = L"aspnetcorev2_inprocess.dll";
const PCWSTR HandlerResolver::s_pwzAspnetcoreOutOfProcessRequestHandlerName = L"aspnetcorev2_outofprocess.dll";

HandlerResolver::HandlerResolver(HMODULE hModule, IHttpServer &pServer)
    : m_hModule(hModule),
      m_pServer(pServer),
      m_fAspnetcoreRHLoadResult(S_FALSE),
      m_loadedApplicationHostingModel(HOSTING_UNKNOWN),
      m_hRequestHandlerDll(nullptr),
      m_pfnAspNetCoreCreateApplication(nullptr)
{
    InitializeSRWLock(&m_requestHandlerLoadLock);
}

HRESULT
HandlerResolver::LoadRequestHandlerAssembly(IHttpApplication &pApplication, STRU& location, ASPNETCORE_SHIM_CONFIG * pConfiguration)
{
    HRESULT hr;
    STACK_STRU(struFileName, MAX_PATH);
    PCWSTR              pstrHandlerDllName;
    if (pConfiguration->QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
    {
        pstrHandlerDllName = s_pwzAspnetcoreInProcessRequestHandlerName;
    }
    else
    {
        pstrHandlerDllName = s_pwzAspnetcoreOutOfProcessRequestHandlerName;
    }

    // Try to see if RH is already loaded, use GetModuleHandleEx to increment ref count
    if (!GetModuleHandleEx(0, pstrHandlerDllName, &m_hRequestHandlerDll))
    {
        if (pConfiguration->QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
        {
            std::unique_ptr<HOSTFXR_OPTIONS> options;

            RETURN_IF_FAILED(HOSTFXR_OPTIONS::Create(
                NULL,
                pConfiguration->QueryProcessPath()->QueryStr(),
                pApplication.GetApplicationPhysicalPath(),
                pConfiguration->QueryArguments()->QueryStr(),
                options));

            RETURN_IF_FAILED(location.Copy(options->GetExeLocation()));

            if (FAILED_LOG(hr = FindNativeAssemblyFromHostfxr(options.get(), pstrHandlerDllName, &struFileName)))
            {
                EventLog::Error(
                    ASPNETCORE_EVENT_INPROCESS_RH_MISSING,
                    ASPNETCORE_EVENT_INPROCESS_RH_MISSING_MSG,
                    struFileName.IsEmpty() ? s_pwzAspnetcoreInProcessRequestHandlerName : struFileName.QueryStr());

                return hr;
            }
        }
        else
        {
            if (FAILED_LOG(hr = FindNativeAssemblyFromGlobalLocation(pConfiguration, pstrHandlerDllName, &struFileName)))
            {
                EventLog::Error(
                    ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING,
                    ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG,
                    struFileName.IsEmpty() ? s_pwzAspnetcoreOutOfProcessRequestHandlerName : struFileName.QueryStr());

                return hr;
            }
        }

        LOG_INFOF("Loading request handler:  %S", struFileName.QueryStr());

        m_hRequestHandlerDll = LoadLibraryW(struFileName.QueryStr());
        RETURN_LAST_ERROR_IF_NULL(m_hRequestHandlerDll);
    }

    m_pfnAspNetCoreCreateApplication = reinterpret_cast<PFN_ASPNETCORE_CREATE_APPLICATION>(GetProcAddress(m_hRequestHandlerDll, "CreateApplication"));

    RETURN_LAST_ERROR_IF_NULL(m_pfnAspNetCoreCreateApplication);

    return S_OK;
}

HRESULT
HandlerResolver::GetApplicationFactory(IHttpApplication &pApplication, STRU& location, PFN_ASPNETCORE_CREATE_APPLICATION * pfnCreateApplication)
{
    ASPNETCORE_SHIM_CONFIG pConfiguration;
    RETURN_IF_FAILED(pConfiguration.Populate(&m_pServer, &pApplication));

    if (m_fAspnetcoreRHLoadResult == S_FALSE)
    {
        SRWExclusiveLock lock(m_requestHandlerLoadLock);
        if (m_fAspnetcoreRHLoadResult == S_FALSE)
        {
            m_loadedApplicationHostingModel = pConfiguration.QueryHostingModel();
            m_loadedApplicationId = pApplication.GetApplicationId();
            LOG_IF_FAILED(m_fAspnetcoreRHLoadResult = LoadRequestHandlerAssembly(pApplication, location, &pConfiguration));
        }
    }

    // Mixed hosting models
    if (m_loadedApplicationHostingModel != pConfiguration.QueryHostingModel())
    {
        EventLog::Error(
            ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR,
            ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR_MSG,
            pApplication.GetApplicationId(),
            pConfiguration.QueryHostingModel());

        return E_FAIL;
    }
    // Multiple in-process apps
    else if (m_loadedApplicationHostingModel == HOSTING_IN_PROCESS && m_loadedApplicationId != pApplication.GetApplicationId())
    {
        EventLog::Error(
            ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP,
            ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP_MSG,
            pApplication.GetApplicationId());

        return E_FAIL;
    }

    *pfnCreateApplication = m_pfnAspNetCoreCreateApplication;
    return m_fAspnetcoreRHLoadResult;
}

HRESULT
HandlerResolver::FindNativeAssemblyFromGlobalLocation(
    ASPNETCORE_SHIM_CONFIG * pConfiguration,
    PCWSTR pstrHandlerDllName,
    STRU* struFilename
)
{
    try
    {
        std::wstring modulePath = GlobalVersionUtility::GetModuleName(m_hModule);

        modulePath = GlobalVersionUtility::RemoveFileNameFromFolderPath(modulePath);

        std::wstring retval = GlobalVersionUtility::GetGlobalRequestHandlerPath(modulePath.c_str(),
            pConfiguration->QueryHandlerVersion()->QueryStr(),
            pstrHandlerDllName
        );

        RETURN_IF_FAILED(struFilename->Copy(retval.c_str()));
    }
    catch (...)
    {
        STRU struEvent;
        if (SUCCEEDED(struEvent.Copy(ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG)))
        {
            EventLog::Info(
                ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING,
                struEvent.QueryStr());
        }

        return OBSERVE_CAUGHT_EXCEPTION();
    }

    return S_OK;
}

//
// Tries to find aspnetcorerh.dll from the application
// Calls into hostfxr.dll to find it.
// Will leave hostfxr.dll loaded as it will be used again to call hostfxr_main.
//
HRESULT
HandlerResolver::FindNativeAssemblyFromHostfxr(
    HOSTFXR_OPTIONS* hostfxrOptions,
    PCWSTR libraryName,
    STRU* struFilename
)
{
    STRU        struApplicationFullPath;
    STRU        struNativeSearchPaths;
    STRU        struNativeDllLocation;
    INT         intIndex = -1;
    INT         intPrevIndex = 0;
    BOOL        fFound = FALSE;
    DWORD       dwBufferSize = 1024 * 10;
    DWORD       dwRequiredBufferSize = 0;

    DBG_ASSERT(struFilename != NULL);

    RETURN_LAST_ERROR_IF_NULL(m_hHostFxrDll = LoadLibraryW(hostfxrOptions->GetHostFxrLocation()));

    auto pFnHostFxrSearchDirectories = reinterpret_cast<hostfxr_get_native_search_directories_fn>(GetProcAddress(m_hHostFxrDll, "hostfxr_get_native_search_directories"));

    RETURN_LAST_ERROR_IF_NULL(pFnHostFxrSearchDirectories);
    RETURN_IF_FAILED(struNativeSearchPaths.Resize(dwBufferSize));

    while (TRUE)
    {
        auto intHostFxrExitCode = pFnHostFxrSearchDirectories(
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

            RETURN_IF_FAILED(struNativeSearchPaths.Resize(dwBufferSize));
        }
        else
        {
            // Log "Error finding native search directories from aspnetcore application.
            return E_UNEXPECTED;
        }
    }

    RETURN_IF_FAILED(struNativeSearchPaths.SyncWithBuffer());

    fFound = FALSE;

    // The native search directories are semicolon delimited.
    // Split on semicolons, append aspnetcorerh.dll, and check if the file exists.
    while ((intIndex = struNativeSearchPaths.IndexOf(L";", intPrevIndex)) != -1)
    {
        RETURN_IF_FAILED(struNativeDllLocation.Copy(&struNativeSearchPaths.QueryStr()[intPrevIndex], intIndex - intPrevIndex));

        if (!struNativeDllLocation.EndsWith(L"\\"))
        {
            RETURN_IF_FAILED(struNativeDllLocation.Append(L"\\"));
        }

        RETURN_IF_FAILED(struNativeDllLocation.Append(libraryName));

        if (FILE_UTILITY::CheckIfFileExists(struNativeDllLocation.QueryStr()))
        {
            RETURN_IF_FAILED(struFilename->Copy(struNativeDllLocation));
            fFound = TRUE;
            break;
        }

        intPrevIndex = intIndex + 1;
    }

    if (!fFound)
    {
        return HRESULT_FROM_WIN32(ERROR_DLL_NOT_FOUND);
    }

    return S_OK;
}

