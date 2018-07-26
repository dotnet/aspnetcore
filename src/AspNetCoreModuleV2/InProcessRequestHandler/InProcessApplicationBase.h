// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "application.h"
#include "AppOfflineTrackingApplication.h"

typedef INT(*hostfxr_main_fn) (CONST DWORD argc, CONST PCWSTR argv[]); // TODO these may need to be BSTRs

class InProcessApplicationBase : public AppOfflineTrackingApplication
{
public:

    InProcessApplicationBase(
        IHttpServer& pHttpServer,
        IHttpApplication& pHttpApplication);

    ~InProcessApplicationBase() = default;

    VOID Stop(bool fServerInitiated) override;

protected:
    BOOL m_fRecycleCalled;
    SRWLOCK m_srwLock;
    IHttpServer& m_pHttpServer;
    // Allows to override call to hostfxr_main with custome callback
    // used in testing
    static hostfxr_main_fn          s_fMainCallback;
};

