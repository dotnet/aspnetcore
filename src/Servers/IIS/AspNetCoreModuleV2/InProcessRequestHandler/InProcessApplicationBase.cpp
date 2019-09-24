// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "InProcessApplicationBase.h"

InProcessApplicationBase::InProcessApplicationBase(
    IHttpServer& pHttpServer,
    IHttpApplication& pHttpApplication)
    : AppOfflineTrackingApplication(pHttpApplication),
      m_fRecycleCalled(FALSE),
      m_pHttpServer(pHttpServer)
{
}

VOID
InProcessApplicationBase::StopInternal(bool fServerInitiated)
{
    AppOfflineTrackingApplication::StopInternal(fServerInitiated);

    // Stop was initiated by server no need to do anything, server would stop on it's own
    if (fServerInitiated)
    {
        return;
    }

    if (!m_pHttpServer.IsCommandLineLaunch())
    {
        // IIS scenario.
        // We don't actually handle any shutdown logic here.
        // Instead, we notify IIS that the process needs to be recycled, which will call
        // ApplicationManager->Shutdown(). This will call shutdown on the application.
        m_pHttpServer.RecycleProcess(L"AspNetCore InProcess Recycle Process on Demand");
    }
    else
    {
        // Send WM_QUIT to the main window to initiate graceful shutdown
        EnumWindows([](HWND hwnd, LPARAM) -> BOOL
        {
            DWORD processId;

            if (GetWindowThreadProcessId(hwnd, &processId) &&
                processId == GetCurrentProcessId() &&
                GetConsoleWindow() != hwnd)
            {
                PostMessage(hwnd, WM_QUIT, 0, 0);
                return false;
            }

            return true;
        }, 0);
    }
}

