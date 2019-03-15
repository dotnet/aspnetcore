// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "cpprest/details/basic_types.h"
#include "websocket_client.h"
#include "web_request_factory.h"
#include "http_client.h"

std::string remove_date_from_log_entry(const std::string &log_entry);

std::shared_ptr<signalr::websocket_client> create_test_websocket_client(
    std::function<void(std::function<void(std::string, std::exception_ptr)>)> receive_function = [](std::function<void(std::string, std::exception_ptr)> callback) { callback("", nullptr); },
    std::function<void(const std::string& msg, std::function<void(std::exception_ptr)>)> send_function = [](const std::string&, std::function<void(std::exception_ptr)> callback) { callback(nullptr); },
    std::function<void(const std::string&, std::function<void(std::exception_ptr)>)> connect_function = [](const std::string&, std::function<void(std::exception_ptr)> callback) { callback(nullptr); },
    std::function<void(std::function<void(std::exception_ptr)>)> close_function = [](std::function<void(std::exception_ptr)> callback) { callback(nullptr); });

std::unique_ptr<signalr::web_request_factory> create_test_web_request_factory();
std::unique_ptr<signalr::http_client> create_test_http_client();
std::string create_uri();
std::string create_uri(const std::string& query_string);
std::vector<std::string> filter_vector(const std::vector<std::string>& source, const std::string& string);
std::string dump_vector(const std::vector<std::string>& source);
