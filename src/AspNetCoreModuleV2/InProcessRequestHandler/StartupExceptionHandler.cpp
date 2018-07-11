// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "StartupExceptionApplication.h"
#include "StartupExceptionHandler.h"

REQUEST_NOTIFICATION_STATUS StartupExceptionHandler::OnExecuteRequestHandler()
{
    if (!m_disableLogs)
    {
        HTTP_DATA_CHUNK   DataChunk;
        IHttpResponse* pResponse = m_pContext->GetResponse();
        pResponse->SetStatus(500, "Internal Server Error", 30, E_FAIL, NULL, TRUE);
        pResponse->SetHeader("Content-Type",
            "text/html",
            (USHORT)strlen("text/html"),
            FALSE
        );
        const std::string& html500Page = m_pApplication->GetStaticHtml500Content();

        DataChunk.DataChunkType = HttpDataChunkFromMemory;
        DataChunk.FromMemory.pBuffer = (PVOID)html500Page.c_str();
        DataChunk.FromMemory.BufferLength = (ULONG)html500Page.size();
        pResponse->WriteEntityChunkByReference(&DataChunk);
    }
    else
    {
        m_pContext->GetResponse()->SetStatus(500, "Internal Server Error", 30, E_FAIL);
    }

    return REQUEST_NOTIFICATION_STATUS::RQ_NOTIFICATION_FINISH_REQUEST;
}

