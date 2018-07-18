// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "applicationmanager.h"

class ASPNET_CORE_GLOBAL_MODULE : public CGlobalModule
{

public:

    ASPNET_CORE_GLOBAL_MODULE(
        APPLICATION_MANAGER* pApplicationManager
    );

    ~ASPNET_CORE_GLOBAL_MODULE()
    {
    }

    VOID Terminate()
    {
        LOG_INFO("ASPNET_CORE_GLOBAL_MODULE::Terminate");
        // Remove the class from memory.
        delete this;
    }

    GLOBAL_NOTIFICATION_STATUS
    OnGlobalStopListening(
        _In_ IGlobalStopListeningProvider * pProvider
    );

    GLOBAL_NOTIFICATION_STATUS
    OnGlobalConfigurationChange(
        _In_ IGlobalConfigurationChangeProvider * pProvider
    );

private:
    APPLICATION_MANAGER * m_pApplicationManager;
};
