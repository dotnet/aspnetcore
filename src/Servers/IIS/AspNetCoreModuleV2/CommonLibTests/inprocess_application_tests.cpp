// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include <array>
#include "inprocessapplication.h"
#include "fakeclasses.h"

using ::testing::_;
using ::testing::NiceMock;

// Externals defined in inprocess
BOOL       g_fProcessDetach;
HANDLE     g_hEventLog;
std::wstring g_exceptionEventLog;

namespace InprocessTests
{
    TEST(InProcessTest, NoNullRefForExePath)
    {
        MockHttpServer server;
        NiceMock<MockHttpApplication> application;

        ON_CALL(application, GetApplicationPhysicalPath())
            .WillByDefault(testing::Return(L"Some path"));

        ON_CALL(application, GetAppConfigPath())
            .WillByDefault(testing::Return(L""));

        ON_CALL(application, GetApplicationId())
            .WillByDefault(testing::Return(L""));

        auto requestHandlerConfig = std::unique_ptr<InProcessOptions>(MockInProcessOptions::CreateConfig());

        std::wstring exePath(L"hello");

        std::array<APPLICATION_PARAMETER, 1> parameters{
            {"InProcessExeLocation", exePath.data()}
        };

        IN_PROCESS_APPLICATION *app = new IN_PROCESS_APPLICATION(server, application, std::move(requestHandlerConfig), parameters.data(), 1);

        ASSERT_STREQ(app->QueryExeLocation().c_str(), L"hello");
    }

    TEST(InProcessTest, GeneratesVirtualPath)
    {
        MockHttpServer server;
        NiceMock<MockHttpApplication> application;

        ON_CALL(application, GetApplicationPhysicalPath())
            .WillByDefault(testing::Return(L"Some path"));

        ON_CALL(application, GetAppConfigPath())
            .WillByDefault(testing::Return(L"SECTION1/SECTION2/SECTION3/SECTION4/SECTION5"));

        ON_CALL(application, GetApplicationId())
            .WillByDefault(testing::Return(L""));

        auto requestHandlerConfig = std::unique_ptr<InProcessOptions>(MockInProcessOptions::CreateConfig());
        IN_PROCESS_APPLICATION *app = new IN_PROCESS_APPLICATION(server, application, std::move(requestHandlerConfig), nullptr, 0);

        ASSERT_STREQ(app->QueryApplicationVirtualPath().c_str(), L"/SECTION5");
    }

    TEST(InProcessTest, GeneratesVirtualPathForDefaultApp)
    {
        MockHttpServer server;
        NiceMock<MockHttpApplication> application;

        ON_CALL(application, GetApplicationPhysicalPath())
            .WillByDefault(testing::Return(L"Some path"));

        ON_CALL(application, GetAppConfigPath())
            .WillByDefault(testing::Return(L"SECTION1/SECTION2/SECTION3/SECTION4"));

        ON_CALL(application, GetApplicationId())
            .WillByDefault(testing::Return(L""));

        auto requestHandlerConfig = std::unique_ptr<InProcessOptions>(MockInProcessOptions::CreateConfig());
        IN_PROCESS_APPLICATION *app = new IN_PROCESS_APPLICATION(server, application, std::move(requestHandlerConfig), nullptr, 0);

        ASSERT_STREQ(app->QueryApplicationVirtualPath().c_str(), L"/");
    }
}
