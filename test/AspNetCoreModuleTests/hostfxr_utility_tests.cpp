// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "CppUnitTest.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace AspNetCoreModuleTests
{
    TEST_CLASS(HOSTFXR_UTILITY_TESTS)
    {
    public:

        TEST_METHOD(ParseHostfxrArguments_BasicHostFxrArguments)
        {
            DWORD retVal = 0;
            BSTR* bstrArray;
            PCWSTR exeStr = L"C:/Program Files/dotnet.exe";

            HRESULT hr = HOSTFXR_UTILITY::ParseHostfxrArguments(
                L"exec \"test.dll\"", // args
                exeStr,  // exe path
                L"invalid",  // physical path to application
                NULL, // event log
                &retVal, // arg count
                &bstrArray); // args array.

            Assert::AreEqual(hr, S_OK);
            Assert::AreEqual(DWORD(3), retVal);
            Assert::AreEqual(exeStr, bstrArray[0]);
            Assert::AreEqual(L"exec", bstrArray[1]);
            Assert::AreEqual(L"test.dll", bstrArray[2]);
        }

        TEST_METHOD(ParseHostfxrArguments_NoExecProvided)
        {
            DWORD retVal = 0;
            BSTR* bstrArray;
            PCWSTR exeStr = L"C:/Program Files/dotnet.exe";

            HRESULT hr = HOSTFXR_UTILITY::ParseHostfxrArguments(
                L"test.dll", // args
                exeStr,  // exe path
                L"ignored",  // physical path to application
                NULL, // event log
                &retVal, // arg count
                &bstrArray); // args array.

            Assert::AreEqual(hr, S_OK);
            Assert::AreEqual(DWORD(2), retVal);
            Assert::AreEqual(exeStr, bstrArray[0]);
            Assert::AreEqual(L"test.dll", bstrArray[1]);
        }

        TEST_METHOD(ParseHostfxrArguments_ConvertDllToAbsolutePath)
        {
            DWORD retVal = 0;
            BSTR* bstrArray;
            PCWSTR exeStr = L"C:/Program Files/dotnet.exe";

            HRESULT hr = HOSTFXR_UTILITY::ParseHostfxrArguments(
                L"exec \"test.dll\"", // args
                exeStr,  // exe path
                L"C:/test",  // physical path to application
                NULL, // event log
                &retVal, // arg count
                &bstrArray); // args array.

            Assert::AreEqual(hr, S_OK);
            Assert::AreEqual(DWORD(3), retVal);
            Assert::AreEqual(exeStr, bstrArray[0]);
            Assert::AreEqual(L"exec", bstrArray[1]);
            Assert::AreEqual(L"\\\\?\\C:\\test\\test.dll", bstrArray[2]);
        }

        TEST_METHOD(ParseHostfxrArguments_ProvideNoArgs_InvalidArgs)
        {
            DWORD retVal = 0;
            BSTR* bstrArray;
            PCWSTR exeStr = L"C:/Program Files/dotnet.exe";

            HRESULT hr = HOSTFXR_UTILITY::ParseHostfxrArguments(
                L"", // args
                exeStr,  // exe path
                L"ignored",  // physical path to application
                NULL, // event log
                &retVal, // arg count
                &bstrArray); // args array.

            Assert::AreEqual(E_INVALIDARG, hr);
        }
    };
}
