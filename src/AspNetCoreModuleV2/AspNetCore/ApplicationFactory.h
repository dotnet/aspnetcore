// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <array>
#include <string>
#include "iapplication.h"
#include "HandleWrapper.h"

typedef
HRESULT
(WINAPI * PFN_ASPNETCORE_CREATE_APPLICATION)(
    _In_  IHttpServer           *pServer,
    _In_  IHttpApplication      *pHttpApplication,
    _In_  APPLICATION_PARAMETER *pParameters,
    _In_  DWORD                  nParameters,
    _Out_ IAPPLICATION         **pApplication
    );

class ApplicationFactory
{
public:
    ApplicationFactory(HMODULE hRequestHandlerDll, std::wstring location, PFN_ASPNETCORE_CREATE_APPLICATION pfnAspNetCoreCreateApplication):
        m_pfnAspNetCoreCreateApplication(pfnAspNetCoreCreateApplication),
        m_location(location),
        m_hRequestHandlerDll(hRequestHandlerDll)
    {
    }

    HRESULT Execute(
        _In_  IHttpServer           *pServer,
        _In_  IHttpApplication      *pHttpApplication,
        _Out_ IAPPLICATION         **pApplication) const
    {
        std::array<APPLICATION_PARAMETER, 1> parameters {
            {"InProcessExeLocation", reinterpret_cast<const void*>(m_location.data())}
        };
        return m_pfnAspNetCoreCreateApplication(pServer, pHttpApplication, parameters.data(), static_cast<DWORD>(parameters.size()), pApplication);
    }

private:
    PFN_ASPNETCORE_CREATE_APPLICATION m_pfnAspNetCoreCreateApplication;
    std::wstring m_location;
    HandleWrapper<ModuleHandleTraits> m_hRequestHandlerDll;
};
