// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"


HRESULT
CheckInstallToSharedConfig(
    IN  MSIHANDLE           hInstall,
        BOOL *              pbShouldInstall
    )
{
    HRESULT         hr = S_OK;
    STRU            strShared;
    STRU            strWriteToShared;
 
    *pbShouldInstall = FALSE;

    //Check if config is shared 
    hr = MsiUtilGetProperty( hInstall, L"IISCONFIGISSHARED", &strShared );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    if( 0 != wcscmp( strShared.QueryStr(), L"1" ) )
    {
        //config is not shared, tell caller to schedule executeCA for config
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"IIS Configuration is NOT shared. Setup will schedule the deferred custom action.");
        *pbShouldInstall = TRUE;
        goto exit;
    }

    //
    //config is shared lets check IIUSESHAREDCONFIG Property
    //
    hr = MsiUtilGetProperty( hInstall, L"IIUSESHAREDCONFIG", &strWriteToShared );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }    
    if( 0 == wcscmp( strWriteToShared.QueryStr(), L"1" ) )
    {
        //Config is shared but property is set
        //tell caller to schedule executeCA for config
        //
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"IIS Configuration IS shared. IIUSESHAREDCONFIG property indicated that setup SHOULD schedule the deferred custom action..");
        *pbShouldInstall = TRUE;
    }
    else
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"IIS Configuration IS shared. IIUSESHAREDCONFIG property indicated that setup should NOT schedule the deferred custom action.");
    }
    
exit:
    return hr;
}        

