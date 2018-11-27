// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/details/basic_types.h"
#include "websocket_client.h"
#include "web_request_factory.h"

utility::string_t remove_date_from_log_entry(const utility::string_t &log_entry);

std::shared_ptr<signalr::websocket_client> create_test_websocket_client(
    std::function<pplx::task<std::string>()> receive_function = [](){ return pplx::task_from_result<std::string>(""); },
    std::function<pplx::task<void>(const utility::string_t &msg)> send_function = [](const utility::string_t msg){ return pplx::task_from_result(); },
    std::function<pplx::task<void>(const web::uri &url)> connect_function = [](const web::uri &){ return pplx::task_from_result(); },
    std::function<pplx::task<void>()> close_function = [](){ return pplx::task_from_result(); });

std::unique_ptr<signalr::web_request_factory> create_test_web_request_factory();
utility::string_t create_uri();
std::vector<utility::string_t> filter_vector(const std::vector<utility::string_t>& source, const utility::string_t& string);
utility::string_t dump_vector(const std::vector<utility::string_t>& source);