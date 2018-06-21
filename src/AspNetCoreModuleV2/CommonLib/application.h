// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "iapplication.h"
#include "ntassert.h"

class APPLICATION : public IAPPLICATION
{

public:

    APPLICATION_STATUS
    QueryStatus() override
    {
        return m_status;
    }

    APPLICATION()
        : m_cRefs(1)
    {
    }

    VOID
    ReferenceApplication() override
    {
        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceApplication() override
    {
        DBG_ASSERT(m_cRefs != 0);

        LONG cRefs = 0;
        if ((cRefs = InterlockedDecrement(&m_cRefs)) == 0)
        {
            delete this;
        }
    }

protected:
    volatile APPLICATION_STATUS     m_status = APPLICATION_STATUS::UNKNOWN;

private:
    mutable LONG                    m_cRefs;
};
