// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "pplx/pplxtasks.h"
#include "cpprest/base_uri.h"
#include "signalrclient/transport_type.h"
#include "logger.h"

namespace signalr
{
    class transport
    {
    public:
        virtual pplx::task<void> connect(const web::uri &url) = 0;

        virtual pplx::task<void> send(const utility::string_t &data) = 0;

        virtual pplx::task<void> disconnect() = 0;

        virtual transport_type get_transport_type() const = 0;

        virtual ~transport();

    protected:
        transport(const logger& logger, const std::function<void(const utility::string_t &)>& process_response_callback,
            std::function<void(const std::exception&)> error_callback);

        void process_response(const utility::string_t &message);
        void error(const std::exception &e);

        logger m_logger;

    private:
        std::function<void(const utility::string_t &)> m_process_response_callback;

        std::function<void(const std::exception&)> m_error_callback;
    };
}