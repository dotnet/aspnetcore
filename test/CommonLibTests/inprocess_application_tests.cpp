// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"

#include <array>
#include "inprocessapplication.h"
#include "fakeclasses.h"

using ::testing::_;
using ::testing::NiceMock;

// Externals defined in inprocess
BOOL       g_fProcessDetach;
HANDLE     g_hEventLog;

namespace InprocessTests
{
    TEST(InProcessTest, NoNullRefForExePath)
    {
        MockHttpServer server;
        NiceMock<MockHttpApplication> application;

        ON_CALL(application, GetApplicationPhysicalPath())
            .WillByDefault(testing::Return(L"Some path"));

        auto requestHandlerConfig = std::unique_ptr<REQUESTHANDLER_CONFIG>(MockRequestHandlerConfig::CreateConfig());

        std::wstring exePath(L"hello");

        std::array<APPLICATION_PARAMETER, 1> parameters{
            {"InProcessExeLocation", exePath.data()}
        };

        IN_PROCESS_APPLICATION *app = new IN_PROCESS_APPLICATION(server, application, std::move(requestHandlerConfig), parameters.data(), 1);

        ASSERT_STREQ(app->QueryExeLocation(), L"hello");
    }
}
