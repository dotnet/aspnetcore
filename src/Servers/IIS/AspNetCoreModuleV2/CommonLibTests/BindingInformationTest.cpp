// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
