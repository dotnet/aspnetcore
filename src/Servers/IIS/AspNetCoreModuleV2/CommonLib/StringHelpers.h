// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <string>

[[nodiscard]]
bool ends_with(const std::wstring &source, const std::wstring &suffix, bool ignoreCase = false);

[[nodiscard]]
bool equals_ignore_case(const std::wstring& s1, const std::wstring& s2);

[[nodiscard]]
int compare_ignore_case(const std::wstring& s1, const std::wstring& s2);

[[nodiscard]]
std::wstring to_wide_string(const std::string &source, const unsigned int codePage);

template<typename ... Args>
[[nodiscard]]
std::wstring format(const std::wstring& format, Args ... args)
{
    std::wstring result;
    if (!format.empty())
    {
        const size_t size = swprintf(nullptr, 0, format.c_str(), args ...); // Extra char for '\0'
        result.resize(size);
        if (swprintf(result.data(), result.size() + 1, format.c_str(), args ... ) == -1)
        {
            throw std::system_error(std::error_code(errno, std::system_category()));
        }
    }
    return result;
}

template<typename ... Args>
[[nodiscard]]
std::string format(const std::string& format, Args ... args)
{
    std::string result;
    if (!format.empty())
    {
        const size_t size = snprintf(nullptr, 0, format.c_str(), args ...); // Extra char for '\0'
        result.resize(size);
        if (snprintf(result.data(), result.size() + 1, format.c_str(), args ... ) == -1)
        {
            throw std::system_error(std::error_code(errno, std::system_category()));
        }
    }
    return result;
}

struct ignore_case_comparer
{
    bool operator() (const std::wstring & s1, const std::wstring & s2) const {
        return compare_ignore_case(s1, s2) == -1;
    }
};
