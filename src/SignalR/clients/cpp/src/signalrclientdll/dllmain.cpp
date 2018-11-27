// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "../signalrclient/stdafx.h"

#if !defined(__cplusplus_winrt)

BOOL APIENTRY DllMain( HMODULE /*hModule*/,
                       DWORD  ul_reason_for_call,
                       LPVOID /*lpReserved*/
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

#endif
