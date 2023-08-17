#include "ModuleEnvironment.h"
#include <string>
#include <sstream>

extern DWORD dwIISServerVersion;

static std::wstring GetIISVersion() {
    auto major = (int)(dwIISServerVersion >> 16);
    auto minor = (int)(dwIISServerVersion & 0xffff);

    std::wstringstream version;
    version << major << "." << minor;

    return version.str();
}

static std::wstring ToVirtualPath(const std::wstring& configurationPath) {
    auto segments = 0;
    auto position = configurationPath.find('/');
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
    SetEnvironmentVariable(L"ASPNETCORE_IIS_APP_POOL_CONFIG_FILE", ((IHttpServer2&)server).GetAppPoolConfigFile());

    auto site = pHttpContext.GetSite();
    SetEnvironmentVariable(L"ASPNETCORE_IIS_SITE_NAME", site->GetSiteName());
    SetEnvironmentVariable(L"ASPNETCORE_IIS_SITE_ID", std::to_wstring(site->GetSiteId()).c_str());

    auto app = pHttpContext.GetApplication();
    SetEnvironmentVariable(L"ASPNETCORE_IIS_APP_CONFIG_PATH", app->GetAppConfigPath());
    SetEnvironmentVariable(L"ASPNETCORE_IIS_APPLICATION_ID", app->GetApplicationId());
    SetEnvironmentVariable(L"ASPNETCORE_IIS_APPLICATION_VIRTUAL_PATH", ToVirtualPath(app->GetAppConfigPath()).c_str());
}
