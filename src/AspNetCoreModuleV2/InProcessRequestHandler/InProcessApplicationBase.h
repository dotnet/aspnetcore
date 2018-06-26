// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "precomp.hxx"
#include "application.h"
#include "requesthandler_config.h"

typedef INT(*hostfxr_main_fn) (CONST DWORD argc, CONST PCWSTR argv[]); // TODO these may need to be BSTRs

class InProcessApplicationBase : public APPLICATION
{
public:

    InProcessApplicationBase(IHttpServer* pHttpServer);

    ~InProcessApplicationBase() = default;

    VOID Recycle(VOID) override;

protected:
    BOOL m_fRecycleCalled;
    SRWLOCK m_srwLock;
    IHttpServer* const m_pHttpServer;
    // Allows to override call to hostfxr_main with custome callback
    // used in testing
    static hostfxr_main_fn          s_fMainCallback;
};

