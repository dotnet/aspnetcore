// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <string>

[[nodiscard]]
bool ends_with(const std::wstring &source, const std::wstring &suffix, bool ignoreCase = false);

[[nodiscard]]
bool equals_ignore_case(const std::wstring& s1, const std::wstring& s2);

template<typename ... Args>
[[nodiscard]]
std::wstring format(const std::wstring& format, Args ... args)
{
    const size_t size = swprintf(nullptr, 0, format.c_str(), args ...) + 1; // Extra char for '\0'
    std::unique_ptr<wchar_t[]> formattedBuffer(new wchar_t[size]);
    swprintf(formattedBuffer.get(), size, format.c_str(), args ... );
    return std::wstring(formattedBuffer.get(), formattedBuffer.get() + size - 1);
}

template<typename ... Args>
[[nodiscard]]
std::string format(const std::string& format, Args ... args)
{
    const size_t size = snprintf(nullptr, 0, format.c_str(), args ...) + 1; // Extra char for '\0'
    std::unique_ptr<char[]> formattedBuffer(new char[size]);
    snprintf(formattedBuffer.get(), size, format.c_str(), args ... );
    return std::string(formattedBuffer.get(), formattedBuffer.get() + size - 1);
}

