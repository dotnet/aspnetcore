// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"

class NullOutputManager : public BaseOutputManager
{
public:

    NullOutputManager() = default;

    ~NullOutputManager() = default;

    void Start()
    {
    }

    void Stop()
    {
    }

    std::wstring GetStdOutContent()
    {
        return L"";
    }
};

