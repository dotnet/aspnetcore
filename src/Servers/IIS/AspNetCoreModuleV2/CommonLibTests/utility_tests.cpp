// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "Environment.h"
#include "StringHelpers.h"

TEST(PassUnexpandedEnvString, ExpandsResult)
{
    HRESULT hr = S_OK;
    PCWSTR unexpandedString = L"ANCM_TEST_ENV_VAR";
    PCWSTR unexpandedStringValue = L"foobar";
    STRU   struExpandedString;
    SetEnvironmentVariable(L"ANCM_TEST_ENV_VAR", unexpandedStringValue);

    hr = struExpandedString.CopyAndExpandEnvironmentStrings(L"%ANCM_TEST_ENV_VAR%");
    EXPECT_EQ(hr, S_OK);
    EXPECT_STREQ(L"foobar", struExpandedString.QueryStr());
}

TEST(PassUnexpandedEnvString, LongStringExpandsResults)
{
    HRESULT hr = S_OK;
    PCWSTR unexpandedString = L"ANCM_TEST_ENV_VAR_LONG";
    STRU   struStringValue;
    STACK_STRU(struExpandedString, MAX_PATH);

    struStringValue.Append(L"TestValueThatIsLongerThan256CharactersLongToTriggerResize");
    struStringValue.Append(L"TestValueThatIsLongerThan256CharactersLongToTriggerResize");
    struStringValue.Append(L"TestValueThatIsLongerThan256CharactersLongToTriggerResize");
    struStringValue.Append(L"TestValueThatIsLongerThan256CharactersLongToTriggerResize");
    struStringValue.Append(L"TestValueThatIsLongerThan256CharactersLongToTriggerResize");
    struStringValue.Append(L"TestValueThatIsLongerThan256CharactersLongToTriggerResize");

    SetEnvironmentVariable(unexpandedString, struStringValue.QueryStr());

    hr = struExpandedString.CopyAndExpandEnvironmentStrings(L"%ANCM_TEST_ENV_VAR_LONG%");
    EXPECT_EQ(hr, S_OK);
    EXPECT_EQ(struStringValue.QueryCCH(), struExpandedString.QueryCCH());
    // The values are exactly the same, however EXPECT_EQ is returning false.
    //EXPECT_EQ(struStringValue.QueryStr(), struExpandedString.QueryStr());
    EXPECT_STREQ(struStringValue.QueryStr(), struExpandedString.QueryStr());
}


TEST(GetEnvironmentVariableValue, ReturnsCorrectLength)
{
    SetEnvironmentVariable(L"RANDOM_ENV_VAR_1", L"test");

    auto result = Environment::GetEnvironmentVariableValue(L"RANDOM_ENV_VAR_1");
    EXPECT_TRUE(result.has_value());
    EXPECT_EQ(result.value().length(), 4);
    EXPECT_STREQ(result.value().c_str(), L"test");
}


TEST(GetEnvironmentVariableValue, ReturnsNulloptWhenNotFound)
{
    auto result = Environment::GetEnvironmentVariableValue(L"RANDOM_ENV_VAR_2");
    EXPECT_FALSE(result.has_value());
}

TEST(CheckStringHelpers, FormatWithoutContentDoesNotIncreaseSizeString)
{
    std::string testString = "test";
    auto result = format(testString);
    EXPECT_EQ(testString.size(), result.size());
}

TEST(CheckStringHelpers, FormatWithoutContentDoesNotIncreaseSizeWstring)
{
    std::wstring testString = L"test";
    auto result = format(testString);
    EXPECT_EQ(testString.size(), result.size());
}
