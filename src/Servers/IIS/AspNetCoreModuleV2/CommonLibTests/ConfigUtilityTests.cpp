// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "gmock/gmock.h"
using ::testing::_;
using ::testing::DoAll;
using ::testing::NiceMock;

namespace ConfigUtilityTests
{
    using ::testing::Test;

    class ConfigUtilityTest : public Test
    {
    protected:
        void TestHandlerVersion(std::wstring key, std::wstring value, std::wstring expected, HRESULT(*func)(IAppHostElement*, STRU&))
        {
            IAppHostElement* retElement = NULL;

            STRU handlerVersion;

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
                .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(key.c_str())), testing::Return(S_OK)))
                .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(value.c_str())), testing::Return(S_OK)));

            HRESULT hr = func(element.get(), handlerVersion);

            EXPECT_EQ(hr, S_OK);
            EXPECT_STREQ(handlerVersion.QueryStr(), expected.c_str());
        }
    };

    TEST_F(ConfigUtilityTest, CheckHandlerVersionKeysAndValues)
    {
        auto func = ConfigUtility::FindHandlerVersion;
        TestHandlerVersion(L"handlerVersion", L"value", L"value", func);
        TestHandlerVersion(L"handlerversion", L"value", L"value", func);
        TestHandlerVersion(L"HandlerversioN", L"value", L"value", func);
        TestHandlerVersion(L"randomvalue", L"value", L"", func);
        TestHandlerVersion(L"", L"value", L"", func);
        TestHandlerVersion(L"", L"", L"", func);
    }

    TEST_F(ConfigUtilityTest, CheckDebugLogFile)
    {
        auto func = ConfigUtility::FindDebugFile;

        TestHandlerVersion(L"debugFile", L"value", L"value", func);
        TestHandlerVersion(L"debugFILE", L"value", L"value", func);
    }

    TEST_F(ConfigUtilityTest, CheckDebugLevel)
    {
        auto func = ConfigUtility::FindDebugLevel;

        TestHandlerVersion(L"debugLevel", L"value", L"value", func);
        TestHandlerVersion(L"debugLEVEL", L"value", L"value", func);
    }

    TEST(ConfigUtilityTestSingle, MultipleElements)
    {
        IAppHostElement* retElement = NULL;
        STRU handlerVersion;

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
            .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(L"key")), testing::Return(S_OK)))
            .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(L"value")), testing::Return(S_OK)))
            .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(L"handlerVersion")), testing::Return(S_OK)))
            .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(L"value2")), testing::Return(S_OK)));

        HRESULT hr = ConfigUtility::FindHandlerVersion(element.get(), handlerVersion);

        EXPECT_EQ(hr, S_OK);
        EXPECT_STREQ(handlerVersion.QueryStr(), L"value2");
    }

    TEST(ConfigUtilityTestSingle, IgnoresFailedGetElement)
    {
        STRU handlerVersion;

        auto element = std::make_unique<NiceMock<MockElement>>();
        ON_CALL(*element, GetElementByName(_, _))
            .WillByDefault(DoAll(testing::SetArgPointee<1>(nullptr), testing::Return(HRESULT_FROM_WIN32( ERROR_INVALID_INDEX ))));

        HRESULT hr = ConfigUtility::FindHandlerVersion(element.get(), handlerVersion);

        EXPECT_EQ(hr, S_OK);
        EXPECT_STREQ(handlerVersion.QueryStr(), L"");
    }
}
