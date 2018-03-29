// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <assert.h>
#include <condition_variable>
#include <mutex>

namespace signalr
{
    class event
    {
    private:
        std::mutex m_lock;
        std::condition_variable m_condition;
        bool m_signaled;
    public:

        static const unsigned int timeout_infinite = 0xFFFFFFFF;

        event()
            : m_signaled(false)
        {
        }

        void set()
        {
            std::lock_guard<std::mutex> lock(m_lock);
            m_signaled = true;
            m_condition.notify_all();
        }

        void reset()
        {
            std::lock_guard<std::mutex> lock(m_lock);
            m_signaled = false;
        }

        unsigned int wait(unsigned int timeout)
        {
            std::unique_lock<std::mutex> lock(m_lock);
            if (timeout == event::timeout_infinite)
            {
                m_condition.wait(lock, [this]() { return m_signaled; });
                return 0;
            }
            else
            {
                std::chrono::milliseconds period(timeout);
                auto status = m_condition.wait_for(lock, period, [this]() { return m_signaled; });
                assert(status == m_signaled);
                // Return 0 if the wait completed as a result of signaling the event. Otherwise, return timeout_infinite
                return status ? 0 : event::timeout_infinite;
            }
        }

        unsigned int wait()
        {
            return wait(event::timeout_infinite);
        }
    };
}
