// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "pplx/pplxtasks.h"
#include "signalrclient/transport_type.h"
#include "logger.h"

namespace signalr
{
    class transport
    {
    public:
        virtual transport_type get_transport_type() const = 0;

        virtual ~transport();

        virtual void start(const std::string& url, /*format,*/ std::function<void(std::exception_ptr)> callback) = 0;
        virtual void stop(/*format,*/ std::function<void(std::exception_ptr)> callback) = 0;
        virtual void on_close(std::function<void(std::exception_ptr)> callback) = 0;

        virtual void send(std::string payload, std::function<void(std::exception_ptr)> callback) = 0;

        void on_receive(std::function<void(std::string, std::exception_ptr)> callback);

    protected:
        transport(const logger& logger);

        void process_response(std::string message);
        void process_response(std::exception_ptr exception);

        logger m_logger;

    private:
        std::function<void(std::string, std::exception_ptr)> m_process_response_callback;

        std::function<void(const std::exception&)> m_error_callback;
    };
}
