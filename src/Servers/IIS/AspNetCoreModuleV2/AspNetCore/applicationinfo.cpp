// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationinfo.h"

#include "proxymodule.h"
#include "HostFxrResolver.h"
#include "debugutil.h"
#include "resources.h"
#include "SRWExclusiveLock.h"
#include "exceptions.h"
#include "EventLog.h"
#include "ServerErrorApplication.h"
#include "AppOfflineApplication.h"
#include "WebConfigConfigurationSource.h"
#include "ConfigurationLoadException.h"
#include "resource.h"
#include "file_utility.h"
#include "ModuleEnvironment.h"

extern HINSTANCE           g_hServerModule;
extern BOOL                g_fInAppOfflineShutdown;

HRESULT
APPLICATION_INFO::CreateHandler(
    IHttpContext& pHttpContext,
    std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER>& pHandler
)
{
    HRESULT             hr = S_OK;

    {
        SRWSharedLock lock(m_applicationLock);

        RETURN_IF_FAILED(hr = TryCreateHandler(pHttpContext, pHandler));

        if (hr == S_OK)
        {
            return S_OK;
        }
    }

    {
        SRWExclusiveLock lock(m_applicationLock);

        // check if other thread created application
        RETURN_IF_FAILED(hr = TryCreateHandler(pHttpContext, pHandler));

        // In some cases (adding and removing app_offline quickly) application might start and stop immediately
        // so retry until we get valid handler or error
        while (hr != S_OK)
        {
            // At this point application is either null or shutdown and is returning S_FALSE
            if (m_pApplication != nullptr)
            {
                LOG_INFO(L"Application went offline");

                // Call to wait for application to complete stopping
                m_pApplication->Stop(/* fServerInitiated */ false);
                m_pApplication = nullptr;
                m_pApplicationFactory = nullptr;
            }

            RETURN_IF_FAILED(CreateApplication(pHttpContext));

            RETURN_IF_FAILED(hr = TryCreateHandler(pHttpContext, pHandler));
        }
    }

    return S_OK;
}

