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
            FileOutputManager* pManager = new FileOutputManager;

            pManager->Initialize(fileNamePrefix.c_str(), tempDirectory.path().c_str());
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
        PCSTR expected = "test";

        auto tempDirectory = TempDirectory();

        FileOutputManager* pManager = new FileOutputManager;
        pManager->Initialize(L"", tempDirectory.path().c_str());
        {
            FileManagerWrapper wrapper(pManager);

            fprintf(stdout, expected);
            pManager->Stop();

            STRA straContent;
            ASSERT_TRUE(pManager->GetStdOutContent(&straContent));

            ASSERT_STREQ(straContent.QueryStr(), expected);
        }
    }

    TEST(FileOutManagerOutputTest, StdErr)
    {
        PCSTR expected = "test";

        auto tempDirectory = TempDirectory();

        FileOutputManager* pManager = new FileOutputManager;
        pManager->Initialize(L"", tempDirectory.path().c_str());
        {
            FileManagerWrapper wrapper(pManager);

            fprintf(stderr, expected);
            pManager->Stop();

            STRA straContent;
            ASSERT_TRUE(pManager->GetStdOutContent(&straContent));

            ASSERT_STREQ(straContent.QueryStr(), expected);
        }
    }

    TEST(FileOutManagerOutputTest, CapAt30KB)
    {
        PCSTR expected = "hello world";

        auto tempDirectory = TempDirectory();

        FileOutputManager* pManager = new FileOutputManager;
        pManager->Initialize(L"", tempDirectory.path().c_str());
        {
            FileManagerWrapper wrapper(pManager);

            for (int i = 0; i < 3000; i++)
            {
                printf(expected);
            }
            pManager->Stop();
            STRA straContent;
            ASSERT_TRUE(pManager->GetStdOutContent(&straContent));

            ASSERT_EQ(straContent.QueryCCH(), 30000);
        }
    }


    TEST(FileOutManagerOutputTest, StartStopRestoresCorrectly)
    {
        PCSTR expected = "test";

        auto tempDirectory = TempDirectory();

        for (int i = 0; i < 10; i++)
        {
            FileOutputManager* pManager = new FileOutputManager;
            pManager->Initialize(L"", tempDirectory.path().c_str());
            {
                FileManagerWrapper wrapper(pManager);

                printf(expected);
                pManager->Stop();
                STRA straContent;
                ASSERT_TRUE(pManager->GetStdOutContent(&straContent));

                ASSERT_STREQ(straContent.QueryStr(), expected);
            }
        }
    }
}
