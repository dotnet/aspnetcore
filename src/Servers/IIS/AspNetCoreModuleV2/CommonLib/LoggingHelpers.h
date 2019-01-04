// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "NonCopyable.h"
#include "PipeOutputManager.h"

class LoggingHelpers
{
public:

    static
    PipeOutputManager
    StartStdOutRedirection(
        RedirectionOutput& output
    );

    static std::shared_ptr<RedirectionOutput>
    CreateOutputs(
        bool enableLogging,
        std::wstring outputFileName,
        std::wstring applicationPath,
        std::shared_ptr<RedirectionOutput> stringStreamOutput
    );
};

