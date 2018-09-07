// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "BaseOutputManager.h"

class LoggingHelpers
{
public:

    static
    HRESULT
    CreateLoggingProvider(
        bool fLoggingEnabled,
        bool fEnableNativeLogging,
        PCWSTR pwzStdOutFileName,
        PCWSTR pwzApplicationPath,
        std::unique_ptr<BaseOutputManager>& outputManager
    );
};

