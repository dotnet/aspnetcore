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
#include "LoggingHelpers.h"
#include "resources.h"
#include "ConfigurationLoadException.h"
#include "WebConfigConfigurationSource.h"
#include "ModuleHelpers.h"
#include "BaseOutputManager.h"
#include "Environment.h"

const PCWSTR HandlerResolver::s_pwzAspnetcoreInProcessRequestHandlerName = L"aspnetcorev2_inprocess.dll";
const PCWSTR HandlerResolver::s_pwzAspnetcoreOutOfProcessRequestHandlerName = L"aspnetcorev2_outofprocess.dll";

HandlerResolver::HandlerResolver(HMODULE hModule, const IHttpServer &pServer)
    : m_hModule(hModule),
      m_pServer(pServer),
      m_loadedApplicationHostingModel(HOSTING_UNKNOWN)
{
    InitializeSRWLock(&m_requestHandlerLoadLock);
}

HRESULT
HandlerResolver::LoadRequestHandlerAssembly(const IHttpApplication &pApplication, const ShimOptions& pConfiguration, std::unique_ptr<ApplicationFactory>& pApplicationFactory)
{
    HRESULT hr = S_OK;
    PCWSTR pstrHandlerDllName = nullptr;
    bool preventUnload = false;
    if (pConfiguration.QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
    {
        preventUnload = false;
        pstrHandlerDllName = s_pwzAspnetcoreInProcessRequestHandlerName;
    }
    else
    {
        // OutOfProcess handler is not able to handle unload correctly
        // It has code running after application.Stop exits
        preventUnload = true;
        pstrHandlerDllName = s_pwzAspnetcoreOutOfProcessRequestHandlerName;
    }
    HandleWrapper<ModuleHandleTraits> hRequestHandlerDll;
    std::wstring location;
    std::wstring handlerDllPath;
    // Try to see if RH is already loaded, use GetModuleHandleEx to increment ref count
    if (!GetModuleHandleEx(0, pstrHandlerDllName, &hRequestHandlerDll))
    {
        if (pConfiguration.QueryHostingModel() == APP_HOSTING_MODEL::HOSTING_IN_PROCESS)
        {
            std::unique_ptr<HOSTFXR_OPTIONS> options;
            std::unique_ptr<BaseOutputManager> outputManager;

            RETURN_IF_FAILED(HOSTFXR_OPTIONS::Create(
                L"",
                pConfiguration.QueryProcessPath(),
                pApplication.GetApplicationPhysicalPath(),
                pConfiguration.QueryArguments(),
                options));

            location = options->GetDotnetExeLocation();

            RETURN_IF_FAILED(LoggingHelpers::CreateLoggingProvider(
                pConfiguration.QueryStdoutLogEnabled(),
                !m_pServer.IsCommandLineLaunch(),
                pConfiguration.QueryStdoutLogFile().c_str(),
                pApplication.GetApplicationPhysicalPath(),
                outputManager));


            hr = FindNativeAssemblyFromHostfxr(*options.get(), pstrHandlerDllName, handlerDllPath, outputManager.get());

            if (FAILED_LOG(hr))
            {
                auto output = outputManager->GetStdOutContent();

                EventLog::Error(
                    ASPNETCORE_EVENT_GENERAL_ERROR,
                    ASPNETCORE_EVENT_INPROCESS_RH_ERROR_MSG,
                    output.c_str());
                return hr;
            }
        }
        else
        {
            if (FAILED_LOG(hr = FindNativeAssemblyFromGlobalLocation(pConfiguration, pstrHandlerDllName, handlerDllPath)))
            {
                EventLog::Error(
                    ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING,
                    ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG,
                    handlerDllPath.empty() ? s_pwzAspnetcoreOutOfProcessRequestHandlerName : handlerDllPath.c_str());

                return hr;
            }
        }

        LOG_INFOF(L"Loading request handler:  '%ls'", handlerDllPath.c_str());

        hRequestHandlerDll = LoadLibrary(handlerDllPath.c_str());
        RETURN_LAST_ERROR_IF_NULL(hRequestHandlerDll);
        if (preventUnload)
        {
            // Pin module in memory
            GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_PIN, handlerDllPath.c_str(), &hRequestHandlerDll);
        }
    }

    auto pfnAspNetCoreCreateApplication = ModuleHelpers::GetKnownProcAddress<PFN_ASPNETCORE_CREATE_APPLICATION>(hRequestHandlerDll, "CreateApplication");
    RETURN_LAST_ERROR_IF_NULL(pfnAspNetCoreCreateApplication);

    pApplicationFactory = std::make_unique<ApplicationFactory>(hRequestHandlerDll.release(), location, pfnAspNetCoreCreateApplication);
    return S_OK;
}

HRESULT
HandlerResolver::GetApplicationFactory(const IHttpApplication &pApplication, std::unique_ptr<ApplicationFactory>& pApplicationFactory, const ShimOptions& options)
{
    SRWExclusiveLock lock(m_requestHandlerLoadLock);
    if (m_loadedApplicationHostingModel != HOSTING_UNKNOWN)
    {
        // Mixed hosting models
        if (m_loadedApplicationHostingModel != options.QueryHostingModel())
        {
            EventLog::Error(
                ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR,
                ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR_MSG,
                pApplication.GetApplicationId(),
                options.QueryHostingModel());

            return E_FAIL;
        }
        // Multiple in-process apps
        if (m_loadedApplicationHostingModel == HOSTING_IN_PROCESS && m_loadedApplicationId != pApplication.GetApplicationId())
        {
            EventLog::Error(
                ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP,
                ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP_MSG,
                pApplication.GetApplicationId());

            return E_FAIL;
        }
    }

    m_loadedApplicationHostingModel = options.QueryHostingModel();
    m_loadedApplicationId = pApplication.GetApplicationId();
    RETURN_IF_FAILED(LoadRequestHandlerAssembly(pApplication, options, pApplicationFactory));

    return S_OK;
}

