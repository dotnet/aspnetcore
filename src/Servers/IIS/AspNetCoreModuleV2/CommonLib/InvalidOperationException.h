// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include <string>

class InvalidOperationException: public std::runtime_error
{
    public:
       InvalidOperationException(std::wstring msg)
            : runtime_error("InvalidOperationException"), message(std::move(msg))
        {
        }

        std::wstring as_wstring() const
        {
            return message;
        }

    private:
        std::wstring message;
};
