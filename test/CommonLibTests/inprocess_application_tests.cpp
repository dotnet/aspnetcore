// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"

#include "inprocessapplication.h"
#include "fakeclasses.h"

// Externals defined in inprocess
BOOL       g_fProcessDetach;
HANDLE     g_hEventLog;

namespace InprocessTests
{
    TEST(InProcessTest, NoNullRefForExePath)
    {
        auto server = new MockHttpServer();
        auto requestHandlerConfig = MockRequestHandlerConfig::CreateConfig();
        auto config = std::unique_ptr<REQUESTHANDLER_CONFIG>(requestHandlerConfig);
        IN_PROCESS_APPLICATION *app = new IN_PROCESS_APPLICATION(server, std::move(config));
        {
            std::wstring exePath(L"hello");
            app->SetParameter(L"InProcessExeLocation", exePath.c_str());
        }
        ASSERT_STREQ(app->QueryExeLocation(), L"hello");
    }
}
