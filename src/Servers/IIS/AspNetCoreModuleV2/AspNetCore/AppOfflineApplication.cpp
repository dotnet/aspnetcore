// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AppOfflineApplication.h"

#include "HandleWrapper.h"
#include "AppOfflineHandler.h"
#include "exceptions.h"

HRESULT AppOfflineApplication::CreateHandler(IHttpContext* pHttpContext, IREQUEST_HANDLER** pRequestHandler)
{
    try
    {
        auto handler = std::make_unique<AppOfflineHandler>(*pHttpContext, m_strAppOfflineContent);
        *pRequestHandler = handler.release();
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
        std::string pszBuff(static_cast<size_t>(li.LowPart) + 1, '\0');

        RETURN_LAST_ERROR_IF(!ReadFile(handle, pszBuff.data(), li.LowPart, &bytesRead, nullptr));
        pszBuff.resize(bytesRead);

        m_strAppOfflineContent = pszBuff;
    }

    return S_OK;
}

bool AppOfflineApplication::ShouldBeStarted(const IHttpApplication& pApplication)
{
    return FileExists(GetAppOfflineLocation(pApplication));
}
