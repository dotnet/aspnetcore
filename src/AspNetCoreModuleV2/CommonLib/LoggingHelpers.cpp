// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "LoggingHelpers.h"
#include "IOutputManager.h"
#include "FileOutputManager.h"
#include "PipeOutputManager.h"
#include "NullOutputManager.h"
#include "debugutil.h"
#include <Windows.h>
#include <io.h>
#include "ntassert.h"

HRESULT
LoggingHelpers::CreateLoggingProvider(
    bool fIsLoggingEnabled,
    bool fEnableNativeLogging,
    PCWSTR pwzStdOutFileName,
    PCWSTR pwzApplicationPath,
    std::unique_ptr<IOutputManager>& outputManager
)
{
    HRESULT hr = S_OK;

    DBG_ASSERT(outputManager != NULL);

    try
    {
        if (fIsLoggingEnabled)
        {
            auto manager = std::make_unique<FileOutputManager>(fEnableNativeLogging);
            hr = manager->Initialize(pwzStdOutFileName, pwzApplicationPath);
            outputManager = std::move(manager);
        }
        else if (!GetConsoleWindow())
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
