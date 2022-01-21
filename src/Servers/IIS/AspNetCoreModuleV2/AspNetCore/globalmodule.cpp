// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "globalmodule.h"

extern BOOL         g_fInShutdown;

ASPNET_CORE_GLOBAL_MODULE::ASPNET_CORE_GLOBAL_MODULE(std::shared_ptr<APPLICATION_MANAGER> pApplicationManager) noexcept
    :m_pApplicationManager(std::move(pApplicationManager))
{
}

//
// Is called when IIS decided to terminate worker process
// Shut down all core apps
//
GLOBAL_NOTIFICATION_STATUS
ASPNET_CORE_GLOBAL_MODULE::OnGlobalStopListening(
    _In_ IGlobalStopListeningProvider * pProvider
)
{
    UNREFERENCED_PARAMETER(pProvider);

    LOG_INFO(L"ASPNET_CORE_GLOBAL_MODULE::OnGlobalStopListening");

    if (g_fInShutdown)
    {
        // Avoid receiving two shutdown notifications.
        return GL_NOTIFICATION_CONTINUE;
    }

    m_pApplicationManager->ShutDown();
    m_pApplicationManager = nullptr;

    // Return processing to the pipeline.
    return GL_NOTIFICATION_CONTINUE;
}

//
// Is called when configuration changed
// Recycled the corresponding core app if its configuration changed
//
GLOBAL_NOTIFICATION_STATUS
ASPNET_CORE_GLOBAL_MODULE::OnGlobalConfigurationChange(
    _In_ IGlobalConfigurationChangeProvider * pProvider
)
{
    if (g_fInShutdown)
    {
        return GL_NOTIFICATION_CONTINUE;
    }
    // Retrieve the path that has changed.
    PCWSTR pwszChangePath = pProvider->GetChangePath();

    LOG_INFOF(L"ASPNET_CORE_GLOBAL_MODULE::OnGlobalConfigurationChange '%ls'", pwszChangePath);

    // Test for an error.
    if (nullptr != pwszChangePath &&
        _wcsicmp(pwszChangePath, L"MACHINE") != 0 &&
        _wcsicmp(pwszChangePath, L"MACHINE/WEBROOT") != 0)
    {
        // Configuration change recycling behavior can be turned off via setting disallowRotationOnConfigChange=true on the handler settings section.
        // We need this duplicate setting because the global module is unable to read the app settings disallowRotationOnConfigChange value.
        if (m_pApplicationManager && m_pApplicationManager->ShouldRecycleOnConfigChange())
        {
            m_pApplicationManager->RecycleApplicationFromManager(pwszChangePath);   
        }
    }

    // Return processing to the pipeline.
    return GL_NOTIFICATION_CONTINUE;
}
