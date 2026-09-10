// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "filewatcher.h"
#include "AppOfflineTrackingApplication.h"
#include "fakeclasses.h"

class FileWatcherTests : public testing::Test
{
protected:
    void SetUp() override
    {
        EXPECT_CALL(m_mockHttpApplication, GetApplicationPhysicalPath())
            .WillRepeatedly(testing::Return(m_applicationPath.c_str()));
        EXPECT_CALL(m_mockHttpApplication, GetApplicationId())
            .WillRepeatedly(testing::Return(L"/TestApp"));
        EXPECT_CALL(m_mockHttpApplication, GetAppConfigPath())
            .WillRepeatedly(testing::Return(L"/TestApp/web.config"));
    }

    void SetupFileChangeNotification(
        FILE_WATCHER& watcher,
        AppOfflineTrackingApplication* pApplication,
        PCWSTR pszWatchedFileName,
        PCWSTR pszNotificationFileName,
        DWORD& outSize)
    {
        watcher._pApplication = ReferenceApplication(pApplication);

        HRESULT hr = watcher._strFileName.Copy(pszWatchedFileName);
        ASSERT_TRUE(SUCCEEDED(hr));

        const DWORD fileNameLength = static_cast<DWORD>(wcslen(pszNotificationFileName) * sizeof(WCHAR));
        outSize = FIELD_OFFSET(FILE_NOTIFY_INFORMATION, FileName) + fileNameLength;

        ASSERT_TRUE(watcher._buffDirectoryChanges.Resize(outSize));

        auto pNotificationInfo = reinterpret_cast<FILE_NOTIFY_INFORMATION*>(watcher._buffDirectoryChanges.QueryPtr());
        memset(pNotificationInfo, 0, outSize);
        pNotificationInfo->NextEntryOffset = 0;
        pNotificationInfo->Action = FILE_ACTION_MODIFIED;
        pNotificationInfo->FileNameLength = fileNameLength;
        memcpy(pNotificationInfo->FileName, pszNotificationFileName, fileNameLength);
    }

    MockHttpApplication m_mockHttpApplication;
    std::wstring m_applicationPath = L"C:\\TestApp";
};

TEST_F(FileWatcherTests, HandleChangeCompletion_AppOfflineExactMatch_IsDetected)
{
    AppOfflineTrackingApplication* pApplication = new MockAppOfflineTrackingApplication(m_mockHttpApplication);
    FILE_WATCHER watcher;

    DWORD notifySize = 0;
    SetupFileChangeNotification(watcher, pApplication, L"app_offline.htm", L"app_offline.htm", notifySize);

    HRESULT hr = watcher.HandleChangeCompletion(notifySize);
    ASSERT_TRUE(SUCCEEDED(hr));

    EXPECT_TRUE(pApplication->m_detectedAppOffline);

    pApplication->DereferenceApplication();
}

TEST_F(FileWatcherTests, HandleChangeCompletion_AppOfflinePrefix_IsIgnored)
{
    AppOfflineTrackingApplication* pApplication = new MockAppOfflineTrackingApplication(m_mockHttpApplication);
    FILE_WATCHER watcher;

    DWORD notifySize = 0;
    SetupFileChangeNotification(watcher, pApplication, L"app_offline.htm", L"app_o", notifySize);

    HRESULT hr = watcher.HandleChangeCompletion(notifySize);
    ASSERT_TRUE(SUCCEEDED(hr));

    EXPECT_FALSE(pApplication->m_detectedAppOffline);

    pApplication->DereferenceApplication();
}

TEST_F(FileWatcherTests, HandleChangeCompletion_AppOfflineSuffix_IsIgnored)
{
    AppOfflineTrackingApplication* pApplication = new MockAppOfflineTrackingApplication(m_mockHttpApplication);
    FILE_WATCHER watcher;

    DWORD notifySize = 0;
    SetupFileChangeNotification(watcher, pApplication, L"app_offline.htm", L"app_offline.htmx", notifySize);

    HRESULT hr = watcher.HandleChangeCompletion(notifySize);
    ASSERT_TRUE(SUCCEEDED(hr));

    EXPECT_FALSE(pApplication->m_detectedAppOffline);

    pApplication->DereferenceApplication();
}
