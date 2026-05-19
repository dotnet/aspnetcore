// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        OBSERVE_CAUGHT_EXCEPTION()
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
