// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "StringHelpers.h"
#include "exceptions.h"

bool endsWith(const std::wstring& source, const std::wstring& suffix, bool ignoreCase)
{
    if (source.length() < suffix.length())
    {
        return false;
    }

    const auto offset = source.length() - suffix.length();
    return CSTR_EQUAL == CompareStringOrdinal(source.c_str() + offset, static_cast<int>(suffix.length()), suffix.c_str(), static_cast<int>(suffix.length()), ignoreCase);
}

bool equals_ignore_case(const std::wstring& s1, const std::wstring& s2)
{
    return compare_ignore_case(s1, s2) == 0;
}

int compare_ignore_case(const std::wstring& s1, const std::wstring& s2)
{
    return CompareStringOrdinal(s1.c_str(), static_cast<int>(s1.length()), s2.c_str(), static_cast<int>(s2.length()), true) - CSTR_EQUAL;
}

std::wstring to_wide_string(const std::string &source, const unsigned int codePage)
{
    return to_wide_string(source, static_cast<int>(source.length()), codePage);
}

std::wstring to_wide_string(const std::string& source, const int length, const unsigned int codePage)
{
    // MultiByteToWideChar returns 0 on failure, which is also the same return value
    // for empty strings. Preemptive return.
    if (length == 0)
    {
        return L"";
    }

    std::wstring destination;

    int nChars = MultiByteToWideChar(codePage, 0, source.data(), length, nullptr, 0);
    THROW_LAST_ERROR_IF(nChars == 0);

    destination.resize(nChars);

    nChars = MultiByteToWideChar(codePage, 0, source.data(), length, destination.data(), nChars);
    THROW_LAST_ERROR_IF(nChars == 0);

    return destination;
}

std::string to_multi_byte_string(const std::wstring& text, const unsigned int codePage)
{
    auto const encodedByteCount = WideCharToMultiByte(codePage, 0, text.data(), -1, nullptr, 0, nullptr, nullptr);

    std::string encodedBytes;
    encodedBytes.resize(encodedByteCount);
    WideCharToMultiByte(codePage, 0, text.data(), -1, encodedBytes.data(), encodedByteCount, nullptr, nullptr);
    encodedBytes.resize(encodedByteCount - 1);
    return encodedBytes;
}
