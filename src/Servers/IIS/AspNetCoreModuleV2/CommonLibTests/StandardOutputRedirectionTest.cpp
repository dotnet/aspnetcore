// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "gtest/internal/gtest-port.h"
#include "StandardStreamRedirection.h"

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
                StandardStreamRedirection pManager(redirectionOutput, false);

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

namespace PipeOutputManagerTests
{
    TEST(PipeManagerOutputTest, StdOut)
    {
        PCWSTR expected = L"test";

        StringStreamRedirectionOutput redirectionOutput;
        {
            StandardStreamRedirection pManager(redirectionOutput, false);
            fwprintf(stdout, expected);
        }

        auto output = redirectionOutput.GetOutput();
        ASSERT_STREQ(output.c_str(), expected);
    }

    TEST(PipeManagerOutputTest, StdOutMultiToWide)
    {
        StringStreamRedirectionOutput redirectionOutput;
        {
            StandardStreamRedirection pManager(redirectionOutput, false);
            fprintf(stdout, "test");
        }
        auto output = redirectionOutput.GetOutput();
        ASSERT_STREQ(output.c_str(), L"test");
    }

    TEST(PipeManagerOutputTest, StdErr)
    {
        PCWSTR expected = L"test";

        StringStreamRedirectionOutput redirectionOutput;
        {
            StandardStreamRedirection pManager(redirectionOutput, false);
            fwprintf(stderr, expected);
        }

        auto output = redirectionOutput.GetOutput();
        ASSERT_STREQ(output.c_str(), expected);
    }

    TEST(PipeManagerOutputTest, CheckMaxPipeSize)
    {
        std::wstring test;
        for (int i = 0; i < 3000; i++)
        {
            test.append(L"hello world");
        }

        StringStreamRedirectionOutput redirectionOutput;
        {
            StandardStreamRedirection pManager(redirectionOutput, false);
            wprintf(test.c_str());
        }

        auto output = redirectionOutput.GetOutput();
        ASSERT_EQ(output.size(), (DWORD)30000);
    }

    TEST(PipeManagerOutputTest, SetInvalidHandlesForErrAndOut)
    {
        auto m_fdPreviousStdOut = _dup(_fileno(stdout));
        auto m_fdPreviousStdErr = _dup(_fileno(stderr));

        SetStdHandle(STD_ERROR_HANDLE, INVALID_HANDLE_VALUE);
        SetStdHandle(STD_OUTPUT_HANDLE, INVALID_HANDLE_VALUE);

        StringStreamRedirectionOutput redirectionOutput;
        {
            StandardStreamRedirection pManager(redirectionOutput, false);
            _dup2(m_fdPreviousStdOut, _fileno(stdout));
            _dup2(m_fdPreviousStdErr, _fileno(stderr));

            // Test will fail if we didn't redirect stdout back to a file descriptor.
            // This is because gtest relies on console output to know if a test succeeded or failed.
            // If the output still points to a file/pipe, the test (and all other tests after it) will fail.
        }
    }

    TEST(PipeManagerOutputTest, CreateDeleteMultipleTimesStdOutWorks)
    {
        for (int i = 0; i < 10; i++)
        {
            auto stdoutBefore = _fileno(stdout);
            auto stderrBefore = _fileno(stderr);
            PCWSTR expected = L"test";

            StringStreamRedirectionOutput redirectionOutput;
            {
                StandardStreamRedirection pManager(redirectionOutput, false);
                fwprintf(stdout, expected);
            }

            auto output = redirectionOutput.GetOutput();
            ASSERT_STREQ(output.c_str(), expected);
            ASSERT_EQ(stdoutBefore, _fileno(stdout));
            ASSERT_EQ(stderrBefore, _fileno(stderr));
        }
        // When this returns, we get an AV from gtest.
    }

    TEST(PipeManagerOutputTest, CreateDeleteKeepOriginalStdErr)
    {
        for (int i = 0; i < 10; i++)
        {
            auto stdoutBefore = _fileno(stdout);
            auto stderrBefore = _fileno(stderr);
            auto stdoutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            auto stderrHandle = GetStdHandle(STD_ERROR_HANDLE);
            PCWSTR expected = L"test";

            StringStreamRedirectionOutput redirectionOutput;
            {
                StandardStreamRedirection pManager(redirectionOutput, false);
                fwprintf(stderr, expected);
            }

            auto output = redirectionOutput.GetOutput();
            ASSERT_STREQ(output.c_str(), expected);
            ASSERT_EQ(stdoutBefore, _fileno(stdout));

            ASSERT_EQ(stderrBefore, _fileno(stderr));
        }

        wprintf(L"Hello!");
    }


    TEST(StringStreamRedirectionOutputTest, StdOut)
    {
        PCWSTR expected = L"test";

        {
            StringStreamRedirectionOutput redirectionOutput;
            {
                StandardStreamRedirection pManager(redirectionOutput, false);
                fwprintf(stdout, expected);
            }

            auto output = redirectionOutput.GetOutput();
            ASSERT_FALSE(output.empty());

            ASSERT_STREQ(output.c_str(), expected);
        }
    }

    TEST(StringStreamRedirectionOutputTest, StdErr)
    {
        PCWSTR expected = L"test";

        StringStreamRedirectionOutput redirectionOutput;
        {
            StandardStreamRedirection pManager(redirectionOutput, false);
            fwprintf(stderr, expected);
        }

        auto output = redirectionOutput.GetOutput();
        ASSERT_FALSE(output.empty());

        ASSERT_STREQ(output.c_str(), expected);
    }

    TEST(StringStreamRedirectionOutputTest, CapAt30KB)
    {
        PCWSTR expected = L"hello world";

        auto tempDirectory = TempDirectory();

        StringStreamRedirectionOutput redirectionOutput;
        {
            StandardStreamRedirection pManager(redirectionOutput, false);
            for (int i = 0; i < 3000; i++)
            {
                wprintf(expected);
            }
        }

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
            {
                StandardStreamRedirection pManager(redirectionOutput, false);
                wprintf(expected);
            }
            auto output = redirectionOutput.GetOutput();
            ASSERT_FALSE(output.empty());

            ASSERT_STREQ(output.c_str(), expected);
        }
    }
}

