// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

typedef INT(*hostfxr_get_native_search_directories_fn) (CONST INT argc, CONST PCWSTR* argv, PWSTR buffer, DWORD buffer_size, DWORD* required_buffer_size);
typedef INT(*hostfxr_main_fn) (CONST DWORD argc, CONST PCWSTR argv[]);

#define READ_BUFFER_SIZE 4096

class HOSTFXR_UTILITY
{
public:
    HOSTFXR_UTILITY();
    ~HOSTFXR_UTILITY();

	static
	HRESULT
	GetHostFxrParameters(
        HANDLE              hEventLog,
        PCWSTR				pcwzProcessPath,
        PCWSTR              pcwzApplicationPhysicalPath,
        PCWSTR              pcwzArguments,
        _Inout_ STRU*       pStruHostFxrDllLocation,
        _Out_ DWORD*        pdwArgCount,
        _Out_ BSTR**       ppwzArgv
	);

    static
    HRESULT
    GetStandaloneHostfxrParameters(
        PCWSTR              pwzExeAbsolutePath, // includes .exe file extension.
        PCWSTR				pcwzApplicationPhysicalPath,
        PCWSTR              pcwzArguments,
        HANDLE              hEventLog,
        _Inout_ STRU*		pStruHostFxrDllLocation,
        _Out_ DWORD*		pdwArgCount,
        _Out_ BSTR**		ppwzArgv
    );

    static
    HRESULT
    ParseHostfxrArguments(
        PCWSTR              pwzArgumentsFromConfig,
        PCWSTR              pwzExePath, 
        PCWSTR				pcwzApplicationPhysicalPath,
        HANDLE              hEventLog,
        _Out_ DWORD*        pdwArgCount,
        _Out_ BSTR**        ppwzArgv
    );

    static
    HRESULT
    GetAbsolutePathToDotnet(
        STRU*   pStruAbsolutePathToDotnet
    );

    static
    HRESULT
    GetAbsolutePathToHostFxr(
        _In_ STRU* pStruAbsolutePathToDotnet,
        _In_ HANDLE hEventLog,
        _Out_ STRU* pStruAbsolutePathToHostfxr
    );

    static
    BOOL
    InvokeWhereToFindDotnet(
        _Inout_ STRU* pStruAbsolutePathToDotnet
    );

    static
    HRESULT
    GetAbsolutePathToDotnetFromProgramFiles(
        _Inout_ STRU* pStruAbsolutePathToDotnet
    );
};

