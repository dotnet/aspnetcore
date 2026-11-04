// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _SETUP_LOG_H_
#define _SETUP_LOG_H_


//
// Severity levels for setup log information.  This is arranged in the
// order of increasing severity.
//
enum SETUP_LOG_SEVERITY
{
    SETUP_LOG_SEVERITY_DEBUG,
    SETUP_LOG_SEVERITY_INFORMATION,
    SETUP_LOG_SEVERITY_WARNING,
    SETUP_LOG_SEVERITY_ERROR
};

//consider using an IIS prefix for Msi* methods - they conflict with MSI apis

//
// Initialize logging once at beginning of CA
//

VOID
IISLogInitialize(
    IN MSIHANDLE    hInstall,
    IN LPCWSTR      pszCAName
    );

//
// Close logging at end / exit of CA
//

VOID
IISLogClose(
    VOID
    );

//
// Writes a message to msi log file
//
VOID
IISLogWrite(
    IN SETUP_LOG_SEVERITY   setupLogSeverity,
    IN LPCWSTR              pszLogMessageFormat,
    ...
    );

#endif


