// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "applicationinfo.h"

#include "rpcdce.h"
#include "Rpc.h"
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

extern HINSTANCE           g_hServerModule;
extern BOOL         g_fInAppOfflineShutdown;

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
    auto& pHttpApplication = *pHttpContext.GetApplication();
    if (AppOfflineApplication::ShouldBeStarted(pHttpApplication))
    {
        LOG_INFO(L"Detected app_offline file, creating polling application");
        m_pApplication = make_application<AppOfflineApplication>(pHttpApplication);

        return S_OK;
    }

    if (g_fInAppOfflineShutdown)
    {
        m_pApplication = make_application<ServerErrorApplication>(
            pHttpApplication,
            E_FAIL,
            false /* disableStartupPage */,
            "" /* responseContent */,
            503i16 /* statusCode */,
            0i16 /* subStatusCode */,
            "Application Shutting Down");
        return S_OK;
    }

    try
    {
        const WebConfigConfigurationSource configurationSource(m_pServer.GetAdminManager(), pHttpApplication);
        ShimOptions options(configurationSource);

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

            auto page = options.QueryHostingModel() == HOSTING_IN_PROCESS ? IN_PROCESS_SHIM_STATIC_HTML : OUT_OF_PROCESS_SHIM_STATIC_HTML;
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
    catch (const ConfigurationLoadException &ex)
    {
        EventLog::Error(
            ASPNETCORE_CONFIGURATION_LOAD_ERROR,
            ASPNETCORE_CONFIGURATION_LOAD_ERROR_MSG,
            ex.get_message().c_str());
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
        EventLog::Error(
            ASPNETCORE_CONFIGURATION_LOAD_ERROR,
            ASPNETCORE_CONFIGURATION_LOAD_ERROR_MSG,
            L"");
    }

    m_pApplication = make_application<ServerErrorApplication>(
        pHttpApplication,
        E_FAIL,
        false /* disableStartupPage */,
        "" /* responseContent */,
        500i16 /* statusCode */,
        0i16 /* subStatusCode */,
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

    std::filesystem::path shadowCopyPath = HandleShadowCopy(options, pHttpContext);

    RETURN_IF_FAILED(m_handlerResolver.GetApplicationFactory(*pHttpContext.GetApplication(), shadowCopyPath, m_pApplicationFactory, options, error));
    LOG_INFO(L"Creating handler application");

    IAPPLICATION * newApplication;
    RETURN_IF_FAILED(m_pApplicationFactory->Execute(
        &m_pServer,
        &pHttpContext,
        shadowCopyPath,
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
    }
   
    LOG_INFOF(L"Stopping application '%ls'", QueryApplicationInfoKey().c_str());
    app->Stop(fServerInitiated);
    LOG_INFO(L"Setting app to null");

    SRWExclusiveLock lock(m_applicationLock);
    LOG_INFO(L"lock acquired");

    m_pApplication = nullptr;
    m_pApplicationFactory = nullptr;
}

std::wstring
APPLICATION_INFO::HandleShadowCopy(const ShimOptions& options, IHttpContext& pHttpContext)
{
    std::filesystem::path shadowCopyPath;

    if (options.QueryShadowCopyEnabled())
    {
        shadowCopyPath = options.QueryShadowCopyDirectory();
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
            CreateDirectory(shadowCopyBaseDirectory.path().wstring().c_str(), NULL);
        }

        for (auto& entry : std::filesystem::directory_iterator(shadowCopyPath))
        {
            if (entry.is_directory())
            {
                try
                {
                    std::string::size_type size;
                    int intFileName = std::stoi(entry.path().filename().string(), &size);
                    if (intFileName > directoryName)
                    {
                        directoryName = intFileName;
                        directoryNameStr = std::string(entry.path().string());
                    }
                }
                catch (...)
                {
                    OBSERVE_CAUGHT_EXCEPTION();
                    // Ignore any folders that can't be converted to an int.
                }
            }
        }

        shadowCopyPath = shadowCopyPath / std::filesystem::path(directoryNameStr);
        HRESULT hr = Environment::CopyToDirectory(physicalPath, shadowCopyPath, options.QueryCleanShadowCopyDirectory(), shadowCopyBaseDirectory.path().parent_path());
        if (hr != S_OK)
        {
            return std::wstring();
        }
    }

    return shadowCopyPath;
}
