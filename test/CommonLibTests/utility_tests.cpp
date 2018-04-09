// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"

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
