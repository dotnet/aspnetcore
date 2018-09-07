// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "gtest/internal/gtest-port.h"
#include "PipeOutputManager.h"

class FileManagerWrapper
{
public:
    PipeOutputManager * manager;
    FileManagerWrapper(PipeOutputManager* m)
        : manager(m)
    {
        manager->Start();
    }

    ~FileManagerWrapper()
    {
        delete manager;
    }
};

namespace PipeOutputManagerTests
{
    TEST(PipeManagerOutputTest, StdOut)
    {
        PCWSTR expected = L"test";

        PipeOutputManager* pManager = new PipeOutputManager(true);

        pManager->Start();
        fwprintf(stdout, expected);
        pManager->Stop();

        auto output = pManager->GetStdOutContent();
        ASSERT_STREQ(output.c_str(), expected);
        delete pManager;
    }

    TEST(PipeManagerOutputTest, StdOutMultiToWide)
    {
        PipeOutputManager* pManager = new PipeOutputManager(true);

        pManager->Start();
        fprintf(stdout, "test");
        pManager->Stop();

        auto output = pManager->GetStdOutContent();
        ASSERT_STREQ(output.c_str(), L"test");
        delete pManager;
    }

    TEST(PipeManagerOutputTest, StdErr)
    {
        PCWSTR expected = L"test";

        PipeOutputManager* pManager = new PipeOutputManager();

        pManager->Start();
        fwprintf(stderr, expected);
        pManager->Stop();

        auto output = pManager->GetStdOutContent();
        ASSERT_STREQ(output.c_str(), expected);
        delete pManager;
    }

    TEST(PipeManagerOutputTest, CheckMaxPipeSize)
    {
        std::wstring test;
        for (int i = 0; i < 3000; i++)
        {
            test.append(L"hello world");
        }

        PipeOutputManager* pManager = new PipeOutputManager();

        pManager->Start();
        wprintf(test.c_str());
        pManager->Stop();

        auto output = pManager->GetStdOutContent();
        ASSERT_EQ(output.size(), (DWORD)30000);
        delete pManager;
    }

    TEST(PipeManagerOutputTest, SetInvalidHandlesForErrAndOut)
    {
        auto m_fdPreviousStdOut = _dup(_fileno(stdout));
        auto m_fdPreviousStdErr = _dup(_fileno(stderr));

        SetStdHandle(STD_ERROR_HANDLE, INVALID_HANDLE_VALUE);
        SetStdHandle(STD_OUTPUT_HANDLE, INVALID_HANDLE_VALUE);

        PCWSTR expected = L"test";

        PipeOutputManager* pManager = new PipeOutputManager();
        pManager->Start();

        _dup2(m_fdPreviousStdOut, _fileno(stdout));
        _dup2(m_fdPreviousStdErr, _fileno(stderr));

        // Test will fail if we didn't redirect stdout back to a file descriptor.
        // This is because gtest relies on console output to know if a test succeeded or failed.
        // If the output still points to a file/pipe, the test (and all other tests after it) will fail.
        delete pManager;
    }

    TEST(PipeManagerOutputTest, CreateDeleteMultipleTimesStdOutWorks)
    {
        for (int i = 0; i < 10; i++)
        {
            auto stdoutBefore = _fileno(stdout);
            auto stderrBefore = _fileno(stderr);
            PCWSTR expected = L"test";

            PipeOutputManager* pManager = new PipeOutputManager();

            pManager->Start();
            fwprintf(stdout, expected);

            pManager->Stop();

            auto output = pManager->GetStdOutContent();
            ASSERT_STREQ(output.c_str(), expected);
            ASSERT_EQ(stdoutBefore, _fileno(stdout));
            ASSERT_EQ(stderrBefore, _fileno(stderr));
            delete pManager;
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

            PipeOutputManager* pManager = new PipeOutputManager();

            pManager->Start();
            fwprintf(stderr, expected);
            pManager->Stop();

            auto output = pManager->GetStdOutContent();
            ASSERT_STREQ(output.c_str(), expected);
            ASSERT_EQ(stdoutBefore, _fileno(stdout));

            ASSERT_EQ(stderrBefore, _fileno(stderr));

            delete pManager;
        }

        wprintf(L"Hello!");
    }
}

