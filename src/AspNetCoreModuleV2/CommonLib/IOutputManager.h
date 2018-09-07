// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"
#include "stringa.h"

class IOutputManager
{
public:
    virtual
    void
    Start() = 0;

    virtual
    ~IOutputManager() = default;

    virtual
    std::wstring
    GetStdOutContent() = 0;

    virtual
    void
    Stop() = 0;
};

