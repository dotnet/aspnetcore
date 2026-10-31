// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>

class ConfigurationLoadException: public std::runtime_error
{
    public:
        ConfigurationLoadException(std::wstring msg)
            : runtime_error("Configuration load exception has occurred"), message(std::move(msg))
        {
        }

        std::wstring get_message() const { return message; }

    private:
        std::wstring message;
};
