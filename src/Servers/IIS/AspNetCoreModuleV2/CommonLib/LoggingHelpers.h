// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "NonCopyable.h"
#include "StandardStreamRedirection.h"

class LoggingHelpers
{
public:

    static std::shared_ptr<RedirectionOutput>
    CreateOutputs(
        bool enableFileLogging,
        std::wstring outputFileName,
        std::wstring applicationPath,
        std::shared_ptr<RedirectionOutput> stringStreamOutput
    );
};