HRESULT
APPLICATION_INFO::CreateApplication(IHttpContext& pHttpContext)
{
    SetApplicationEnvironmentVariables(m_pServer, pHttpContext);

    auto& pHttpApplication = *pHttpContext.GetApplication();
    if (AppOfflineApplication::ShouldBeStarted(pHttpApplication))
    {
        LOG_INFO(L"Detected app_offline file, creating polling application");
        m_pApplication = make_application<AppOfflineApplication>(pHttpApplication);

        return S_OK;
    }

    std::wstring pExceptionMessage;

    try
    {
        const WebConfigConfigurationSource configurationSource(m_pServer.GetAdminManager(), pHttpApplication);
        ShimOptions options(configurationSource);

        if (g_fInAppOfflineShutdown)
        {
            m_pApplication = make_application<ServerErrorApplication>(
                pHttpApplication,
                E_FAIL,
                options.QueryDisableStartupPage() /* disableStartupPage */,
                "" /* responseContent */,
                503i16 /* statusCode */,
                0i16 /* subStatusCode */,
                "Application Shutting Down");
            return S_OK;
        }

        ErrorContext errorContext;
        errorContext.statusCode = 500i16;
        errorContext.subStatusCode = 0i16;

        const auto hr = TryCreateApplication(pHttpContext, options, errorContext);

        if (FAILED_LOG(hr))
        {
            EventLog::Error(
                ASPNETCORE_EVENT_ADD_APPLICATION_ERROR,
                ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG,
                pHttpApplication.GetApplicationId(),
                hr);

            auto page = ANCM_ERROR_PAGE;
            std::string responseContent;
            if (options.QueryShowDetailedErrors())
            {
                responseContent = FILE_UTILITY::GetHtml(g_hServerModule, page, errorContext.statusCode, errorContext.subStatusCode, errorContext.generalErrorType, errorContext.errorReason, errorContext.detailedErrorContent);
            }
            else
            {
                responseContent = FILE_UTILITY::GetHtml(g_hServerModule, page, errorContext.statusCode, errorContext.subStatusCode, errorContext.generalErrorType, errorContext.errorReason);
            }

            m_pApplication = make_application<ServerErrorApplication>(
                pHttpApplication,
                hr,
                options.QueryDisableStartupPage(),
                responseContent,
                errorContext.statusCode,
                errorContext.subStatusCode,
                "Internal Server Error");
        }
        return S_OK;
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
        pExceptionMessage = CaughtExceptionToString();
        EventLog::Error(
            ASPNETCORE_CONFIGURATION_LOAD_ERROR,
            ASPNETCORE_CONFIGURATION_LOAD_ERROR_MSG,
            pExceptionMessage.c_str());
    }

    ErrorContext errorContext;
    errorContext.statusCode = 500i16;
    errorContext.subStatusCode = 30i16;
    errorContext.generalErrorType = "ASP.NET Core app failed to start - An exception was thrown during startup";
    if (GetLastError() == ERROR_ACCESS_DENIED)
    {
        errorContext.errorReason = "Ensure the application pool process model has write permissions to the shadow copy directory";
    }
    // TODO: Depend on show detailed errors or nah?
    errorContext.detailedErrorContent = to_multi_byte_string(pExceptionMessage, CP_UTF8);
    auto page = ANCM_ERROR_PAGE;


    auto responseContent = FILE_UTILITY::GetHtml(g_hServerModule,
                                                 page,
                                                 errorContext.statusCode,
                                                 errorContext.subStatusCode,
                                                 errorContext.generalErrorType,
                                                 errorContext.errorReason,
                                                 errorContext.detailedErrorContent);
    m_pApplication = make_application<ServerErrorApplication>(
        pHttpApplication,
        E_FAIL,
        false /* disableStartupPage */,
        responseContent /* responseContent */,
        errorContext.statusCode /* statusCode */,
        errorContext.subStatusCode/* subStatusCode */,
        "Internal Server Error");

    return S_OK;
}

HRESULT
APPLICATION_INFO::TryCreateApplication(IHttpContext& pHttpContext, const ShimOptions& options, ErrorContext& error)
{
    const auto startupEvent = Environment::GetEnvironmentVariableValue(L"ASPNETCORE_STARTUP_SUSPEND_EVENT");
    if (startupEvent.has_value())
    {
        LOG_INFOF(L"Startup suspend event %ls", startupEvent.value().c_str());

        HandleWrapper<NullHandleTraits> eventHandle = OpenEvent(SYNCHRONIZE, false, startupEvent.value().c_str());

        if (eventHandle == nullptr)
        {
            LOG_INFOF(L"Unable to open startup suspend event");
        }
        else
        {
            auto const suspendedEventName = startupEvent.value() + L"_suspended";

            HandleWrapper<NullHandleTraits> suspendedEventHandle = OpenEvent(EVENT_MODIFY_STATE, false, suspendedEventName.c_str());
            if (suspendedEventHandle != nullptr)
            {
                LOG_LAST_ERROR_IF(!SetEvent(suspendedEventHandle));
            }
            LOG_LAST_ERROR_IF(WaitForSingleObject(eventHandle, INFINITE) != WAIT_OBJECT_0);
        }
    }

    std::filesystem::path shadowCopyPath;

    // Only support shadow copying for IIS.
    if (options.QueryShadowCopyEnabled() && !m_pServer.IsCommandLineLaunch())
    {
        try
        {
            shadowCopyPath = HandleShadowCopy(options, pHttpContext, error);
        }
        catch (...)
        {
            OBSERVE_CAUGHT_EXCEPTION();
            throw;
        }

        if (shadowCopyPath.empty())
        {
            return E_FAIL;
        }
    }

    RETURN_IF_FAILED(m_handlerResolver.GetApplicationFactory(*pHttpContext.GetApplication(), shadowCopyPath, m_pApplicationFactory, options, error));
    LOG_INFO(L"Creating handler application");

    IAPPLICATION * newApplication;
    std::wstring shadowCopyWstring = shadowCopyPath.wstring();
    RETURN_IF_FAILED(m_pApplicationFactory->Execute(
        &m_pServer,
        &pHttpContext,
        shadowCopyWstring,
        &newApplication));

    m_pApplication.reset(newApplication);
    return S_OK;
}

