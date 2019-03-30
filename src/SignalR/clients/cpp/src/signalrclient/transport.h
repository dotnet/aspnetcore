// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "pplx/pplxtasks.h"
#include "signalrclient/transport_type.h"
#include "signalrclient/transfer_format.h"
#include "logger.h"

namespace signalr
{
    class transport
    {
    public:
        virtual transport_type get_transport_type() const = 0;

        virtual ~transport();

        virtual void start(const std::string& url, transfer_format format, std::function<void(std::exception_ptr)> callback) noexcept = 0;
        virtual void stop(std::function<void(std::exception_ptr)> callback) noexcept = 0;
        virtual void on_close(std::function<void(std::exception_ptr)> callback) = 0;

        virtual void send(std::string payload, std::function<void(std::exception_ptr)> callback) noexcept = 0;

        virtual void on_receive(std::function<void(std::string, std::exception_ptr)> callback) = 0;

    protected:
        transport(const logger& logger);

        logger m_logger;
    };
}
