// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "gmock/gmock.h"
using ::testing::_;
using ::testing::NiceMock;

namespace ConfigUtilityTests
{
    using ::testing::Test;

    class ConfigUtilityTest : public Test
    {
    protected:
        void Test(std::wstring key, std::wstring value, std::wstring expected)
        {
            IAppHostElement* retElement = NULL;

            STRU handlerVersion;
            BSTR bstrKey = SysAllocString(key.c_str());
            BSTR bstrValue = SysAllocString(value.c_str());

            // NiceMock removes warnings about "uninteresting calls",
            auto element = std::make_unique<NiceMock<MockElement>>();
            auto innerElement = std::make_unique<NiceMock<MockElement>>();
            auto collection = std::make_unique<NiceMock<MockCollection>>();
            auto nameElement = std::make_unique<NiceMock<MockElement>>();
            auto mockProperty = std::make_unique<NiceMock<MockProperty>>();

            ON_CALL(*element, GetElementByName(_, _))
                .WillByDefault(DoAll(testing::SetArgPointee<1>(innerElement.get()), testing::Return(S_OK)));
            ON_CALL(*innerElement, get_Collection(_))
                .WillByDefault(testing::DoAll(testing::SetArgPointee<0>(collection.get()), testing::Return(S_OK)));
            ON_CALL(*collection, get_Count(_))
                .WillByDefault(DoAll(testing::SetArgPointee<0>(1), testing::Return(S_OK)));
            ON_CALL(*collection, get_Item(_, _))
                .WillByDefault(DoAll(testing::SetArgPointee<1>(nameElement.get()), testing::Return(S_OK)));
            ON_CALL(*nameElement, GetPropertyByName(_, _))
                .WillByDefault(DoAll(testing::SetArgPointee<1>(mockProperty.get()), testing::Return(S_OK)));
            EXPECT_CALL(*mockProperty, get_StringValue(_))
                .WillOnce(DoAll(testing::SetArgPointee<0>(bstrKey), testing::Return(S_OK)))
                .WillOnce(DoAll(testing::SetArgPointee<0>(bstrValue), testing::Return(S_OK)));

            HRESULT hr = ConfigUtility::FindHandlerVersion(element.get(), &handlerVersion);

            EXPECT_STREQ(handlerVersion.QueryStr(), expected.c_str());

            SysFreeString(bstrKey);
            SysFreeString(bstrValue);
        }
    };

    TEST_F(ConfigUtilityTest, CheckHandlerVersionKeysAndValues)
    {
        Test(L"handlerVersion", L"value", L"value");
        Test(L"handlerversion", L"value", L"value");
        Test(L"HandlerversioN", L"value", L"value");
        Test(L"randomvalue", L"value", L"");
        Test(L"", L"value", L"");
        Test(L"", L"", L"");
    }

    TEST(ConfigUtilityTestSingle, MultipleElements)
    {
        IAppHostElement* retElement = NULL;
        STRU handlerVersion;

        BSTR bstrKey = SysAllocString(L"key");
        BSTR bstrValue = SysAllocString(L"value");
        BSTR bstrKey2 = SysAllocString(L"handlerVersion");
        BSTR bstrValue2  = SysAllocString(L"value2");

        auto element = std::make_unique<NiceMock<MockElement>>();
        auto innerElement = std::make_unique<NiceMock<MockElement>>();
        auto collection = std::make_unique<NiceMock<MockCollection>>();
        auto nameElement = std::make_unique<NiceMock<MockElement>>();
        auto mockProperty = std::make_unique<NiceMock<MockProperty>>();

        ON_CALL(*element, GetElementByName(_, _))
            .WillByDefault(DoAll(testing::SetArgPointee<1>(innerElement.get()), testing::Return(S_OK)));
        ON_CALL(*innerElement, get_Collection(_))
            .WillByDefault(testing::DoAll(testing::SetArgPointee<0>(collection.get()), testing::Return(S_OK)));
        ON_CALL(*collection, get_Count(_))
            .WillByDefault(DoAll(testing::SetArgPointee<0>(2), testing::Return(S_OK)));
        ON_CALL(*collection, get_Item(_, _))
            .WillByDefault(DoAll(testing::SetArgPointee<1>(nameElement.get()), testing::Return(S_OK)));
        ON_CALL(*nameElement, GetPropertyByName(_, _))
            .WillByDefault(DoAll(testing::SetArgPointee<1>(mockProperty.get()), testing::Return(S_OK)));
        EXPECT_CALL(*mockProperty, get_StringValue(_))
            .WillOnce(DoAll(testing::SetArgPointee<0>(bstrKey), testing::Return(S_OK)))
            .WillOnce(DoAll(testing::SetArgPointee<0>(bstrValue), testing::Return(S_OK)))
            .WillOnce(DoAll(testing::SetArgPointee<0>(bstrKey2), testing::Return(S_OK)))
            .WillOnce(DoAll(testing::SetArgPointee<0>(bstrValue2), testing::Return(S_OK)));

        HRESULT hr = ConfigUtility::FindHandlerVersion(element.get(), &handlerVersion);

        EXPECT_STREQ(handlerVersion.QueryStr(), L"value2");

        SysFreeString(bstrKey);
        SysFreeString(bstrValue);
        SysFreeString(bstrKey2);
        SysFreeString(bstrValue2);
    }
}
