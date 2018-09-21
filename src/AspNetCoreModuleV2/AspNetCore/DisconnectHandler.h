// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include "irequesthandler.h"

class ASPNET_CORE_PROXY_MODULE;

class DisconnectHandler final: public IHttpConnectionStoredContext
{
public:
    DisconnectHandler()
        : m_pHandler(nullptr)
    {
        InitializeSRWLock(&m_handlerLock);
    }

    virtual
    ~DisconnectHandler()
    {
        SetHandler(nullptr);
    }

    void
    NotifyDisconnect() override;

    void
    CleanupStoredContext() noexcept override;

    void
    SetHandler(std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER> handler) noexcept;

private:
    SRWLOCK m_handlerLock {};
    std::unique_ptr<IREQUEST_HANDLER, IREQUEST_HANDLER_DELETER> m_pHandler;
};

