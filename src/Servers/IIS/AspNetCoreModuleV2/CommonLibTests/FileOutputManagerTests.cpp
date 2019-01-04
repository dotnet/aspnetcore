// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "gtest/internal/gtest-port.h"
#include "PipeOutputManager.h"

namespace FileRedirectionOutputTests
{
    using ::testing::Test;
    class FileRedirectionOutputTest : public Test
    {
    protected:
        void
        Test(std::wstring fileNamePrefix, FILE* out)
        {
            PCWSTR expected = L"test";

            auto tempDirectory = TempDirectory();

            {
                FileRedirectionOutput redirectionOutput(tempDirectory.path(), fileNamePrefix);
                PipeOutputManager pManager(redirectionOutput);

                wprintf(expected, out);
            }

            for (auto & p : std::filesystem::directory_iterator(tempDirectory.path()))
            {
                std::wstring filename(p.path().filename());
                ASSERT_EQ(filename.substr(0, fileNamePrefix.size()), fileNamePrefix);

                std::wstring content = Helpers::ReadFileContent(std::wstring(p.path()));
            }
        }
    };

    TEST_F(FileRedirectionOutputTest, WriteToFileCheckContentsWritten)
    {
        Test(L"", stdout);
        Test(L"log", stdout);
    }

    TEST_F(FileRedirectionOutputTest, WriteToFileCheckContentsWrittenErr)
    {
        Test(L"", stderr);
        Test(L"log", stderr);
    }
}

namespace StringStreamRedirectionOutputTests
{
    TEST(StringStreamRedirectionOutputTest, StdOut)
    {
        PCWSTR expected = L"test";

        {
            StringStreamRedirectionOutput redirectionOutput;
            PipeOutputManager pManager(redirectionOutput);

            fwprintf(stdout, expected);
            pManager.Stop();

            auto output = redirectionOutput.GetOutput();
            ASSERT_FALSE(output.empty());

            ASSERT_STREQ(output.c_str(), expected);
        }
    }

    TEST(StringStreamRedirectionOutputTest, StdErr)
    {
        PCWSTR expected = L"test";

        StringStreamRedirectionOutput redirectionOutput;
        PipeOutputManager pManager(redirectionOutput);

        fwprintf(stderr, expected);
        pManager.Stop();

        auto output = redirectionOutput.GetOutput();
        ASSERT_FALSE(output.empty());

        ASSERT_STREQ(output.c_str(), expected);
    }

    TEST(StringStreamRedirectionOutputTest, CapAt30KB)
    {
        PCWSTR expected = L"hello world";

        auto tempDirectory = TempDirectory();

        StringStreamRedirectionOutput redirectionOutput;
        PipeOutputManager pManager(redirectionOutput);
        for (int i = 0; i < 3000; i++)
        {
            wprintf(expected);
        }
        pManager.Stop();
        auto output = redirectionOutput.GetOutput();
        ASSERT_FALSE(output.empty());

        ASSERT_EQ(output.size(), 30000);
    }

    TEST(StringStreamRedirectionOutputTest, StartStopRestoresCorrectly)
    {
        PCWSTR expected = L"test";

        for (int i = 0; i < 10; i++)
        {
            StringStreamRedirectionOutput redirectionOutput;
            PipeOutputManager pManager(redirectionOutput);
            wprintf(expected);
            pManager.Stop();
            auto output = redirectionOutput.GetOutput();
            ASSERT_FALSE(output.empty());

            ASSERT_STREQ(output.c_str(), expected);
        }
    }
}
