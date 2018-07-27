// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <memory>
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
    Stop(bool fServerInitiated) = 0;

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

struct IAPPLICATION_DELETER
{
    void operator ()(IAPPLICATION* application) const
    {
        application->DereferenceApplication();
    }
};

template< class APPLICATION >
std::unique_ptr<APPLICATION, IAPPLICATION_DELETER> ReferenceApplication(APPLICATION* application)
{
    application->ReferenceApplication();
    return std::unique_ptr<APPLICATION, IAPPLICATION_DELETER>(application);
};
