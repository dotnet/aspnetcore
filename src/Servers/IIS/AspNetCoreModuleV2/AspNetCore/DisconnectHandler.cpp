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
        std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER> pHandler;
        {
            SRWExclusiveLock lock(m_handlerLock);
            m_pHandler.swap(pHandler);
            m_disconnectFired = true;
        }

        if (pHandler != nullptr)
        {
            pHandler->NotifyDisconnect();
        }
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
    }
}

void DisconnectHandler::CleanupStoredContext() noexcept
{
    delete this;
}

void DisconnectHandler::SetHandler(std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER> handler)
{
    IREQUEST_HANDLER* pHandler = nullptr;
    {
        SRWExclusiveLock lock(m_handlerLock);

        handler.swap(m_pHandler);
        pHandler = m_pHandler.get();
    }

    assert(pHandler != nullptr);

    if (pHandler != nullptr && (m_disconnectFired || m_pHttpConnection != nullptr && !m_pHttpConnection->IsConnected()))
    {
        pHandler->NotifyDisconnect();
    }
}

void DisconnectHandler::RemoveHandler() noexcept
{
    SRWExclusiveLock lock(m_handlerLock);
    m_pHandler = nullptr;
}
