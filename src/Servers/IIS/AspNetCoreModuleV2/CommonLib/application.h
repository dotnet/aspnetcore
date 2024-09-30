// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include "iapplication.h"
#include "ntassert.h"
#include "SRWExclusiveLock.h"
#include "SRWSharedLock.h"
#include "exceptions.h"
#include "HandleWrapper.h"

class APPLICATION : public IAPPLICATION
{
public:
    // Non-copyable
    APPLICATION(const APPLICATION&) = delete;
    const APPLICATION& operator=(const APPLICATION&) = delete;

    HRESULT
    TryCreateHandler(
        _In_  IHttpContext       *pHttpContext,
        _Outptr_result_maybenull_ IREQUEST_HANDLER  **pRequestHandler) override
    {
        *pRequestHandler = nullptr;

        SRWSharedLock stopLock(m_stopLock);

        // If we have acquired the stopLock, we don't need to acquire the data lock
        // as m_fStoppedCalled is only set by Stop.
        if (m_fStopCalled)
        {
            return S_FALSE;
        }

        TraceContextScope traceScope(pHttpContext->GetTraceContext());

        return CreateHandler(pHttpContext, pRequestHandler);
    }

    virtual
    HRESULT
    CreateHandler(
        _In_ IHttpContext       *pHttpContext,
        _Outptr_opt_ IREQUEST_HANDLER  **pRequestHandler) = 0;

    APPLICATION(const IHttpApplication& pHttpApplication)
        : m_fStopCalled(false),
          m_cRefs(1),
          m_applicationPhysicalPath(pHttpApplication.GetApplicationPhysicalPath()),
          m_applicationConfigPath(pHttpApplication.GetAppConfigPath()),
          m_applicationId(pHttpApplication.GetApplicationId())
    {
        InitializeSRWLock(&m_stopLock);
        InitializeSRWLock(&m_dataLock);
        m_applicationVirtualPath = ToVirtualPath(m_applicationConfigPath);
    }

    VOID
    Stop(bool fServerInitiated) override
    {
        SRWExclusiveLock stopLock(m_stopLock);

        {
            SRWExclusiveLock dataLock(m_dataLock);
            if (m_fStopCalled)
            {
                return;
            }

            m_fStopCalled = true;
        }

        StopInternal(fServerInitiated);
    }

    virtual
    VOID
    StopInternal(bool fServerInitiated)
    {
        UNREFERENCED_PARAMETER(fServerInitiated);
    }

    VOID
    ReferenceApplication() noexcept override
    {
        DBG_ASSERT(m_cRefs > 0);

        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceApplication() noexcept override
    {
        DBG_ASSERT(m_cRefs > 0);

        if (InterlockedDecrement(&m_cRefs) == 0)
        {
            delete this;
        }
    }

    const std::wstring&
    QueryApplicationId() const noexcept
    {
        return m_applicationId;
    }

    const std::wstring&
    QueryApplicationPhysicalPath() const noexcept
    {
        return m_applicationPhysicalPath;
    }

    const std::wstring&
    QueryApplicationVirtualPath() const noexcept
    {
        return m_applicationVirtualPath;
    }

    const std::wstring&
    QueryConfigPath() const noexcept
    {
        return m_applicationConfigPath;
    }

protected:
    SRWLOCK m_stopLock{};
    SRWLOCK m_dataLock {};
    bool m_fStopCalled;
    std::wstring m_applicationPhysicalPath;

private:
    mutable LONG           m_cRefs;

    std::wstring m_applicationVirtualPath;
    std::wstring m_applicationConfigPath;
    std::wstring m_applicationId;

    static std::wstring ToVirtualPath(const std::wstring& configurationPath)
    {
        auto segments = 0;
        auto position = configurationPath.find('/');
        // Skip first 4 segments of config path
        while (segments != 3 && position != std::wstring::npos)
        {
            segments++;
            position = configurationPath.find('/', position + 1);
        }

        if (position != std::wstring::npos)
        {
            return configurationPath.substr(position);
        }

        return L"/";
    }
};
