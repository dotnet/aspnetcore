#include "precomp.hxx"

ASPNET_CORE_GLOBAL_MODULE::ASPNET_CORE_GLOBAL_MODULE(
    APPLICATION_MANAGER* pApplicationManager)
{
    m_pApplicationManager = pApplicationManager;
}

//
// Is called when IIS decided to terminate worker process
// Shut down all core apps
//
GLOBAL_NOTIFICATION_STATUS
ASPNET_CORE_GLOBAL_MODULE::OnGlobalStopListening(
    _In_ IGlobalStopListeningProvider * pProvider
)
{
    UNREFERENCED_PARAMETER(pProvider);

    if (m_pApplicationManager != NULL)
    {
        // we should let application manager to shudown all allication
        // and dereference it as some requests may still reference to application manager
        m_pApplicationManager->ShutDown();
        m_pApplicationManager = NULL;
    }

    // Return processing to the pipeline.
    return GL_NOTIFICATION_CONTINUE;
}

//
// Is called when configuration changed
// Recycled the corresponding core app if its configuration changed
//
GLOBAL_NOTIFICATION_STATUS
ASPNET_CORE_GLOBAL_MODULE::OnGlobalApplicationStop(
    _In_ IHttpApplicationStopProvider * pProvider
)
{
    // Retrieve the path that has changed.
    IHttpApplication* pApplication = pProvider->GetApplication();

    PCWSTR pwszChangePath = pApplication->GetAppConfigPath();

    // Test for an error.
    if (NULL != pwszChangePath &&
        _wcsicmp(pwszChangePath, L"MACHINE") != 0 &&
        _wcsicmp(pwszChangePath, L"MACHINE/WEBROOT") != 0)
    {
        if (m_pApplicationManager != NULL)
        {
            m_pApplicationManager->RecycleApplication(pwszChangePath);
        }
    }

    // Return processing to the pipeline.
    return GL_NOTIFICATION_CONTINUE;
}