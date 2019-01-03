// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "LoggingHelpers.h"
#include "FileOutputManager.h"
#include "PipeOutputManager.h"
#include "NullOutputManager.h"
#include <Windows.h>
#include "exceptions.h"
#include "BaseOutputManager.h"

LoggingHelpers::Redirection
LoggingHelpers::StartRedirection(
    RedirectionOutput& output,
    HostFxr& hostFxr,
    const IHttpServer& server,
    bool enableLogging,
    std::wstring outputFileName,
    std::wstring applicationPath
)
{
    // Check if there is an existing active console window before redirecting
    // Window == IISExpress with active console window, don't redirect to a pipe
    // if true.
    CONSOLE_SCREEN_BUFFER_INFO dummy;

    auto enableNativeLogging = !server.IsCommandLineLaunch() & !hostFxr.SupportsOutputRedirection();
    auto hostFxrRedirection = hostFxr.RedirectOutput(output);
    std::unique_ptr<BaseOutputManager> outputManager;

    if (enableLogging)
    {
        outputManager = std::make_unique<FileOutputManager>(output, outputFileName, applicationPath, enableNativeLogging);
    }
    else if (!GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE), &dummy))
    {
        outputManager = std::make_unique<PipeOutputManager>(output, enableNativeLogging);
    }
    else
    {
        outputManager = std::make_unique<NullOutputManager>(output);
    }

    return Redirection(std::move(hostFxrRedirection), std::move(outputManager));
}
