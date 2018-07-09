// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "irequesthandler.h"

enum APPLICATION_STATUS
{
    UNKNOWN = 0,
    STARTING,
    RUNNING,
    SHUTDOWN,
    RECYCLED,
    FAIL
};

struct APPLICATION_PARAMETER
{
    LPCSTR       pzName;
    PVOID        pValue;
};

class IAPPLICATION
{
public:

    virtual
    VOID
    ShutDown() = 0;

    virtual
    VOID
    Recycle() = 0;

    virtual
    ~IAPPLICATION() = 0 { };

    virtual
    APPLICATION_STATUS
    QueryStatus() = 0;

    virtual
    VOID
    ReferenceApplication() = 0;

    virtual
    VOID
    DereferenceApplication() = 0;

    virtual
    HRESULT
    CreateHandler(
        _In_  IHttpContext       *pHttpContext,
        _Out_ IREQUEST_HANDLER  **pRequestHandler) = 0;
};
