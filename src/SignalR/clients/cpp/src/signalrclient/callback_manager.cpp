// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "callback_manager.h"

namespace signalr
{
    // dtor_clear_arguments will be passed when closing any pending callbacks when the `callback_manager` is
    // destroyed (i.e. in the dtor)
    callback_manager::callback_manager(const web::json::value& dtor_clear_arguments)
        : m_dtor_clear_arguments(dtor_clear_arguments)
    { }

    callback_manager::~callback_manager()
    {
        clear(m_dtor_clear_arguments);
    }

    // note: callback must not throw except for the `on_progress` callback which will never be invoked from the dtor
    utility::string_t callback_manager::register_callback(const std::function<void(const web::json::value&)>& callback)
    {
        auto callback_id = get_callback_id();

        {
            std::lock_guard<std::mutex> lock(m_map_lock);

            m_callbacks.insert(std::make_pair(callback_id, callback));
        }

        return callback_id;
    }


    // invokes a callback and stops tracking it if remove callback set to true
    bool callback_manager::invoke_callback(const utility::string_t& callback_id, const web::json::value& arguments, bool remove_callback)
    {
        std::function<void(const web::json::value& arguments)> callback;

        {
            std::lock_guard<std::mutex> lock(m_map_lock);

            auto iter = m_callbacks.find(callback_id);
            if (iter == m_callbacks.end())
            {
                return false;
            }

            callback = iter->second;

            if (remove_callback)
            {
                m_callbacks.erase(callback_id);
            }
        }

        callback(arguments);
        return true;
    }

    bool callback_manager::remove_callback(const utility::string_t& callback_id)
    {
        {
            std::lock_guard<std::mutex> lock(m_map_lock);

            return m_callbacks.erase(callback_id) != 0;
        }
    }

    void callback_manager::clear(const web::json::value& arguments)
    {
        {
            std::lock_guard<std::mutex> lock(m_map_lock);

            for (auto& kvp : m_callbacks)
            {
                kvp.second(arguments);
            }

            m_callbacks.clear();
        }
    }

    utility::string_t callback_manager::get_callback_id()
    {
        auto callback_id = m_id++;
        utility::stringstream_t ss;
        ss << callback_id;
        return ss.str();
    }
}
