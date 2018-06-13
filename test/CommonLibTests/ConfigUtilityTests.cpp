// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "gmock/gmock.h"
using ::testing::_;
using ::testing::NiceMock;

namespace ConfigUtilityTests
{
    TEST(ConfigUtilityTest, HandlerVersionSet)
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
            .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(L"handlerVersion")), testing::Return(S_OK)))
            .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(L"value")), testing::Return(S_OK)));

        HRESULT hr = ConfigUtility::FindHandlerVersion(element.get(), &handlerVersion);

        EXPECT_STREQ(handlerVersion.QueryStr(), L"value");
    }

    TEST(ConfigUtilityTest, NoHandlerVersion)
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
            .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(L"randomvalue")), testing::Return(S_OK)))
            .WillOnce(DoAll(testing::SetArgPointee<0>(SysAllocString(L"value")), testing::Return(S_OK)));

        HRESULT hr = ConfigUtility::FindHandlerVersion(element.get(), &handlerVersion);

        EXPECT_STREQ(handlerVersion.QueryStr(), L"");
    }

    TEST(ConfigUtilityTest, MultipleElements)
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

        HRESULT hr = ConfigUtility::FindHandlerVersion(element.get(), &handlerVersion);

        EXPECT_STREQ(handlerVersion.QueryStr(), L"value2");
    }
}
