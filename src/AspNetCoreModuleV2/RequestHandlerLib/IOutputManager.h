// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

#pragma once
class IOutputManager
{
public:
    virtual
    HRESULT
    Start() = 0;

    virtual
    ~IOutputManager() {};

    virtual
    bool
    GetStdOutContent(STRA* struStdOutput) = 0;

    virtual
    HRESULT
    Stop() = 0;
};

