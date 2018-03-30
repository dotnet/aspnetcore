// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "CppUnitTest.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace AspNetCoreModuleTests
{
    TEST_CLASS(UTILITY_TESTS)
    {
    public:

        TEST_METHOD(PassUnexpandedString_ExpandsResult)
        {
            HRESULT hr = S_OK;
            PCWSTR unexpandedString = L"ANCM_TEST_ENV_VAR";
            PCWSTR unexpandedStringValue = L"foobar";
            STRU   struExpandedString;
            SetEnvironmentVariable(L"ANCM_TEST_ENV_VAR", unexpandedStringValue);

            hr = struExpandedString.CopyAndExpandEnvironmentStrings(L"%ANCM_TEST_ENV_VAR%");
            Assert::AreEqual(hr, S_OK);
            Assert::AreEqual(L"foobar", struExpandedString.QueryStr());
        }

        TEST_METHOD(PassUnexpandedString_Resize_ExpandsResult)
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
            Assert::AreEqual(hr, S_OK);
            Assert::AreEqual(struStringValue.QueryCCH(), struExpandedString.QueryCCH());
            // The values are exactly the same, however Assert::AreEqual is returning false.
            //Assert::AreEqual(struStringValue.QueryStr(), struExpandedString.QueryStr());
            Assert::AreEqual(0, wcscmp(struStringValue.QueryStr(), struExpandedString.QueryStr()));
        }
    };
}
