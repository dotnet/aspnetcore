// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/details/basic_types.h"
#include "websocket_client.h"
#include "web_request_factory.h"

std::string remove_date_from_log_entry(const std::string &log_entry);

std::shared_ptr<signalr::websocket_client> create_test_websocket_client(
    std::function<pplx::task<std::string>()> receive_function = [](){ return pplx::task_from_result<std::string>(""); },
    std::function<pplx::task<void>(const std::string& msg)> send_function = [](const std::string&){ return pplx::task_from_result(); },
    std::function<pplx::task<void>(const std::string& url)> connect_function = [](const std::string&){ return pplx::task_from_result(); },
    std::function<pplx::task<void>()> close_function = [](){ return pplx::task_from_result(); });

std::unique_ptr<signalr::web_request_factory> create_test_web_request_factory();
std::string create_uri();
std::string create_uri(const std::string& query_string);
std::vector<std::string> filter_vector(const std::vector<std::string>& source, const std::string& string);
std::string dump_vector(const std::vector<std::string>& source);
