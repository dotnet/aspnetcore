// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "ModuleEnvironment.h"
#include <string>
#include <sstream>

// Set in the RegisterModule call IIS uses to initiate the module
extern DWORD g_dwIISServerVersion;

static std::wstring GetIISVersion() {
    int major = (int)(g_dwIISServerVersion >> 16);
    int minor = (int)(g_dwIISServerVersion & 0xffff);

    std::wstringstream version;
    version << major << "." << minor;

    return version.str();
}

static std::wstring ToVirtualPath(const std::wstring& configurationPath) {
    int segments = 0;
    size_t position = configurationPath.find('/');

    // Skip first 4 segments of config path
    while (segments != 3 && position != std::wstring::npos)
    {
        segments++;
        position = configurationPath.find('/', position + 1);
    }

    if (position != std::wstring::npos)
    {
        return configurationPath.substr(position);
    }

    return L"/";
}

void SetApplicationEnvironmentVariables(_In_ IHttpServer &server, _In_ IHttpContext &pHttpContext) {
    SetEnvironmentVariable(L"ASPNETCORE_IIS_VERSION", GetIISVersion().c_str());

    SetEnvironmentVariable(L"ASPNETCORE_IIS_APP_POOL_ID", server.GetAppPoolName());

    IHttpServer2* server2;
    if (SUCCEEDED(HttpGetExtendedInterface(&server, &server, &server2))) {
        SetEnvironmentVariable(L"ASPNETCORE_IIS_APP_POOL_CONFIG_FILE", server2->GetAppPoolConfigFile());
    }

    IHttpSite* site = pHttpContext.GetSite();
    SetEnvironmentVariable(L"ASPNETCORE_IIS_SITE_NAME", site->GetSiteName());
    SetEnvironmentVariable(L"ASPNETCORE_IIS_SITE_ID", std::to_wstring(site->GetSiteId()).c_str());

    IHttpApplication* app = pHttpContext.GetApplication();
    SetEnvironmentVariable(L"ASPNETCORE_IIS_APP_CONFIG_PATH", app->GetAppConfigPath());
    SetEnvironmentVariable(L"ASPNETCORE_IIS_APPLICATION_ID", app->GetApplicationId());
    SetEnvironmentVariable(L"ASPNETCORE_IIS_APPLICATION_VIRTUAL_PATH", ToVirtualPath(app->GetAppConfigPath()).c_str());
}
