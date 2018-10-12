// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once


enum IIS_HTTP_LISTENER_CA_TYPE
{
    IIS_HTTP_LISTENER_CA_INSTALL,
    IIS_HTTP_LISTENER_CA_UNINSTALL
};

HRESULT GetSidStringForAccount(
    const WCHAR * szAccount,
    __inout STRU * pstr
    );

UINT 
WINAPI 
ScheduleHttpListenerCA(
    IN MSIHANDLE hInstall,
    IN const WCHAR * pszCAName,
    IIS_HTTP_LISTENER_CA_TYPE caType
    );

UINT
__stdcall
ExecuteHttpListenerCA(
    IN MSIHANDLE hInstall,
    IIS_HTTP_LISTENER_CA_TYPE caType
    );

UINT 
WINAPI 
ScheduleInstallHttpListenerCA(
    IN MSIHANDLE hInstall
    );

UINT
__stdcall
ExecuteInstallHttpListenerCA(
    IN      MSIHANDLE   hInstall
    );

UINT 
WINAPI 
ScheduleUnInstallHttpListenerCA(
    IN MSIHANDLE hInstall
    );

UINT
__stdcall
ExecuteUnInstallHttpListenerCA(
    IN      MSIHANDLE   hInstall
    );