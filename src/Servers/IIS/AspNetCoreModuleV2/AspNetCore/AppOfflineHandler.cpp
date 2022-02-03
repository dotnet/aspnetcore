// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AppOfflineHandler.h"

#include "HandleWrapper.h"

REQUEST_NOTIFICATION_STATUS AppOfflineHandler::ExecuteRequestHandler()
{
    HTTP_DATA_CHUNK   DataChunk {};
    auto* pResponse = m_pContext.GetResponse();

    DBG_ASSERT(pResponse);

    // Ignore failure hresults as nothing we can do
    // Set fTrySkipCustomErrors to true as we want client see the offline content
    pResponse->SetStatus(503, "Service Unavailable", 0, S_OK, nullptr, TRUE);
    pResponse->SetHeader("Content-Type",
        "text/html",
        static_cast<USHORT>(strlen("text/html")),
        FALSE
    );

    DataChunk.DataChunkType = HttpDataChunkFromMemory;
    DataChunk.FromMemory.pBuffer = m_strAppOfflineContent.data();
    DataChunk.FromMemory.BufferLength = static_cast<ULONG>(m_strAppOfflineContent.size());
    pResponse->WriteEntityChunkByReference(&DataChunk);

    return REQUEST_NOTIFICATION_STATUS::RQ_NOTIFICATION_FINISH_REQUEST;
}
