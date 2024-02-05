// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "inprocesshandler.h"
#include "inprocessapplication.h"
#include "ShuttingDownApplication.h"
#include "ntassert.h"

ALLOC_CACHE_HANDLER * IN_PROCESS_HANDLER::sm_pAlloc = NULL;

IN_PROCESS_HANDLER::IN_PROCESS_HANDLER(
    _In_ std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER> pApplication,
    _In_ IHttpContext *pW3Context,
    _In_ PFN_REQUEST_HANDLER pRequestHandler,
    _In_ void * pRequestHandlerContext,
    _In_ PFN_DISCONNECT_HANDLER pDisconnectHandler,
    _In_ PFN_ASYNC_COMPLETION_HANDLER pAsyncCompletion
): REQUEST_HANDLER(*pW3Context),
   m_pManagedHttpContext(nullptr),
   m_requestNotificationStatus(RQ_NOTIFICATION_PENDING),
   m_fManagedRequestComplete(FALSE),
   m_pW3Context(pW3Context),
   m_pApplication(std::move(pApplication)),
   m_pRequestHandler(pRequestHandler),
   m_pRequestHandlerContext(pRequestHandlerContext),
   m_pAsyncCompletionHandler(pAsyncCompletion),
   m_pDisconnectHandler(pDisconnectHandler),
   m_disconnectFired(false),
   m_queueNotified(false)
{
    InitializeSRWLock(&m_srwDisconnectLock);
}

__override
REQUEST_NOTIFICATION_STATUS
IN_PROCESS_HANDLER::ExecuteRequestHandler()
{
    ::RaiseEvent<ANCMEvents::ANCM_INPROC_EXECUTE_REQUEST_START>(m_pW3Context, nullptr);

    if (m_pRequestHandler == NULL)
    {
        ::RaiseEvent<ANCMEvents::ANCM_INPROC_EXECUTE_REQUEST_COMPLETION>(m_pW3Context, nullptr, RQ_NOTIFICATION_FINISH_REQUEST);
        return RQ_NOTIFICATION_FINISH_REQUEST;
    }
    else if (m_pApplication->QueryBlockCallbacksIntoManaged())
    {
        return ServerShutdownMessage();
    }

    auto status = m_pRequestHandler(this, m_pRequestHandlerContext);
    ::RaiseEvent<ANCMEvents::ANCM_INPROC_EXECUTE_REQUEST_COMPLETION>(m_pW3Context, nullptr, status);
    return status;
}

__override
REQUEST_NOTIFICATION_STATUS
IN_PROCESS_HANDLER::AsyncCompletion(
    DWORD       cbCompletion,
    HRESULT     hrCompletionStatus
)
{
    ::RaiseEvent<ANCMEvents::ANCM_INPROC_ASYNC_COMPLETION_START>(m_pW3Context, nullptr);

    if (m_fManagedRequestComplete)
    {
        // means PostCompletion has been called and this is the associated callback.
        ::RaiseEvent<ANCMEvents::ANCM_INPROC_ASYNC_COMPLETION_COMPLETION>(m_pW3Context, nullptr, m_requestNotificationStatus);
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
    ::RaiseEvent<ANCMEvents::ANCM_INPROC_ASYNC_COMPLETION_COMPLETION>(m_pW3Context, nullptr, status);
    return status;
}

REQUEST_NOTIFICATION_STATUS IN_PROCESS_HANDLER::ServerShutdownMessage() const
{
    ::RaiseEvent<ANCMEvents::ANCM_INPROC_REQUEST_SHUTDOWN>(m_pW3Context, nullptr);
    return ShuttingDownHandler::ServerShutdownMessage(m_pW3Context);
}

// Called from native IIS
VOID
IN_PROCESS_HANDLER::NotifyDisconnect()
{
    // NotifyDisconnect can be called before the m_pManagedHttpContext is set,
    // so save that in a bool.
    // Don't lock when calling m_pDisconnect to avoid the potential deadlock between this
    // and SetManagedHttpContext
    void* pManagedHttpContext = nullptr;
    {
        SRWExclusiveLock lock(m_srwDisconnectLock);

        if (m_pApplication->QueryBlockCallbacksIntoManaged() ||
        m_fManagedRequestComplete)
        {
            return;
        }

        ::RaiseEvent<ANCMEvents::ANCM_INPROC_REQUEST_DISCONNECT>(m_pW3Context, nullptr);

        pManagedHttpContext = m_pManagedHttpContext;
        m_disconnectFired = true;
    }

    // This could be null if the request is completed before the http context is set
    // for example this can happen when the client cancels the request very quickly after making it
    if (pManagedHttpContext != nullptr)
    {
        m_pDisconnectHandler(pManagedHttpContext);
    }

    // Make sure we unblock any potential current or future m_queueCheck.wait(...) calls
    // We could make this conditional, but it would need to be duplicated in SetManagedHttpContext
    // to avoid a race condition where the http context is null but we called disconnect which could make IndicateManagedRequestComplete hang
    // It's more future proof to just always do this even if nothing will be waiting on the conditional_variable
    {
        // lock before notifying, this prevents the condition where m_queueNotified is already checked but
        // the condition_variable isn't waiting yet, which would cause notify_all to NOOP and block
        // IndicateManagedRequestComplete until a spurious wakeup
        std::lock_guard<std::mutex> lock(m_lockQueue);
        m_queueNotified = true;
    }
    m_queueCheck.notify_all();
}

// Called from managed server
VOID
IN_PROCESS_HANDLER::IndicateManagedRequestComplete(
    VOID
)
{
    bool disconnectFired = false;
    {
        SRWExclusiveLock lock(m_srwDisconnectLock);
        m_fManagedRequestComplete = TRUE;
        m_pManagedHttpContext = nullptr;
        disconnectFired = m_disconnectFired;
    }

    if (disconnectFired)
    {
        // Block until we know NotifyDisconnect completed
        // this is because the caller of IndicateManagedRequestComplete will dispose the
        // GCHandle pointing at m_pManagedHttpContext, and a new GCHandle could use the same address
        // for the next request, this could cause an in-progress NotifyDisconnect call to disconnect the new request
        std::unique_lock<std::mutex> lock(m_lockQueue);
        // loop to handle spurious wakeups
        while (!m_queueNotified)
        {
            m_queueCheck.wait(lock);
        }
    }

    ::RaiseEvent<ANCMEvents::ANCM_INPROC_MANAGED_REQUEST_COMPLETION>(m_pW3Context, nullptr);
}

VOID
IN_PROCESS_HANDLER::SetAsyncCompletionStatus(
    REQUEST_NOTIFICATION_STATUS requestNotificationStatus
)
{
    m_requestNotificationStatus = requestNotificationStatus;
}

// Called from managed server
VOID
IN_PROCESS_HANDLER::SetManagedHttpContext(
    PVOID pManagedHttpContext
)
{
    bool disconnectFired = false;

    {
        SRWExclusiveLock lock(m_srwDisconnectLock);
        m_pManagedHttpContext = pManagedHttpContext;
        disconnectFired = m_disconnectFired;
    }

    if (disconnectFired && pManagedHttpContext != nullptr)
    {
        // Safe to call, managed code is waiting on SetManagedHttpContext in the process request loop and doesn't dispose
        // the GCHandle until after the request loop completes
        m_pDisconnectHandler(pManagedHttpContext);
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
