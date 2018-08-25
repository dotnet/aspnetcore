// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "StringHelpers.h"

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
    return CSTR_EQUAL == CompareStringOrdinal(s1.c_str(), static_cast<int>(s1.length()), s2.c_str(), static_cast<int>(s2.length()), true);
}
