// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "iapplication.h"
#include "exceptions.h"
#include "utility.h"
#include "ntassert.h"
#include "SRWExclusiveLock.h"

class APPLICATION : public IAPPLICATION
{
public:
    // Non-copyable
    APPLICATION(const APPLICATION&) = delete;
    const APPLICATION& operator=(const APPLICATION&) = delete;

    APPLICATION_STATUS
    QueryStatus() override
    {
        return m_fStopCalled ? APPLICATION_STATUS::RECYCLED : APPLICATION_STATUS::RUNNING;
    }

    APPLICATION()
        : m_fStopCalled(false),
          m_cRefs(1)
    {
        InitializeSRWLock(&m_stateLock);
    }


    VOID
    Stop(bool fServerInitiated) override
    {
        SRWExclusiveLock stopLock(m_stateLock);

        if (m_fStopCalled)
        {
            return;
        }

        m_fStopCalled = true;

        StopInternal(fServerInitiated);
    }

    virtual
    VOID
    StopInternal(bool fServerInitiated)
    {
        UNREFERENCED_PARAMETER(fServerInitiated);
    }

    VOID
    ReferenceApplication() override
    {
        DBG_ASSERT(m_cRefs > 0);

        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceApplication() override
    {
        DBG_ASSERT(m_cRefs > 0);

        if (InterlockedDecrement(&m_cRefs) == 0)
        {
            delete this;
        }
    }

protected:
    SRWLOCK m_stateLock;
    bool m_fStopCalled; 

private:
    mutable LONG           m_cRefs;
};
