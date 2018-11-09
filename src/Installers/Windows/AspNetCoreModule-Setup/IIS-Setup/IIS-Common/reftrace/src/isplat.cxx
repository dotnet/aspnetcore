// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.hxx"
#include "pudebug.h"

#define IMPLEMENTATION_EXPORT

extern "C"
PLATFORM_TYPE
IISGetPlatformType(
    VOID
)
/*++

  This function consults the registry and determines the platform type
   for this machine.

  Arguments:

    None

  Returns:
    Platform type

--*/
{
    OSVERSIONINFOEX osVersionInfoEx  = { 0 };
    DWORDLONG       dwlConditionMask = 0;
    BOOL            fReturn          = FALSE;
    
    osVersionInfoEx.dwOSVersionInfoSize = sizeof( osVersionInfoEx );
  
    //
    // If we are not workstation (Client) 
    // that means that we are a server or domain controller (Server)
    //
    osVersionInfoEx.wProductType = VER_NT_WORKSTATION;
    VER_SET_CONDITION( dwlConditionMask, VER_PRODUCT_TYPE, VER_EQUAL );

    fReturn = VerifyVersionInfo(
        &osVersionInfoEx, 
        VER_PRODUCT_TYPE,
        dwlConditionMask );

    //
    // VerifyVersionInfo fails if the return value is zero 
    // and GetLastError returns an error code other than ERROR_OLD_WIN_VERSION
    //
    if ( !fReturn && GetLastError() != ERROR_OLD_WIN_VERSION )
    {
        DPERROR(( DBG_CONTEXT,
                  HRESULT_FROM_WIN32 ( GetLastError() ),
                  "VerifyVersionInfo failed\n" ));

        return PtInvalid;
    }

    return ( fReturn ) ? PtNtWorkstation : PtNtServer;
}



