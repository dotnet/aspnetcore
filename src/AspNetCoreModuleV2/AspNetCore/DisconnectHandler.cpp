// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "DisconnectHandler.h"
#include "exceptions.h"
#include "proxymodule.h"

void DisconnectHandler::NotifyDisconnect()
{
    try
    {
        const auto module = m_pModule.exchange(nullptr);
        if (module != nullptr)
        {
            module ->NotifyDisconnect();
        }
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
    }
}

void DisconnectHandler::CleanupStoredContext() noexcept
{
    SetHandler(nullptr);
    delete this;
}

void DisconnectHandler::SetHandler(ASPNET_CORE_PROXY_MODULE * module) noexcept
{
    m_pModule = module;
}
