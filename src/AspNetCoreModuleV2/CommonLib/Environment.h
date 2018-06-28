// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>

class Environment
{
public:
    Environment() = delete;
    ~Environment() = delete;

    static
    std::wstring ExpandEnvironmentVariables(const std::wstring & str);
};

