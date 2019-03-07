// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <stdexcept>

namespace signalr
{
    class web_exception : public std::runtime_error
    {
    public:
        web_exception(const std::string &what, unsigned short status_code)
            : runtime_error(what), m_status_code(status_code)
        {}

        unsigned short status_code() const noexcept
        {
            return m_status_code;
        }

    private:
        unsigned short m_status_code;
    };
}
