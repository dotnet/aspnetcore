// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

HRESULT
LoggingHelpers::CreateLoggingProvider(
    bool fIsLoggingEnabled,
    bool fEnablePipe,
    PCWSTR pwzStdOutFileName,
    PCWSTR pwzApplicationPath,
    _Out_ IOutputManager** outputManager
)
{
    HRESULT hr = S_OK;

    DBG_ASSERT(outputManager != NULL);

    if (fIsLoggingEnabled)
    {
        FileOutputManager* manager = new FileOutputManager;
        hr = manager->Initialize(pwzStdOutFileName, pwzApplicationPath);
        *outputManager = manager;
    }
    else if (fEnablePipe)
    {
        *outputManager = new PipeOutputManager;
    }
    else
    {
        *outputManager = new NullOutputManager;
    }

    return hr;
}
