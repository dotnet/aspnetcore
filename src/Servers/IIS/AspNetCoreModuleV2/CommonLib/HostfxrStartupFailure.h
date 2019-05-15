// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "PollingAppOfflineApplication.h"
#include "requesthandler.h"
#include "ServerErrorHandler.h"

class HostfxrStartupFailureHandler : public REQUEST_HANDLER
{
public:
    // TODO stdmove error
    HostfxrStartupFailureHandler(IHttpContext& pContext, std::vector<byte> error, bool disableStartupPage)
        : m_error(error),
        m_disableStartupPage(disableStartupPage),
        REQUEST_HANDLER(pContext)
    {
    }

    REQUEST_NOTIFICATION_STATUS ExecuteRequestHandler() override
    {
        if (m_disableStartupPage)
        {
            m_pHttpContext.GetResponse()->SetStatus(500, "Internal Server Error", 31, E_FAIL);
            return RQ_NOTIFICATION_FINISH_REQUEST;
        }

        HTTP_DATA_CHUNK dataChunk = {};
        IHttpResponse* pResponse = m_pHttpContext.GetResponse();
        pResponse->SetStatus(500, "Internal Server Error", 31, E_HANDLE, nullptr, true);
        pResponse->SetHeader("Content-Type",
            "text/plain",
            (USHORT)strlen("text/plain"),
            FALSE
        );

        dataChunk.FromMemory.pBuffer = &m_error[0];
        dataChunk.FromMemory.BufferLength = static_cast<ULONG>(m_error.size());
        pResponse->WriteEntityChunkByReference(&dataChunk);

        return RQ_NOTIFICATION_FINISH_REQUEST;
    }

private:
    std::vector<byte> m_error;
    bool m_disableStartupPage;
};

class HostfxrStartupFailure : public PollingAppOfflineApplication
{
public:
    HostfxrStartupFailure(const IHttpApplication& pApplication, std::vector<byte> error, bool disableStartupPage)
        : m_error(error),
        m_disableStartupPage(disableStartupPage),
        PollingAppOfflineApplication(pApplication, PollingAppOfflineApplicationMode::StopWhenAdded)
    {
    }

    HRESULT CreateHandler(IHttpContext* pHttpContext, IREQUEST_HANDLER** pRequestHandler) override
    {
        auto handler = std::make_unique<HostfxrStartupFailureHandler>(*pHttpContext, m_error, m_disableStartupPage);
        *pRequestHandler = handler.release();
        return S_OK;
    }

    HRESULT OnAppOfflineFound() noexcept override { return S_OK; }

private:
    std::vector<byte> m_error;
    bool m_disableStartupPage;
};

