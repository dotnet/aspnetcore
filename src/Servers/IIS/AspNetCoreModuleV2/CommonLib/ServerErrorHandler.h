// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "requesthandler.h"
#include "file_utility.h"
#include "Environment.h"

class ServerErrorHandler : public REQUEST_HANDLER
{
public:
    ServerErrorHandler(IHttpContext& pContext, USHORT statusCode, USHORT subStatusCode, const std::string& statusText, HRESULT hr, bool disableStartupPage, std::string& responseContent) noexcept
        : REQUEST_HANDLER(pContext),
        m_HR(hr),
        m_disableStartupPage(disableStartupPage),
        m_statusCode(statusCode),
        m_subStatusCode(subStatusCode),
        m_statusText(statusText),
        m_ExceptionInfoContent(responseContent)
    {
    }

    REQUEST_NOTIFICATION_STATUS ExecuteRequestHandler() override
    {
        WriteResponse();

        return RQ_NOTIFICATION_FINISH_REQUEST;
    }

private:
    void WriteResponse()
    {
        if (m_disableStartupPage)
        {
            m_pHttpContext.GetResponse()->SetStatus(m_statusCode, m_statusText.c_str(), m_subStatusCode, E_FAIL);
            return;
        }

        HTTP_DATA_CHUNK dataChunk = {};
        IHttpResponse* pResponse = m_pHttpContext.GetResponse();
        pResponse->SetStatus(m_statusCode, m_statusText.c_str(), m_subStatusCode, m_HR, nullptr, true);
        pResponse->SetHeader("Content-Type",
            "text/html",
            (USHORT)strlen("text/html"),
            FALSE
        );

        dataChunk.DataChunkType = HttpDataChunkFromMemory;
        dataChunk.FromMemory.pBuffer = m_ExceptionInfoContent.data();
        dataChunk.FromMemory.BufferLength = static_cast<ULONG>(m_ExceptionInfoContent.size());

        pResponse->WriteEntityChunkByReference(&dataChunk);
    }

    HRESULT m_HR;
    bool m_disableStartupPage;
    USHORT m_statusCode;
    USHORT m_subStatusCode;
    std::string m_statusText;
    std::string& m_ExceptionInfoContent;
};
