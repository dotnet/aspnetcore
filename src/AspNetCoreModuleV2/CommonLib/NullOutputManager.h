// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"

class NullOutputManager : public IOutputManager
{
public:

    NullOutputManager() = default;

    ~NullOutputManager() = default;

    HRESULT Start()
    {
        // The process has console, e.g., IIS Express scenario
        return S_OK;
    }

    void NotifyStartupComplete()
    {
    }

    bool GetStdOutContent(STRA*)
    {
        return false;
    }
};

