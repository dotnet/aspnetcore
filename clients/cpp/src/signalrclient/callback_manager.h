// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <atomic>
#include <unordered_map>
#include <functional>
#include <mutex>
#include "cpprest/json.h"

namespace signalr
{
    class callback_manager
    {
    public:
        explicit callback_manager(const web::json::value& dtor_error);
        ~callback_manager();

        callback_manager(const callback_manager&) = delete;
        callback_manager& operator=(const callback_manager&) = delete;

        utility::string_t register_callback(const std::function<void(const web::json::value&)>& callback);
        bool invoke_callback(const utility::string_t& callback_id, const web::json::value& arguments, bool remove_callback);
        bool remove_callback(const utility::string_t& callback_id);
        void clear(const web::json::value& arguments);

    private:
        std::atomic<int> m_id { 0 };
        std::unordered_map<utility::string_t, std::function<void(const web::json::value&)>> m_callbacks;
        std::mutex m_map_lock;
        const web::json::value m_dtor_clear_arguments;

        utility::string_t get_callback_id();
    };
}
