// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <array>
#include <string>
#include <utility>
#include "iapplication.h"
#include "HandleWrapper.h"

typedef
HRESULT
(WINAPI * PFN_ASPNETCORE_CREATE_APPLICATION)(
    _In_  IHttpServer           *pServer,
    _In_  const IHttpApplication * pHttpApplication,
    _In_  APPLICATION_PARAMETER *pParameters,
    _In_  DWORD                  nParameters,
    _Out_ IAPPLICATION         **pApplication
    );

class ApplicationFactory
{
public:
    ApplicationFactory(HMODULE hRequestHandlerDll, std::wstring location, PFN_ASPNETCORE_CREATE_APPLICATION pfnAspNetCoreCreateApplication) noexcept:
        m_pfnAspNetCoreCreateApplication(pfnAspNetCoreCreateApplication),
        m_location(std::move(location)),
        m_hRequestHandlerDll(hRequestHandlerDll)
    {
    }

    HRESULT Execute(
        _In_  IHttpServer           *pServer,
        _In_  IHttpContext          *pHttpContext,
        _In_  std::wstring&   shadowCopyDirectory,
        _Outptr_ IAPPLICATION       **pApplication) const
    {
        // m_location.data() is const ptr copy to local to get mutable pointer
        auto location = m_location;
        std::array<APPLICATION_PARAMETER, 4> parameters {
            {
                {"InProcessExeLocation", location.data()},
                {"TraceContext", pHttpContext->GetTraceContext()},
                {"Site", pHttpContext->GetSite()},
                {"ShadowCopyDirectory", shadowCopyDirectory.data()}
            }
        };

        return m_pfnAspNetCoreCreateApplication(pServer, pHttpContext->GetApplication(), parameters.data(), static_cast<DWORD>(parameters.size()), pApplication);
    }

private:
    PFN_ASPNETCORE_CREATE_APPLICATION m_pfnAspNetCoreCreateApplication;
    std::wstring m_location;
    HandleWrapper<ModuleHandleTraits> m_hRequestHandlerDll;
};
