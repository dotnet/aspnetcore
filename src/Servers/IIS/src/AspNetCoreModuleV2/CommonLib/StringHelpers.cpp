// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "StringHelpers.h"
#include "exceptions.h"

bool ends_with(const std::wstring &source, const std::wstring &suffix, bool ignoreCase)
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
    // MultiByteToWideChar returns 0 on failure, which is also the same return value
    // for empty strings. Preemptive return.
    if (source.length() == 0)
    {
        return L"";
    }

    std::wstring destination;

    int nChars = MultiByteToWideChar(codePage, 0, source.data(), static_cast<int>(source.length()), NULL, 0);
    THROW_LAST_ERROR_IF(nChars == 0);

    destination.resize(nChars);

    nChars = MultiByteToWideChar(codePage, 0, source.data(), static_cast<int>(source.length()), destination.data(), nChars);
    THROW_LAST_ERROR_IF(nChars == 0);

    return destination;
}
