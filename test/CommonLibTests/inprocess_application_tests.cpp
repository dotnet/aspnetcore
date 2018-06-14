// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "inprocessapplication.h"
#include "fakeclasses.h"

// Externals defined in inprocess
BOOL       g_fProcessDetach;
HANDLE     g_hEventLog;
DWORD g_dwAspNetCoreDebugFlags;
PCSTR g_szDebugLabel;
DWORD g_dwDebugFlags;

namespace InprocessTests
{
    TEST(InProcessTest, NoNullRefForExePath)
    {
        auto server = new MockHttpServer();
        auto requestHandlerConfig = MockRequestHandlerConfig::CreateConfig();
        IN_PROCESS_APPLICATION *app = new IN_PROCESS_APPLICATION(server, requestHandlerConfig);
        {
            std::wstring exePath(L"hello");
            app->Initialize(exePath.c_str());
        }
        ASSERT_STREQ(app->QueryExeLocation(), L"hello");
    }
}


