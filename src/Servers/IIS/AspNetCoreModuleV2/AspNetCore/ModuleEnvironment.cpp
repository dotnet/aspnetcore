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

static std::wstring ToVirtualPath(const std::wstring& configurationPath)
{
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

void SetApplicationEnvironmentVariables(_In_ IHttpServer* pServer, _In_ IHttpContext* pHttpContext) {
    SetEnvironmentVariable(L"ANCM_IISVersion", GetIISVersion().c_str());
    SetEnvironmentVariable(L"ANCM_AppPoolName", pServer->GetAppPoolName());

    auto site = pHttpContext->GetSite();
    SetEnvironmentVariable(L"ANCM_SiteName", site->GetSiteName());
    SetEnvironmentVariable(L"ANCM_SiteId", std::to_wstring(site->GetSiteId()).c_str());

    auto app = pHttpContext->GetApplication();
    SetEnvironmentVariable(L"ANCM_AppConfigPath", app->GetAppConfigPath());
    SetEnvironmentVariable(L"ANCM_ApplicationId", app->GetApplicationId());
    SetEnvironmentVariable(L"ANCM_ApplicationPhysicalPath", app->GetApplicationPhysicalPath());
    SetEnvironmentVariable(L"ANCM_ApplicationVirtualPath", ToVirtualPath(app->GetAppConfigPath()).c_str());
}
