// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once
#include "aspnetcore_event.h"

class ModuleTracer
{
public:

    ModuleTracer(IHttpTraceContext* traceContext)
    {
        m_traceContext = traceContext;
    }

    VOID
    ExecuteRequestStart()
    {
        if (ANCMEvents::ANCM_INPROC_EXECUTE_REQUEST_START::IsEnabled(m_traceContext))
        {
            ANCMEvents::ANCM_INPROC_EXECUTE_REQUEST_START::RaiseEvent(
                m_traceContext,
                NULL);
        }
    }

    VOID
    ExecuteRequestEnd(REQUEST_NOTIFICATION_STATUS status)
    {
        if (ANCMEvents::ANCM_INPROC_EXECUTE_REQUEST_COMPLETION::IsEnabled(m_traceContext))
        {
            ANCMEvents::ANCM_INPROC_EXECUTE_REQUEST_COMPLETION::RaiseEvent(
                m_traceContext,
                NULL,
                status);
        }
    }

    VOID
    AsyncCompletionStart()
    {

        if (ANCMEvents::ANCM_INPROC_ASYNC_COMPLETION_START::IsEnabled(m_traceContext))
        {
            ANCMEvents::ANCM_INPROC_ASYNC_COMPLETION_START::RaiseEvent(
                m_traceContext,
                NULL);
        }
    }

    VOID
    AsyncCompletionEnd(REQUEST_NOTIFICATION_STATUS status)
    {
        if (ANCMEvents::ANCM_INPROC_ASYNC_COMPLETION_COMPLETION::IsEnabled(m_traceContext))
        {
            ANCMEvents::ANCM_INPROC_ASYNC_COMPLETION_COMPLETION::RaiseEvent(
                m_traceContext,
                NULL,
                status);
        }
    }

    VOID
    RequestShutdown()
    {
        if (ANCMEvents::ANCM_INPROC_REQUEST_SHUTDOWN::IsEnabled(m_traceContext))
        {
            ANCMEvents::ANCM_INPROC_REQUEST_SHUTDOWN::RaiseEvent(
                m_traceContext,
                NULL);
        }
    }

    VOID
    RequestDisconnect()
    {
        if (ANCMEvents::ANCM_INPROC_REQUEST_DISCONNECT::IsEnabled(m_traceContext))
        {
            ANCMEvents::ANCM_INPROC_REQUEST_DISCONNECT::RaiseEvent(
                m_traceContext,
                NULL);
        }
    }

    VOID
    ManagedCompletion()
    {
        if (ANCMEvents::ANCM_INPROC_MANAGED_REQUEST_COMPLETION::IsEnabled(m_traceContext))
        {
            ANCMEvents::ANCM_INPROC_MANAGED_REQUEST_COMPLETION::RaiseEvent(
                m_traceContext,
                NULL);
        }
    }

private:
    IHttpTraceContext * m_traceContext;
};