HRESULT
APPLICATION_INFO::TryCreateHandler(
    IHttpContext& pHttpContext,
    std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER>& pHandler) const
{
    if (m_pApplication != nullptr)
    {
        IREQUEST_HANDLER * newHandler;
        const auto result = m_pApplication->TryCreateHandler(&pHttpContext, &newHandler);
        RETURN_IF_FAILED(result);

        if (result == S_OK)
        {
            pHandler.reset(newHandler);
            // another thread created the application
            return S_OK;
        }
    }

    return S_FALSE;
}

VOID
APPLICATION_INFO::ShutDownApplication(const bool fServerInitiated)
{
    IAPPLICATION* app = nullptr;
    {
        SRWExclusiveLock lock(m_applicationLock);
        if (!m_pApplication)
        {
            return;
        }
        app = m_pApplication.get();

        LOG_INFOF(L"Stopping application '%ls'", QueryApplicationInfoKey().c_str());
        app->Stop(fServerInitiated);

        // do not set to null before app->Stop, it can cause issues with the file watching thread trying to join itself
        // because it was referencing the last instance of the app and the shared_ptr would run the destructor inline.
        m_pApplication = nullptr;
        m_pApplicationFactory = nullptr;
    }
}

/* Overview of shadow copy logic when enabled. See https://github.com/dotnet/aspnetcore/pull/28357 for more context

 * On first request, ANCM goes through its startup sequence, starting dotnet and sending the request into managed code. During this sequence,
 * ANCM will copy the contents of the app directory to another directory which is user specified. The path to this directory can be absolute or relative.

 * Logs and log files will be written to the app directory rather than the shadow copy directory. app_offline will also only be watched in the app directory.
 * The current directory will be set to the app directory as well as the AppContext.BaseDirectory.

 * On publish of new content to the app directory, ANCM will start debouncing file change notifications for dlls, waiting for a steady state.
 * This is done by resetting a timer each time a dll is changed, eventually triggering the timer once there are no dll changes. Afterwards, shutdown is started,
 * causing the process to recycle.

 * Subfolders are created under the user specified shadowCopyDirectory, where the highest int value directory name will be used each time.
 * It will start at subdirectory with name '0' and increment from there. On shutdown, because dlls are still locked by the running process,
 * we need to copy dlls to a different directory than what is currently running in the app. So in the case where the directory name is '0',
 * we will create a directory name '1' and write the contents there. Then on app start, it will pick the directory name '1' as it's the highest value.

 * Other directories in the shadow copy directory will be cleaned up as well. Following the example, after '1' has been selected as the directory to use,
 * we will start a thread that deletes all other folders in that directory.
 */
