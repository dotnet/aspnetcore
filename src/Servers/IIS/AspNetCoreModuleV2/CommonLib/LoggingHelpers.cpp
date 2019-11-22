// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "LoggingHelpers.h"
#include "FileOutputManager.h"
#include "PipeOutputManager.h"
#include "NullOutputManager.h"
#include "debugutil.h"
#include <Windows.h>
#include <io.h>
#include "ntassert.h"
#include "exceptions.h"
#include "EventLog.h"
#include "BaseOutputManager.h"

HRESULT
LoggingHelpers::CreateLoggingProvider(
    bool fIsLoggingEnabled,
    bool fEnableNativeLogging,
    PCWSTR pwzStdOutFileName,
    PCWSTR pwzApplicationPath,
    std::unique_ptr<BaseOutputManager>& outputManager
)
{
    HRESULT hr = S_OK;

    DBG_ASSERT(outputManager != NULL);

    try
    {
        // Check if there is an existing active console window before redirecting
        // Window == IISExpress with active console window, don't redirect to a pipe
        // if true.
        CONSOLE_SCREEN_BUFFER_INFO dummy;

        if (fIsLoggingEnabled)
        {
            auto manager = std::make_unique<FileOutputManager>(pwzStdOutFileName, pwzApplicationPath, fEnableNativeLogging);
            outputManager = std::move(manager);
        }
        else if (!GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE), &dummy))
        {
            outputManager = std::make_unique<PipeOutputManager>(fEnableNativeLogging);
        }
        else
        {
            outputManager = std::make_unique<NullOutputManager>();
        }
    }
    catch (std::bad_alloc&)
    {
        hr = E_OUTOFMEMORY;
    }

    return hr;
}