void HandlerResolver::ResetHostingModel()
{
    SRWExclusiveLock lock(m_requestHandlerLoadLock);

    m_loadedApplicationHostingModel = APP_HOSTING_MODEL::HOSTING_UNKNOWN;
    m_loadedApplicationId.resize(0);
}

HRESULT
HandlerResolver::FindNativeAssemblyFromGlobalLocation(
    const ShimOptions& pConfiguration,
    PCWSTR pstrHandlerDllName,
    std::wstring& handlerDllPath
)
{
    try
    {
        auto handlerPath = Environment::GetEnvironmentVariableValue(L"ASPNETCORE_MODULE_OUTOFPROCESS_HANDLER");
        if (handlerPath.has_value() && std::filesystem::is_regular_file(handlerPath.value()))
        {
            handlerDllPath = handlerPath.value();
            return S_OK;
        }

        std::wstring modulePath = GlobalVersionUtility::GetModuleName(m_hModule);

        modulePath = GlobalVersionUtility::RemoveFileNameFromFolderPath(modulePath);

        handlerDllPath = GlobalVersionUtility::GetGlobalRequestHandlerPath(modulePath.c_str(),
            pConfiguration.QueryHandlerVersion().c_str(),
            pstrHandlerDllName
        );
    }
    catch (...)
    {
       EventLog::Info(
                ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING,
                ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG,
                pstrHandlerDllName);

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
    const HOSTFXR_OPTIONS& hostfxrOptions,
    PCWSTR libraryName,
    std::wstring& handlerDllPath,
    BaseOutputManager* outputManager
)
{
    std::wstring   struNativeSearchPaths;
    size_t         intIndex = 0;
    size_t         intPrevIndex = 0;
    DWORD          dwBufferSize = s_initialGetNativeSearchDirectoriesBufferSize;
    DWORD          dwRequiredBufferSize = 0;
    hostfxr_get_native_search_directories_fn pFnHostFxrSearchDirectories = nullptr;

    RETURN_LAST_ERROR_IF_NULL(m_hHostFxrDll = LoadLibraryW(hostfxrOptions.GetHostFxrLocation().c_str()));

    try
    {
        pFnHostFxrSearchDirectories = ModuleHelpers::GetKnownProcAddress<hostfxr_get_native_search_directories_fn>(m_hHostFxrDll, "hostfxr_get_native_search_directories");
    }
    catch (...)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_GENERAL_ERROR,
            ASPNETCORE_EVENT_HOSTFXR_DLL_INVALID_VERSION_MSG,
            hostfxrOptions.GetHostFxrLocation().c_str()
        );
        return OBSERVE_CAUGHT_EXCEPTION();
    }

    RETURN_LAST_ERROR_IF_NULL(pFnHostFxrSearchDirectories);
    struNativeSearchPaths.resize(dwBufferSize);

    outputManager->TryStartRedirection();

    while (TRUE)
    {
        DWORD                       hostfxrArgc;
        std::unique_ptr<PCWSTR[]>   hostfxrArgv;

        hostfxrOptions.GetArguments(hostfxrArgc, hostfxrArgv);
        const auto intHostFxrExitCode = pFnHostFxrSearchDirectories(
            hostfxrArgc,
            hostfxrArgv.get(),
            struNativeSearchPaths.data(),
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

            struNativeSearchPaths.resize(dwBufferSize);
        }
        else
        {
            // Stop redirecting before logging to event log to avoid logging debug logs
            // twice.
            outputManager->TryStopRedirection();

            // If hostfxr didn't set the required buffer size, something in the app is misconfigured
            // Ex: Framework not found.
            EventLog::Error(
                ASPNETCORE_EVENT_GENERAL_ERROR,
                ASPNETCORE_EVENT_HOSTFXR_FAILURE_MSG
            );

            return E_UNEXPECTED;
        }
    }

    outputManager->TryStopRedirection();

    struNativeSearchPaths.resize(struNativeSearchPaths.find(L'\0'));

    auto fFound = FALSE;

    // The native search directories are semicolon delimited.
    // Split on semicolons, append aspnetcorerh.dll, and check if the file exists.
    while ((intIndex = struNativeSearchPaths.find(L';', intPrevIndex)) != std::wstring::npos)
    {
        auto path = struNativeSearchPaths.substr(intPrevIndex, intIndex - intPrevIndex);

        if (!path.empty() && !(path[path.length() - 1] == L'\\'))
        {
            path.append(L"\\");
        }

        path.append(libraryName);

        if (std::filesystem::is_regular_file(path))
        {
            handlerDllPath = path;
            fFound = TRUE;
            break;
        }

        intPrevIndex = intIndex + 1;
    }

    if (!fFound)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_GENERAL_ERROR,
            ASPNETCORE_EVENT_INPROCESS_RH_REFERENCE_MSG,
            handlerDllPath.empty() ? s_pwzAspnetcoreInProcessRequestHandlerName : handlerDllPath.c_str());
        return HRESULT_FROM_WIN32(ERROR_DLL_NOT_FOUND);
    }

    return S_OK;
}

