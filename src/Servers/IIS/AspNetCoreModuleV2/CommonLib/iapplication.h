// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <memory>
#include "irequesthandler.h"

struct APPLICATION_PARAMETER
{
    LPCSTR pzName;
    void *pValue;
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
    VOID
    ReferenceApplication() = 0;

    virtual
    VOID
    DereferenceApplication() = 0;

    virtual
    HRESULT
    TryCreateHandler(
        _In_  IHttpContext       *pHttpContext,
        _Outptr_ IREQUEST_HANDLER  **pRequestHandler) = 0;
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

template< class APPLICATION, typename ...Params >
std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER> make_application(Params&&... params)
{
#pragma warning( push )
#pragma warning ( disable : 26409 ) // Disable "Avoid using new", using custom deleter here
    return std::unique_ptr<IAPPLICATION, IAPPLICATION_DELETER>(new APPLICATION(std::forward<Params>(params)...));
#pragma warning( pop )
}

template< class TYPE >
TYPE FindParameter(LPCSTR sRequiredParameter, APPLICATION_PARAMETER *pParameters, DWORD nParameters)
{
    for (DWORD i = 0; i < nParameters; i++)
    {
        if (_stricmp(pParameters[i].pzName, sRequiredParameter) == 0)
        {
            return reinterpret_cast<TYPE>(pParameters[i].pValue);
        }
    }
    return nullptr;
}
