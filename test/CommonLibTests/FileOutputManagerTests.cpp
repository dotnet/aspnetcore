// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "gtest/internal/gtest-port.h"

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

            std::wstring tempDirectory = Helpers::CreateRandomTempDirectory();
            FileOutputManager* pManager = new FileOutputManager;
            pManager->Initialize(fileNamePrefix.c_str(), tempDirectory.c_str());
            {
                FileManagerWrapper wrapper(pManager);

                wprintf(expected, out);
            }
          
            // std::filesystem is available on c++17, however gtest fails to build when using it
            // c++14 has filesystem as experimental.
            for (auto & p : std::experimental::filesystem::directory_iterator(tempDirectory))
            {
                std::wstring filename(p.path().filename());
                ASSERT_EQ(filename.substr(0, fileNamePrefix.size()), fileNamePrefix);

                std::wstring content = Helpers::ReadFileContent(std::wstring(p.path()));
                ASSERT_EQ(content.length(), DWORD(4));
                ASSERT_STREQ(content.c_str(), expected);
            }

            Helpers::DeleteDirectory(tempDirectory);
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

    TEST(FileOutManagerOutputTest, StdErr)
    {
        PCSTR expected = "test";

        std::wstring tempDirectory = Helpers::CreateRandomTempDirectory();

        FileOutputManager* pManager = new FileOutputManager;
        pManager->Initialize(L"", tempDirectory.c_str());
        {
            FileManagerWrapper wrapper(pManager);

            printf(expected, stderr);
            STRA straContent;
            ASSERT_TRUE(pManager->GetStdOutContent(&straContent));

            ASSERT_STREQ(straContent.QueryStr(), expected);
        }

        Helpers::DeleteDirectory(tempDirectory);
    }

    TEST(FileOutManagerOutputTest, CheckFileOutput)
    {
        PCSTR expected = "test";

        std::wstring tempDirectory = Helpers::CreateRandomTempDirectory();

        FileOutputManager* pManager = new FileOutputManager;
        pManager->Initialize(L"", tempDirectory.c_str());
        {
            FileManagerWrapper wrapper(pManager);

            printf(expected);
            STRA straContent;
            ASSERT_TRUE(pManager->GetStdOutContent(&straContent));

            ASSERT_STREQ(straContent.QueryStr(), expected);
        }

        Helpers::DeleteDirectory(tempDirectory);
    }

    TEST(FileOutManagerOutputTest, CapAt4KB)
    {
        PCSTR expected = "test";

        std::wstring tempDirectory = Helpers::CreateRandomTempDirectory();

        FileOutputManager* pManager = new FileOutputManager;
        pManager->Initialize(L"", tempDirectory.c_str());
        {
            FileManagerWrapper wrapper(pManager);

            for (int i = 0; i < 1200; i++)
            {
                printf(expected);
            }

            STRA straContent;
            ASSERT_TRUE(pManager->GetStdOutContent(&straContent));

            ASSERT_EQ(straContent.QueryCCH(), 4096);
        }

        Helpers::DeleteDirectory(tempDirectory);
    }
}

