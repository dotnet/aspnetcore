// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"

namespace BindingInformationTest
{
    class BindingInformationTest : public testing::Test
    {
    protected:
        void
        ParseBindingInformation(std::wstring protocol, std::wstring info, std::wstring expectedHost, std::wstring expectedPort)
        {
            BindingInformation information(protocol, info);

            EXPECT_STREQ(information.QueryHost().c_str(), expectedHost.c_str());
            EXPECT_STREQ(information.QueryPort().c_str(), expectedPort.c_str());
            EXPECT_STREQ(information.QueryProtocol().c_str(), protocol.c_str());
        }
    };

    TEST_F(BindingInformationTest, ParsesInformationCorrectly)
    {
        ParseBindingInformation(L"https", L":80:", L"*", L"80");
        ParseBindingInformation(L"https", L":80:host", L"host", L"80");
        ParseBindingInformation(L"http", L":80:host", L"host", L"80");
        ParseBindingInformation(L"http", L"RANDOM_IP:5:", L"*", L"5");
    }
}
