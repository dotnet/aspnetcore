// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include "irequesthandler.h"

class ASPNET_CORE_PROXY_MODULE;

class DisconnectHandler final: public IHttpConnectionStoredContext
{
public:
    DisconnectHandler(IHttpConnection* pHttpConnection)
        : m_pHandler(nullptr), m_pHttpConnection(pHttpConnection), m_disconnectFired(false)
    {
        InitializeSRWLock(&m_handlerLock);
    }

    virtual
    ~DisconnectHandler()
    {
        RemoveHandler();
    }

    void
    NotifyDisconnect() override;

    void
    CleanupStoredContext() noexcept override;

    void
    SetHandler(std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER> handler);

    void RemoveHandler() noexcept;

private:
    SRWLOCK m_handlerLock {};
    std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER> m_pHandler;
    IHttpConnection* m_pHttpConnection;
    bool m_disconnectFired;
};

