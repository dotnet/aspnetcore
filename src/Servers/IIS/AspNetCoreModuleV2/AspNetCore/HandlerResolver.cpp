// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "HandlerResolver.h"
#include "exceptions.h"
#include "SRWExclusiveLock.h"
#include "applicationinfo.h"
#include "EventLog.h"
#include "GlobalVersionUtility.h"
#include "HandleWrapper.h"
#include "file_utility.h"
#include "LoggingHelpers.h"
#include "resources.h"
#include "ModuleHelpers.h"
#include "Environment.h"
#include "HostFxr.h"
#include "RedirectionOutput.h"

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
HandlerResolver::LoadRequestHandlerAssembly(const IHttpApplication &pApplication,
    const ShimOptions& pConfiguration,
    std::unique_ptr<ApplicationFactory>& pApplicationFactory,
    ErrorContext& errorContext)
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
            errorContext.generalErrorType = "ASP.NET Core IIS hosting failure (in-process)";
            std::unique_ptr<HostFxrResolutionResult> options;

            RETURN_IF_FAILED(HostFxrResolutionResult::Create(
                L"",
                pConfiguration.QueryProcessPath(),
                pApplication.GetApplicationPhysicalPath(),
                pConfiguration.QueryArguments(),
                errorContext,
                options));

            location = options->GetDotnetExeLocation();

            auto redirectionOutput = std::make_shared<StringStreamRedirectionOutput>();

            hr = FindNativeAssemblyFromHostfxr(*options, pstrHandlerDllName, handlerDllPath, pApplication, pConfiguration, redirectionOutput, errorContext);

            auto output = redirectionOutput->GetOutput();

            if (FAILED_LOG(hr))
            {
                EventLog::Error(
                    ASPNETCORE_EVENT_GENERAL_ERROR,
                    ASPNETCORE_EVENT_INPROCESS_RH_ERROR_MSG,
                    output.c_str());
                return hr;
            }
        }
        else
        {
            errorContext.generalErrorType = "ASP.NET Core IIS hosting failure (out-of-process)";

            if (FAILED_LOG(hr = FindNativeAssemblyFromGlobalLocation(pConfiguration, pstrHandlerDllName, handlerDllPath)))
            {
                auto handlerName = handlerDllPath.empty() ? s_pwzAspnetcoreOutOfProcessRequestHandlerName : handlerDllPath.c_str();
                EventLog::Error(
                    ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING,
                    ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG,
                    handlerName);

                errorContext.detailedErrorContent = to_multi_byte_string(format(ASPNETCORE_EVENT_OUT_OF_PROCESS_RH_MISSING_MSG, handlerName), CP_UTF8);
                errorContext.statusCode = 500i16;
                errorContext.subStatusCode = 36i16;
                errorContext.errorReason = "The out of process request handler, aspnetcorev2_outofprocess.dll, could not be found next to the aspnetcorev2.dll.";

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
HandlerResolver::GetApplicationFactory(const IHttpApplication& pApplication, std::unique_ptr<ApplicationFactory>& pApplicationFactory, const ShimOptions& options, ErrorContext& errorContext)
{
    SRWExclusiveLock lock(m_requestHandlerLoadLock);
    if (m_loadedApplicationHostingModel != HOSTING_UNKNOWN)
    {
        // Mixed hosting models
        if (m_loadedApplicationHostingModel != options.QueryHostingModel())
        {
            errorContext.detailedErrorContent = to_multi_byte_string(format(ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR_MSG, pApplication.GetApplicationId(), options.QueryHostingModel()), CP_UTF8);
            errorContext.statusCode = 500i16;
            errorContext.subStatusCode = 34i16;
            errorContext.generalErrorType = "ASP.NET Core does not support mixing hosting models";
            errorContext.errorReason = "Select a different app pool to host this app.";

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
            errorContext.detailedErrorContent = to_multi_byte_string(format(ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP_MSG, pApplication.GetApplicationId()), CP_UTF8);

            errorContext.statusCode = 500i16;
            errorContext.subStatusCode = 35i16;
            errorContext.generalErrorType = "ASP.NET Core does not support multiple apps in the same app pool";
            errorContext.errorReason = "Select a different app pool to host this app.";

            EventLog::Error(
                ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP,
                ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP_MSG,
                pApplication.GetApplicationId());

            return E_FAIL;
        }
    }

    m_loadedApplicationHostingModel = options.QueryHostingModel();
    m_loadedApplicationId = pApplication.GetApplicationId();
    RETURN_IF_FAILED(LoadRequestHandlerAssembly(pApplication, options, pApplicationFactory, errorContext));

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
    const HostFxrResolutionResult& hostfxrOptions,
    PCWSTR libraryName,
    std::wstring& handlerDllPath,
    const IHttpApplication &pApplication,
    const ShimOptions& pConfiguration,
    std::shared_ptr<StringStreamRedirectionOutput> stringRedirectionOutput,
    ErrorContext& errorContext
)
try
{
    std::wstring   struNativeSearchPaths;
    size_t         intIndex = 0;
    size_t         intPrevIndex = 0;
    DWORD          dwBufferSize = s_initialGetNativeSearchDirectoriesBufferSize;
    DWORD          dwRequiredBufferSize = 0;

    try
    {
        m_hHostFxrDll.Load(hostfxrOptions.GetHostFxrLocation());
    }
    catch (...)
    {
        errorContext.detailedErrorContent = "Could not load hostfxr.dll.";
        errorContext.statusCode = 500i16;
        errorContext.subStatusCode = 32i16;
        errorContext.generalErrorType = "Failed to load .NET Core host";
        errorContext.errorReason = "The app was likely published for a different bitness than w3wp.exe/iisexpress.exe is running as.";
        throw;
    }
    {
        auto redirectionOutput = LoggingHelpers::CreateOutputs(
                pConfiguration.QueryStdoutLogEnabled(),
                pConfiguration.QueryStdoutLogFile(),
                pApplication.GetApplicationPhysicalPath(),
                stringRedirectionOutput
            );

        StandardStreamRedirection stdOutRedirection(*redirectionOutput.get(), m_pServer.IsCommandLineLaunch());
        auto hostFxrErrorRedirection = m_hHostFxrDll.RedirectOutput(redirectionOutput.get());

        struNativeSearchPaths.resize(dwBufferSize);
        while (TRUE)
        {
            DWORD                       hostfxrArgc;
            std::unique_ptr<PCWSTR[]>   hostfxrArgv;

            hostfxrOptions.GetArguments(hostfxrArgc, hostfxrArgv);

            const auto intHostFxrExitCode = m_hHostFxrDll.GetNativeSearchDirectories(
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
                // If hostfxr didn't set the required buffer size, something in the app is misconfigured
                // This like almost always is framework not found.
                auto output = to_multi_byte_string(stringRedirectionOutput->GetOutput(), CP_UTF8);
                errorContext.detailedErrorContent.resize(output.length());
                memcpy(&errorContext.detailedErrorContent[0], output.c_str(), output.length());

                errorContext.statusCode = 500i16;
                errorContext.subStatusCode = 31i16;
                errorContext.generalErrorType = "Failed to load ASP.NET Core runtime";
                errorContext.errorReason = "The specified version of Microsoft.NetCore.App or Microsoft.AspNetCore.App was not found.";

                EventLog::Error(
                    ASPNETCORE_EVENT_GENERAL_ERROR,
                    ASPNETCORE_EVENT_HOSTFXR_FAILURE_MSG
                );

                return E_UNEXPECTED;
            }
        }
    }

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
        // This only occurs if the request handler isn't referenced by the app, which rarely happens if they are targeting the shared framework.
        errorContext.statusCode = 500i16;
        errorContext.subStatusCode = 33i16;
        errorContext.generalErrorType = "Failed to load ASP.NET Core request handler";
        errorContext.detailedErrorContent = to_multi_byte_string(format(ASPNETCORE_EVENT_INPROCESS_RH_REFERENCE_MSG, handlerDllPath.empty()
                ? s_pwzAspnetcoreInProcessRequestHandlerName
                : handlerDllPath.c_str()),
            CP_UTF8);
        errorContext.errorReason = "Make sure Microsoft.AspNetCore.App is referenced by your application.";

        EventLog::Error(
            ASPNETCORE_EVENT_GENERAL_ERROR,
            ASPNETCORE_EVENT_INPROCESS_RH_REFERENCE_MSG,
            handlerDllPath.empty() ? s_pwzAspnetcoreInProcessRequestHandlerName : handlerDllPath.c_str());
        return HRESULT_FROM_WIN32(ERROR_DLL_NOT_FOUND);
    }

    return S_OK;
}
CATCH_RETURN()
