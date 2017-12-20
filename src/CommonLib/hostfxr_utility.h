// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class HOSTFXR_UTILITY
{
public:
    HOSTFXR_UTILITY();
    ~HOSTFXR_UTILITY();

    static
    HRESULT
    FindHostFxrDll(
        ASPNETCORE_CONFIG *pConfig,
        STRU* struHostFxrDllLocation,
        BOOL* fStandAlone
    );

    static
    HRESULT
    GetStandaloneHostfxrLocation(
        STRU* struHostfxrPath,
        ASPNETCORE_CONFIG *pConfig
    );

    static
    HRESULT
    GetPortableHostfxrLocation(
        STRU* struHostfxrPath
    );
};