std::filesystem::path
APPLICATION_INFO::HandleShadowCopy(const ShimOptions& options, IHttpContext& pHttpContext, ErrorContext& error)
{
    std::filesystem::path shadowCopyPath = options.QueryShadowCopyDirectory();
    std::wstring physicalPath = pHttpContext.GetApplication()->GetApplicationPhysicalPath();

    // Make shadow copy path absolute.
    if (!shadowCopyPath.is_absolute())
    {
        shadowCopyPath = std::filesystem::absolute(std::filesystem::path(physicalPath) / shadowCopyPath);
    }

    // The shadow copy directory itself isn't copied to directly.
    // Instead subdirectories with numerically increasing names are created.
    // This is because on shutdown, the app itself will still have all dlls loaded,
    // meaning we can't copy to the same subdirectory. Therefore, on shutdown,
    // we create a directory that is one larger than the previous largest directory number.
    auto directoryName = 0;
    std::string directoryNameStr = "0";
    auto shadowCopyBaseDirectory = std::filesystem::directory_entry(shadowCopyPath);
    if (!shadowCopyBaseDirectory.exists())
    {
        auto ret = CreateDirectory(shadowCopyBaseDirectory.path().wstring().c_str(), nullptr);
        if (!ret)
        {
            auto pathString = to_multi_byte_string(shadowCopyBaseDirectory.path(), CP_UTF8);
            auto errorCode = std::error_code(GetLastError(), std::system_category());
            std::string errorMessage = format("Failed to create shadow copy base directory %s. Error: %s",
                                              pathString.c_str(),
                                              errorCode.message().c_str());

            // TODO: Better substatus code
            error.statusCode = 500i16;
            error.subStatusCode = 30i16;
            error.generalErrorType = format("ASP.NET Core app failed to start - Failed to copy to shadow copy directory");
            error.errorReason = format("Ensure the application pool process model has write permissions for the shadow copy base directory %s",
                                       pathString.c_str());
            error.detailedErrorContent = errorMessage;
            return std::wstring();
        }
    }

    for (auto& entry : std::filesystem::directory_iterator(shadowCopyPath))
    {
        if (entry.is_directory())
        {
            try
            {
                auto tempDirName = entry.path().filename().string();
                int intFileName = std::stoi(tempDirName);
                if (intFileName > directoryName)
                {
                    directoryName = intFileName;
                    directoryNameStr = tempDirName;
                }
            }
            catch (...)
            {
                OBSERVE_CAUGHT_EXCEPTION();
                // Ignore any folders that can't be converted to an int.
            }
        }
    }

    int copiedFileCount = 0;

    shadowCopyPath = shadowCopyPath / directoryNameStr;
    LOG_INFOF(L"Copying from %ls to shadow copy directory %ls.", physicalPath.c_str(), shadowCopyPath.c_str());

    // Avoid using canonical for shadowCopyBaseDirectory
    // It could expand to a network drive, or an expanded link folder path
    // We already made it an absolute path relative to the physicalPath above
    try {
        // CopyToDirectory will succeed or throw exception, so return value can be ignored
        Environment::CopyToDirectory(physicalPath, shadowCopyPath, options.QueryCleanShadowCopyDirectory(), shadowCopyBaseDirectory.path(), copiedFileCount);
    }
    catch (const std::system_error& ex)
    {
        auto exWideString = to_wide_string(ex.what(), CP_ACP);

        std::wstring logMessage = format(L"Failed to copy files from %s to shadow copy directory %s. Error: %s",
                                         physicalPath.c_str(),
                                         shadowCopyPath.c_str(),
                                         exWideString.c_str());

        LOG_ERRORF(L"%ls", logMessage.c_str());
        EventLog::Error(ASPNETCORE_CONFIGURATION_LOAD_ERROR,
                        ASPNETCORE_CONFIGURATION_LOAD_ERROR_MSG,
                        logMessage.c_str());

        // TODO: Better substatus code
        error.statusCode = 500i16;
        error.subStatusCode = 30i16;

        std::string errorMessage = "Failed to copy to shadow copy directory";
        auto exceptionCode = ex.code().value();
        if (exceptionCode == ERROR_ACCESS_DENIED || exceptionCode == ERROR_PATH_NOT_FOUND)
        {
            errorMessage = "No permissions to shadow copy directory";
        }

        error.generalErrorType = format("ASP.NET Core app failed to start - %s", errorMessage.c_str());
        error.errorReason = format("Ensure the application pool process model has write permissions to the shadow copy directory %ls",
                                   shadowCopyPath.c_str());
        if (options.QueryShowDetailedErrors())
        {
            error.detailedErrorContent = ex.what();
        }
        return std::wstring();
    }

    LOG_INFOF(L"Finished copying %d files to shadow copy directory %ls.", copiedFileCount, shadowCopyBaseDirectory.path().c_str());
    return shadowCopyPath;
}
