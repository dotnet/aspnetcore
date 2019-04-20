// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "gtest/internal/gtest-port.h"
#include "FileOutputManager.h"

class FileManagerWrapper
{
public:
    FileOutputManager* manager;
    FileManagerWrapper(FileOutputManager* m)
        : manager(m)
    {
        manager->Start();
    }

    ~FileManagerWrapper()
    {
        delete manager;
    }
};

namespace FileOutManagerStartupTests
{
    using ::testing::Test;
    class FileOutputManagerTest : public Test
    {
    protected:
        void
        Test(std::wstring fileNamePrefix, FILE* out)
        {
            PCWSTR expected = L"test";

            auto tempDirectory = TempDirectory();
            FileOutputManager* pManager = new FileOutputManager(fileNamePrefix, tempDirectory.path());

            {
                FileManagerWrapper wrapper(pManager);

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

    TEST_F(FileOutputManagerTest, WriteToFileCheckContentsWritten)
    {
        Test(L"", stdout);
        Test(L"log", stdout);
    }

    TEST_F(FileOutputManagerTest, WriteToFileCheckContentsWrittenErr)
    {
        Test(L"", stderr);
        Test(L"log", stderr);
    }
}

namespace FileOutManagerOutputTests
{
    TEST(FileOutManagerOutputTest, StdOut)
    {
        PCWSTR expected = L"test";

        auto tempDirectory = TempDirectory();

        FileOutputManager* pManager = new FileOutputManager(L"", tempDirectory.path());
        {
            FileManagerWrapper wrapper(pManager);

            fwprintf(stdout, expected);
            pManager->Stop();

            auto output = pManager->GetStdOutContent();
            ASSERT_FALSE(output.empty());

            ASSERT_STREQ(output.c_str(), expected);
        }
    }

    TEST(FileOutManagerOutputTest, StdErr)
    {
        PCWSTR expected = L"test";

        auto tempDirectory = TempDirectory();

        FileOutputManager* pManager = new FileOutputManager(L"", tempDirectory.path().c_str());
        {
            FileManagerWrapper wrapper(pManager);

            fwprintf(stderr, expected);
            pManager->Stop();

            auto output = pManager->GetStdOutContent();
            ASSERT_FALSE(output.empty());

            ASSERT_STREQ(output.c_str(), expected);
        }
    }

    TEST(FileOutManagerOutputTest, CapAt30KB)
    {
        PCWSTR expected = L"hello world";

        auto tempDirectory = TempDirectory();

        FileOutputManager* pManager = new FileOutputManager(L"", tempDirectory.path());
        {
            FileManagerWrapper wrapper(pManager);

            for (int i = 0; i < 3000; i++)
            {
                wprintf(expected);
            }
            pManager->Stop();
            auto output = pManager->GetStdOutContent();
            ASSERT_FALSE(output.empty());

            ASSERT_EQ(output.size(), 30000);
        }
    }

    TEST(FileOutManagerOutputTest, StartStopRestoresCorrectly)
    {
        PCWSTR expected = L"test";

        auto tempDirectory = TempDirectory();

        for (int i = 0; i < 10; i++)
        {
            FileOutputManager* pManager = new FileOutputManager(L"", tempDirectory.path());
            {
                FileManagerWrapper wrapper(pManager);

                wprintf(expected);
                pManager->Stop();
                auto output = pManager->GetStdOutContent();
                ASSERT_FALSE(output.empty());

                ASSERT_STREQ(output.c_str(), expected);
            }
        }
    }
}
