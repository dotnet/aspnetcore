// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "LoggingHelpers.h"
#include "StandardStreamRedirection.h"
#include "exceptions.h"

std::shared_ptr<RedirectionOutput> LoggingHelpers::CreateOutputs(
    bool enableFileLogging,
    std::wstring outputFileName,
    std::wstring applicationPath,
    std::shared_ptr<RedirectionOutput> stringStreamOutput)
{
    auto stdOutOutput = std::make_shared<StandardOutputRedirectionOutput>();
    std::shared_ptr<RedirectionOutput> fileOutput;
    if (enableFileLogging)
    {
        fileOutput = std::make_shared<FileRedirectionOutput>(applicationPath, outputFileName);
    }

    return std::make_shared<AggregateRedirectionOutput>(std::move(fileOutput), std::move(stdOutOutput), std::move(stringStreamOutput));
}
