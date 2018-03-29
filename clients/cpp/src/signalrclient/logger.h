// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include "signalrclient/trace_level.h"
#include "signalrclient/log_writer.h"

namespace signalr
{
    class logger
    {
    public:
        logger(const std::shared_ptr<log_writer>& log_writer, trace_level trace_level);

        void log(trace_level level, const utility::string_t& entry);

    private:
        std::shared_ptr<log_writer> m_log_writer;
        trace_level m_trace_level;

        static utility::string_t translate_trace_level(trace_level trace_level);
    };
}