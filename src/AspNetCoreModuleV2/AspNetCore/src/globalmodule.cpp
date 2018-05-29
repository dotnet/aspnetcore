// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "globalmodule.h"

ASPNET_CORE_GLOBAL_MODULE::ASPNET_CORE_GLOBAL_MODULE(
    APPLICATION_MANAGER* pApplicationManager)
{
    m_pApplicationManager = pApplicationManager;
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

    if (g_fInShutdown)
    {
        // Avoid receiving two shutudown notifications.
        return GL_NOTIFICATION_CONTINUE;
    }

    DBG_ASSERT(m_pApplicationManager);
    // we should let application manager to shutdown all allication
    // and dereference it as some requests may still reference to application manager
    m_pApplicationManager->ShutDown();
    m_pApplicationManager = NULL;

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

    // Test for an error.
    if (NULL != pwszChangePath &&
        _wcsicmp(pwszChangePath, L"MACHINE") != 0 &&
        _wcsicmp(pwszChangePath, L"MACHINE/WEBROOT") != 0)
    {
        if (m_pApplicationManager != NULL)
        {
            m_pApplicationManager->RecycleApplicationFromManager(pwszChangePath);
        }
    }

    // Return processing to the pipeline.
    return GL_NOTIFICATION_CONTINUE;
}
