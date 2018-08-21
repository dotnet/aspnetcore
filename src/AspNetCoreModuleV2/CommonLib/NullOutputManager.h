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
        return S_OK;
    }

    HRESULT Stop()
    {
        return S_OK;
    }

    bool GetStdOutContent(STRA*)
    {
        return false;
    }
};

