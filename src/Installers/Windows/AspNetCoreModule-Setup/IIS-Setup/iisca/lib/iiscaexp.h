// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

UINT
WINAPI
IISScheduleInstallCA(
    IN  MSIHANDLE   hInstall
    );

UINT
WINAPI
IISScheduleUninstallCA(
    IN  MSIHANDLE   hInstall
    );

UINT
WINAPI
IISExecuteCA(
    IN  MSIHANDLE   hInstall
    );
    
UINT
WINAPI
IISBeginTransactionCA(
    IN  MSIHANDLE   hInstall
    );

UINT
WINAPI
IISRollbackTransactionCA(
    IN  MSIHANDLE   hInstall
    );

UINT
WINAPI
IISCommitTransactionCA(
    IN  MSIHANDLE   hInstall
    );

