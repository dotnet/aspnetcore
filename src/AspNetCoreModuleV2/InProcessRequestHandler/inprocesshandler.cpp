// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "inprocesshandler.h"
#include "inprocessapplication.h"
#include "IOutputManager.h"
#include "ShuttingDownApplication.h"
#include "ntassert.h"
#include "ModuleTracer.h"

ALLOC_CACHE_HANDLER * IN_PROCESS_HANDLER::sm_pAlloc = NULL;

IN_PROCESS_HANDLER::IN_PROCESS_HANDLER(
    _In_ std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER> pApplication,
    _In_ IHttpContext   *pW3Context,
    _In_ PFN_REQUEST_HANDLER pRequestHandler,
    _In_ void * pRequestHandlerContext,
    _In_ PFN_DISCONNECT_HANDLER pDisconnectHandler,
    _In_ PFN_ASYNC_COMPLETION_HANDLER pAsyncCompletion
): m_pManagedHttpContext(nullptr),
   m_requestNotificationStatus(RQ_NOTIFICATION_PENDING),
   m_fManagedRequestComplete(FALSE),
   m_pW3Context(pW3Context),
   m_pApplication(std::move(pApplication)),
   m_pRequestHandler(pRequestHandler),
   m_pRequestHandlerContext(pRequestHandlerContext),
   m_pAsyncCompletionHandler(pAsyncCompletion),
   m_pDisconnectHandler(pDisconnectHandler),
   m_moduleTracer(pW3Context->GetTraceContext()),
   m_disconnectFired(false)
{
    InitializeSRWLock(&m_srwDisconnectLock);
}

__override
REQUEST_NOTIFICATION_STATUS
IN_PROCESS_HANDLER::OnExecuteRequestHandler()
{
    // FREB log
    m_moduleTracer.ExecuteRequestStart();

    if (m_pRequestHandler == NULL)
    {
        m_moduleTracer.ExecuteRequestEnd(RQ_NOTIFICATION_FINISH_REQUEST);
        return RQ_NOTIFICATION_FINISH_REQUEST;
    }
    else if (m_pApplication->QueryBlockCallbacksIntoManaged())
    {
        return ServerShutdownMessage();
    }

    auto status = m_pRequestHandler(this, m_pRequestHandlerContext);
    m_moduleTracer.ExecuteRequestEnd(status);
    return status;
}

__override
REQUEST_NOTIFICATION_STATUS
IN_PROCESS_HANDLER::OnAsyncCompletion(
    DWORD       cbCompletion,
    HRESULT     hrCompletionStatus
)
{
    m_moduleTracer.AsyncCompletionStart();

    if (m_fManagedRequestComplete)
    {
        // means PostCompletion has been called and this is the associated callback.
        m_moduleTracer.AsyncCompletionEnd(m_requestNotificationStatus);
        return m_requestNotificationStatus; 
    }
    if (m_pApplication->QueryBlockCallbacksIntoManaged())
    {
        // this can potentially happen in ungraceful shutdown.
        // Or something really wrong happening with async completions
        // At this point, managed is in a shutting down state and we cannot send a request to it.
        return ServerShutdownMessage();
    }

    assert(m_pManagedHttpContext != nullptr);
    // Call the managed handler for async completion.

    auto status = m_pAsyncCompletionHandler(m_pManagedHttpContext, hrCompletionStatus, cbCompletion);
    m_moduleTracer.AsyncCompletionEnd(status);
    return status;
}

REQUEST_NOTIFICATION_STATUS IN_PROCESS_HANDLER::ServerShutdownMessage()
{
    m_moduleTracer.RequestShutdown();
    return ShuttingDownHandler::ServerShutdownMessage(m_pW3Context);
}

VOID
IN_PROCESS_HANDLER::NotifyDisconnect()
{
    m_moduleTracer.RequestDisconnect();

    if (m_pApplication->QueryBlockCallbacksIntoManaged() ||
        m_fManagedRequestComplete)
    {
        return;
    }

    // NotifyDisconnect can be called before the m_pManagedHttpContext is set,
    // so save that in a bool.
    // Don't lock when calling m_pDisconnect to avoid the potential deadlock between this
    // and SetManagedHttpContext
    void* pManagedHttpContext = nullptr;
    {
        SRWExclusiveLock lock(m_srwDisconnectLock);
        pManagedHttpContext = m_pManagedHttpContext;
        m_disconnectFired = true;
    }

    if (pManagedHttpContext != nullptr)
    {
        m_pDisconnectHandler(m_pManagedHttpContext);
    }
}

VOID
IN_PROCESS_HANDLER::IndicateManagedRequestComplete(
    VOID
)
{
    m_fManagedRequestComplete = TRUE;
    m_pManagedHttpContext = nullptr;
    m_moduleTracer.ManagedCompletion();
}

VOID
IN_PROCESS_HANDLER::SetAsyncCompletionStatus(
    REQUEST_NOTIFICATION_STATUS requestNotificationStatus
)
{
    m_requestNotificationStatus = requestNotificationStatus;
}

VOID
IN_PROCESS_HANDLER::SetManagedHttpContext(
    PVOID pManagedHttpContext
)
{
    {
        SRWExclusiveLock lock(m_srwDisconnectLock);
        m_pManagedHttpContext = pManagedHttpContext;
    }

    if (m_disconnectFired && m_pManagedHttpContext != nullptr)
    {
        m_pDisconnectHandler(m_pManagedHttpContext);
    }
}

// static
void * IN_PROCESS_HANDLER::operator new(size_t)
{
    DBG_ASSERT(sm_pAlloc != NULL);
    if (sm_pAlloc == NULL)
    {
        return NULL;
    }
    return sm_pAlloc->Alloc();
}

// static
void IN_PROCESS_HANDLER::operator delete(void * pMemory)
{
    DBG_ASSERT(sm_pAlloc != NULL);
    if (sm_pAlloc != NULL)
    {
        sm_pAlloc->Free(pMemory);
    }
}

// static
HRESULT
IN_PROCESS_HANDLER::StaticInitialize(VOID)
/*++

Routine Description:

Global initialization routine for IN_PROCESS_HANDLER

Return Value:

HRESULT

--*/
{
    HRESULT                         hr = S_OK;

    sm_pAlloc = new ALLOC_CACHE_HANDLER;
    if (sm_pAlloc == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = sm_pAlloc->Initialize(sizeof(IN_PROCESS_HANDLER),
                               64); // nThreshold

Finished:
    if (FAILED(hr))
    {
        StaticTerminate();
    }
    return hr;
}

// static
void
IN_PROCESS_HANDLER::StaticTerminate(VOID)
{
    if (sm_pAlloc != NULL)
    {
        delete sm_pAlloc;
        sm_pAlloc = NULL;
    }
}
