// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once
#include <atomic>

class ASPNET_CORE_PROXY_MODULE;

class DisconnectHandler final: public IHttpConnectionStoredContext
{
public:
    DisconnectHandler()
        : m_pModule(nullptr)
    {
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
    SetHandler(ASPNET_CORE_PROXY_MODULE * module) noexcept;

private:
    std::atomic<ASPNET_CORE_PROXY_MODULE*> m_pModule;
};

