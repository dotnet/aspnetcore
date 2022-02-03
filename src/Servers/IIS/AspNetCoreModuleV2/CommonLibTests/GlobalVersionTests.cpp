// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include "gtest/internal/gtest-port.h"

namespace GlobalVersionTests
{
    using ::testing::Test;
    namespace fs = std::filesystem;

    class GlobalVersionTest : public Test
    {
    protected:
        void
        RemoveFileNamePath(PCWSTR dllPath, PCWSTR expected)
        {
            std::wstring res = GlobalVersionUtility::RemoveFileNameFromFolderPath(dllPath);
            EXPECT_STREQ(res.c_str(), expected);
        }
    };

    TEST_F(GlobalVersionTest, RemovesPathCorrectly)
    {
        RemoveFileNamePath(L"test\\log.txt", L"test");
        RemoveFileNamePath(L"test\\log", L"test");
        RemoveFileNamePath(L"C:\\Program Files\\IIS\\aspnetcorev2.dll", L"C:\\Program Files\\IIS");
        RemoveFileNamePath(L"test\\log.txt", L"test");
    }

    TEST(GetRequestHandlerVersions, GetFolders)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / L"2.0.0"));

        auto res = GlobalVersionUtility::GetRequestHandlerVersions(tempPath.path().c_str());
        EXPECT_EQ(res.size(), 1);
        EXPECT_EQ(res.at(0), fx_ver_t(2, 0, 0, std::wstring()));
    }

    TEST(GetRequestHandlerVersions, GetFolderPreview)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / L"2.0.0-preview"));

        auto res = GlobalVersionUtility::GetRequestHandlerVersions(tempPath.path().c_str());
        EXPECT_EQ(res.size(), 1);
        EXPECT_EQ(res.at(0), fx_ver_t(2, 0, 0, std::wstring(L"-preview")));
    }

    TEST(GetRequestHandlerVersions, GetFolderManyVersions)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / + L"2.0.0"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / + L"1.9.0"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / + L"2.1.0"));

        auto res = GlobalVersionUtility::GetRequestHandlerVersions(tempPath.path().c_str());
        EXPECT_EQ(res.size(), 3);
        EXPECT_TRUE(std::find(res.begin(), res.end(), fx_ver_t(1, 9, 0, std::wstring())) != std::end(res));
        EXPECT_TRUE(std::find(res.begin(), res.end(), fx_ver_t(2, 0, 0, std::wstring())) != std::end(res));
        EXPECT_TRUE(std::find(res.begin(), res.end(), fx_ver_t(2, 1, 0, std::wstring())) != std::end(res));
    }

    TEST(FindHighestGlobalVersion, HighestVersionWithSingleFolder)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.0.0"));

        auto res = GlobalVersionUtility::FindHighestGlobalVersion(tempPath.path().c_str());

        EXPECT_STREQ(res.c_str(), L"2.0.0");
    }

    TEST(FindHighestGlobalVersion, HighestVersionWithMultipleVersions)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.0.0"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.1.0"));

        auto res = GlobalVersionUtility::FindHighestGlobalVersion(tempPath.path().c_str());

        EXPECT_STREQ(res.c_str(), L"2.1.0");
    }

    // Sem version 2.0 will not be used with ANCM out of process handler, but it's the most convenient way to test it.
    TEST(FindHighestGlobalVersion, HighestVersionWithSemVersion20)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.1.0-preview"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.1.0-preview.1.1"));

        auto res = GlobalVersionUtility::FindHighestGlobalVersion(tempPath.path().c_str());

        EXPECT_STREQ(res.c_str(), L"2.1.0-preview.1.1");
    }

    TEST(FindHighestGlobalVersion, HighestVersionWithMultipleVersionsPreview)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.0.0"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.1.0"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.2.0-preview"));

        auto res = GlobalVersionUtility::FindHighestGlobalVersion(tempPath.path().c_str());

        EXPECT_STREQ(res.c_str(), L"2.2.0-preview");
    }

    TEST(FindHighestGlobalVersion, HighestVersionWithMultipleVersionNoPreview)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.0.0"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.1.0-preview"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.1.0"));

        auto res = GlobalVersionUtility::FindHighestGlobalVersion(tempPath.path().c_str());

        EXPECT_STREQ(res.c_str(), L"2.1.0");
    }

    TEST(GetGlobalRequestHandlerPath, FindHighestVersionNoHandlerName)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.0.0"));
        auto result = GlobalVersionUtility::GetGlobalRequestHandlerPath(tempPath.path().c_str(), L"", L"aspnetcorev2_outofprocess.dll");

        EXPECT_STREQ(result.c_str(), (tempPath.path() / L"2.0.0\\aspnetcorev2_outofprocess.dll").c_str());
    }

    TEST(GetGlobalRequestHandlerPath, FindHighestVersionPreviewWins)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.0.0"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.1.0-preview"));

        auto result = GlobalVersionUtility::GetGlobalRequestHandlerPath(tempPath.path().c_str(), L"", L"aspnetcorev2_outofprocess.dll");

        EXPECT_STREQ(result.c_str(), (tempPath.path() / L"2.1.0-preview\\aspnetcorev2_outofprocess.dll").c_str());
    }

    TEST(GetGlobalRequestHandlerPath, FindHighestVersionSpecificVersion)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.0.0"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.1.0-preview"));

        auto result = GlobalVersionUtility::GetGlobalRequestHandlerPath(tempPath.path().c_str(), L"2.0.0", L"aspnetcorev2_outofprocess.dll");

        EXPECT_STREQ(result.c_str(), (tempPath.path() / L"2.0.0\\aspnetcorev2_outofprocess.dll").c_str());
    }

    TEST(GetGlobalRequestHandlerPath, FindHighestVersionSpecificPreview)
    {
        auto tempPath = TempDirectory();
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.0.0"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.1.0-preview"));
        EXPECT_TRUE(fs::create_directories(tempPath.path() / "2.2.0"));


        auto result = GlobalVersionUtility::GetGlobalRequestHandlerPath(tempPath.path().c_str(), L"2.1.0-preview", L"aspnetcorev2_outofprocess.dll");

        EXPECT_STREQ(result.c_str(), (tempPath.path() / L"2.1.0-preview\\aspnetcorev2_outofprocess.dll").c_str());
    }
}
