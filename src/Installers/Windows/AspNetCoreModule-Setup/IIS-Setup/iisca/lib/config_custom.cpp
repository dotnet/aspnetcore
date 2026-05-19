// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

UINT 
__stdcall 
CheckForSharedConfigurationCA(
    MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;
    UINT status = ERROR_SUCCESS;
    BOOL fIsSharedConfig = FALSE;
    STRU            strWriteToShared;
       
    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    hr = GetSharedConfigEnabled( &fIsSharedConfig );
    if ( FAILED( hr ) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Unable to detect whether shared configuration is in use.");
        status = LogMsiCustomActionError( hInstall, 30001 );
        goto exit;
    }
    if ( fIsSharedConfig )
    {
        //set config shared property.
        //will be used by other CAs via CheckInstallToSharedConfig
        hr = MsiSetProperty( hInstall, L"IISCONFIGISSHARED", L"1" );       
        if ( FAILED( hr ) )
        {
            goto exit;
        }
        //
        //config is shared lets check if user
        //set public property IIUSESHAREDCONFIG
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
        }
        else
        {
            //
            //public property not set error-out install
            //
            IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Shared Configuration detected.");
            status = LogMsiCustomActionError( hInstall, 30002 );
            goto exit;
        }
    }
    else
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"No Shared Configuration detected.");
    }

exit:
    
    IISLogClose();
    return status;
}

