// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "applicationmanager.h"

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

private:
    std::shared_ptr<APPLICATION_MANAGER> m_pApplicationManager;
};
