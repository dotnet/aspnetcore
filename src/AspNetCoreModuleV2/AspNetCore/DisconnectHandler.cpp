// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "DisconnectHandler.h"
#include "exceptions.h"
#include "proxymodule.h"
#include "SRWExclusiveLock.h"

void DisconnectHandler::NotifyDisconnect()
{
    try
    {
        std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER> module;
        {
            SRWExclusiveLock lock(m_handlerLock);
            m_pHandler.swap(module);
        }

        if (module != nullptr)
        {
            module->NotifyDisconnect();
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

void DisconnectHandler::SetHandler(std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER> handler) noexcept
{
    SRWExclusiveLock lock(m_handlerLock);
    handler.swap(m_pHandler);
}
