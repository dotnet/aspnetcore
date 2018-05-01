// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"

//
// Pure abstract class
//
class REQUEST_HANDLER: public virtual IREQUEST_HANDLER
{

public:
    VOID
    ReferenceRequestHandler() override
    {
        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceRequestHandler() override
    {
        DBG_ASSERT(m_cRefs != 0);

        LONG cRefs = 0;
        if ((cRefs = InterlockedDecrement(&m_cRefs)) == 0)
        {
            delete this;
        }
    }

private:
    mutable LONG                    m_cRefs = 1;
};
