// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "AppOfflineTrackingApplication.h"

typedef INT(*hostfxr_main_fn) (CONST DWORD argc, CONST PCWSTR argv[]); // TODO these may need to be BSTRs

class InProcessApplicationBase : public AppOfflineTrackingApplication
{
public:

    InProcessApplicationBase(
        IHttpServer& pHttpServer,
        IHttpApplication& pHttpApplication);

    ~InProcessApplicationBase() = default;

    VOID StopInternal(bool fServerInitiated) override;

protected:
    BOOL m_fRecycleCalled;
    IHttpServer& m_pHttpServer;
};

