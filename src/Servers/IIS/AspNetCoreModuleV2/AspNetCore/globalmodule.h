// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "applicationmanager.h"
#include <thread>

class ASPNET_CORE_GLOBAL_MODULE : NonCopyable, public CGlobalModule
{

public:

    ASPNET_CORE_GLOBAL_MODULE(
        std::shared_ptr<APPLICATION_MANAGER> pApplicationManager
    ) noexcept;

    virtual ~ASPNET_CORE_GLOBAL_MODULE() = default;

    VOID Terminate() override
    {
        LOG_INFO(L"ASPNET_CORE_GLOBAL_MODULE::Terminate");

        if (m_shutdown.joinable())
        {
            m_shutdown.join();
        }

        // Remove the class from memory.
        delete this;
    }

    GLOBAL_NOTIFICATION_STATUS
    OnGlobalStopListening(
        _In_ IGlobalStopListeningProvider * pProvider
    ) override;

    GLOBAL_NOTIFICATION_STATUS
    OnGlobalConfigurationChange(
        _In_ IGlobalConfigurationChangeProvider * pProvider
    ) override;

    GLOBAL_NOTIFICATION_STATUS
    OnGlobalApplicationStop(
        IN IHttpApplicationStopProvider* pProvider
    ) override;

private:
    std::shared_ptr<APPLICATION_MANAGER> m_pApplicationManager;
    std::thread m_shutdown;

    void StartShutdown()
    {
        // Shutdown has already been started
        if (m_shutdown.joinable())
        {
            return;
        }

        // Run shutdown on a background thread. It seems like IIS keeps giving us requests if OnGlobalStopListening is still running
        // which will result in 503s since we're shutting down.
        // But if we return ASAP from OnGlobalStopListening (by not shutting down inline),
        // IIS will actually stop giving us new requests and queue them instead for processing by the new app process.
        m_shutdown = std::thread([this]()
            {
                auto delay = m_pApplicationManager->GetShutdownDelay();
                LOG_INFOF(L"Shutdown starting in %d ms.", delay.count());
                // Delay so that any incoming requests while we're returning from OnGlobalStopListening are allowed to be processed
                std::this_thread::sleep_for(delay);
                m_pApplicationManager->ShutDown();
                m_pApplicationManager = nullptr;
            });
    }
};
