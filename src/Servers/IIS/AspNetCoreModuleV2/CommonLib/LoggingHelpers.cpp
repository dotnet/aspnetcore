// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "LoggingHelpers.h"
#include "PipeOutputManager.h"
#include "exceptions.h"
#include "debugutil.h"

PipeOutputManager
LoggingHelpers::StartStdOutRedirection(
    RedirectionOutput& output
)
{
    LOG_INFOF(L"Redirecting stdout/stderr to a pipe.");
    return PipeOutputManager(output);
}

std::shared_ptr<RedirectionOutput> LoggingHelpers::CreateOutputs(
    bool enableLogging,
    std::wstring outputFileName,
    std::wstring applicationPath,
    std::shared_ptr<RedirectionOutput> stringStreamOutput)
{
    auto stdOutOutput = std::make_shared<StandardOutputRedirectionOutput>();
    std::shared_ptr<RedirectionOutput> fileOutput;
    if (enableLogging)
    {
        fileOutput = std::make_shared<FileRedirectionOutput>(applicationPath, outputFileName);
    }

    return std::make_shared<AggregateRedirectionOutput>(std::move(fileOutput), std::move(stdOutOutput), std::move(stringStreamOutput));
}
