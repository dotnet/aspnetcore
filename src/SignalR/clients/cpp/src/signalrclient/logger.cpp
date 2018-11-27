// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "logger.h"
#include "cpprest/asyncrt_utils.h"
#include <iomanip>

namespace signalr
{
    logger::logger(const std::shared_ptr<log_writer>& log_writer, trace_level trace_level)
        : m_log_writer(log_writer), m_trace_level(trace_level)
    { }

    void logger::log(trace_level level, const utility::string_t& entry)
    {
        if ((level & m_trace_level) != trace_level::none)
        {
            try
            {
                utility::ostringstream_t os;
                os << utility::datetime::utc_now().to_string(utility::datetime::date_format::ISO_8601) << _XPLATSTR(" [")
                    << std::left << std::setw(12) << translate_trace_level(level) << "] "<< entry << std::endl;
                m_log_writer->write(os.str());
            }
            catch (const std::exception &e)
            {
                ucerr << _XPLATSTR("error occurred when logging: ") << utility::conversions::to_string_t(e.what())
                    << std::endl << _XPLATSTR("    entry: ") << entry << std::endl;
            }
            catch (...)
            {
                ucerr << _XPLATSTR("unknown error occurred when logging") << std::endl << _XPLATSTR("    entry: ") << entry << std::endl;
            }
        }
    }

    utility::string_t logger::translate_trace_level(trace_level trace_level)
    {
        switch (trace_level)
        {
        case signalr::trace_level::messages:
            return _XPLATSTR("message");
        case signalr::trace_level::state_changes:
            return _XPLATSTR("state change");
        case signalr::trace_level::events:
            return _XPLATSTR("event");
        case signalr::trace_level::errors:
            return _XPLATSTR("error");
        case signalr::trace_level::info:
            return _XPLATSTR("info");
        default:
            _ASSERTE(false);
            return _XPLATSTR("(unknown)");
        }
    }
}