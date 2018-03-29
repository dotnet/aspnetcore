// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <functional>
#include <cctype>
#include "cpprest/details/basic_types.h"

namespace signalr
{
    // Note: These functions are not pretending to be all-purpose helpers for case insensitive string comparison. Rather
    // we use them to compare hub and hub method names which are expected to be almost exclusively ASCII and this is the
    // simplest thing that would work without having to take dependencies on third party libraries.
    struct case_insensitive_equals : std::binary_function<utility::string_t, utility::string_t, bool>
    {
        bool operator()(const utility::string_t& s1, const utility::string_t& s2) const
        {
            if (s1.length() != s2.length())
            {
                return false;
            }

            for (auto s1_iterator = s1.begin(), s2_iterator = s2.begin(); s1_iterator != s1.end(); ++s1_iterator, ++s2_iterator)
            {
                if (std::toupper(*s1_iterator) != std::toupper(*s2_iterator))
                {
                    return false;
                }
            }

            return true;
        }
    };

    struct case_insensitive_hash : std::unary_function<utility::string_t, std::size_t>
    {
        std::size_t operator()(const utility::string_t& s) const
        {
            size_t hash = 0;
            std::hash<size_t> hasher;
            for (const utility::char_t& c : s)
            {
                hash ^= hasher(std::toupper(c)) + (hash << 5) + (hash >> 2);
            }

            return hash;
        }
    };
}