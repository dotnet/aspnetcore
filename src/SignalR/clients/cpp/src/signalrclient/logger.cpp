// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "logger.h"
#include "cpprest/asyncrt_utils.h"
#include <iomanip>

namespace signalr
{
    logger::logger(const std::shared_ptr<log_writer>& log_writer, trace_level trace_level) noexcept
        : m_log_writer(log_writer), m_trace_level(trace_level)
    { }

    void logger::log(trace_level level, const std::string& entry) const
    {
        if ((level & m_trace_level) != trace_level::none)
        {
            try
            {
                std::stringstream os;
                os << utility::conversions::to_utf8string(utility::datetime::utc_now().to_string(utility::datetime::date_format::ISO_8601)) << " ["
                    << std::left << std::setw(12) << translate_trace_level(level) << "] "<< entry << std::endl;
                m_log_writer->write(os.str());
            }
            catch (const std::exception &e)
            {
                std::cerr << "error occurred when logging: " << e.what()
                    << std::endl << "    entry: " << entry << std::endl;
            }
            catch (...)
            {
                std::cerr << "unknown error occurred when logging" << std::endl << "    entry: " << entry << std::endl;
            }
        }
    }

    std::string logger::translate_trace_level(trace_level trace_level)
    {
        switch (trace_level)
        {
        case signalr::trace_level::messages:
            return "message";
        case signalr::trace_level::state_changes:
            return "state change";
        case signalr::trace_level::events:
            return "event";
        case signalr::trace_level::errors:
            return "error";
        case signalr::trace_level::info:
            return "info";
        default:
            _ASSERTE(false);
            return "(unknown)";
        }
    }
}
