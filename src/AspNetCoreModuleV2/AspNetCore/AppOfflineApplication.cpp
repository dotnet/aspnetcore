// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "AppOfflineApplication.h"

#include "HandleWrapper.h"
#include "AppOfflineHandler.h"
#include "exceptions.h"

HRESULT AppOfflineApplication::CreateHandler(IHttpContext* pHttpContext, IREQUEST_HANDLER** pRequestHandler)
{
    try
    {
        *pRequestHandler = new AppOfflineHandler(pHttpContext, m_strAppOfflineContent);
    }
    CATCH_RETURN();

    return S_OK;
}

HRESULT AppOfflineApplication::OnAppOfflineFound()
{
    LARGE_INTEGER   li = {};

    HandleWrapper<InvalidHandleTraits> handle = CreateFile(m_appOfflineLocation.c_str(),
                        GENERIC_READ,
                        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                        nullptr,
                        OPEN_EXISTING,
                        FILE_ATTRIBUTE_NORMAL,
                        nullptr);

    RETURN_LAST_ERROR_IF(handle == INVALID_HANDLE_VALUE);

    RETURN_LAST_ERROR_IF(!GetFileSizeEx(handle, &li));

    if (li.HighPart != 0)
    {
        // > 4gb file size not supported
        // todo: log a warning at event log
        return E_INVALIDARG;
    }

    if (li.LowPart > 0)
    {
        DWORD bytesRead = 0;
        std::string pszBuff(li.LowPart + 1, '\0');

        RETURN_LAST_ERROR_IF(!ReadFile(handle, pszBuff.data(), li.LowPart, &bytesRead, NULL));
        pszBuff.resize(bytesRead);

        m_strAppOfflineContent = pszBuff;
    }

    return S_OK;
}

bool AppOfflineApplication::ShouldBeStarted(IHttpApplication& pApplication)
{
    return is_regular_file(GetAppOfflineLocation(pApplication));
}
