// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <stdexcept>
#include "cpprest/details/basic_types.h"
#include "cpprest/asyncrt_utils.h"

namespace signalr
{
    class web_exception : public std::runtime_error
    {
    public:
        web_exception(const utility::string_t &what, unsigned short status_code)
            : runtime_error(utility::conversions::to_utf8string(what)), m_status_code(status_code)
        {}

        unsigned short status_code() const
        {
            return m_status_code;
        }

    private:
        unsigned short m_status_code;
    };
}