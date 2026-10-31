// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"



#define IIS_CONFIG_BACKUP_EXT L"IISOOBBACK"


typedef BOOL (WINAPI *LPFN_ISWOW64PROCESS) (HANDLE, PBOOL);

LPFN_ISWOW64PROCESS fnIsWow64Process;

BOOL IsWow64()
{
    BOOL bIsWow64 = FALSE;

    //IsWow64Process is not available on all supported versions of Windows.
    //Use GetModuleHandle to get a handle to the DLL that contains the function
    //and GetProcAddress to get a pointer to the function if available.

    fnIsWow64Process = (LPFN_ISWOW64PROCESS) GetProcAddress(
        GetModuleHandle(TEXT("kernel32")),"IsWow64Process");

    if(NULL != fnIsWow64Process)
    {
        if (!fnIsWow64Process(GetCurrentProcess(),&bIsWow64))
        {
            //handle error
        }
    }
    return bIsWow64;
}

/********************************************************************
IISScheduleInstall CA - CUSTOM ACTION ENTRY POINT for reading IIS custom
table settings into CA Data

********************************************************************/
UINT
WINAPI
IISScheduleInstallCA(
    IN  MSIHANDLE   hInstall
    )
{
    HRESULT hr = S_OK;
    CA_DATA_WRITER cadata;
    BOOL bWriteToShared = FALSE;
    BOOL fCoInit = FALSE;
    
    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    if ( SUCCEEDED( CoInitializeEx( NULL, COINIT_MULTITHREADED ) ) )
    {
        fCoInit = TRUE;
    }
    
    //
    //See if we are going to update shared config
    // 
    hr = CheckInstallToSharedConfig( hInstall, &bWriteToShared ); 
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }  
    if ( !bWriteToShared )
    {
        //do not update config for this module
        //ExecuteCA will not be scheduled.
    } 
    //
    // Schedule transactions
    //
    hr = MsiUtilScheduleDeferredAction( hInstall,
                                        L"IISBeginTransactionCA",
                                        cadata.QueryData() );
    if ( FAILED(hr) )
    {
        goto exit;
    }   
    hr = MsiUtilScheduleDeferredAction( hInstall,
                                        L"IISRollbackTransactionCA",
                                        cadata.QueryData() );
    if ( FAILED(hr) )
    {
        goto exit;
    }      
    hr = MsiUtilScheduleDeferredAction( hInstall,
                                        L"IISCommitTransactionCA",
                                        cadata.QueryData() );
    if ( FAILED(hr) )
    {
        goto exit;
    } 
    if( bWriteToShared )
    {
        //
        // Do the Config Install actions
        //    
        hr = ScheduleInstallModuleCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }    
        hr = ScheduleRegisterUIModuleCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
        hr = ScheduleInstallHandlerCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
        hr = ScheduleRegisterSectionSchemaCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
        hr = ScheduleRegisterTraceAreaCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
        hr = ScheduleInstallSectionDefaultsCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
        hr = ScheduleInstallSectionAdditionsCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
        hr = ScheduleInstallCgiRestrictionsCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
    }

    //
    // Do the Non-Config Install actions
    //    
    hr = ScheduleRegisterMofFileCA( hInstall, &cadata );
    if ( FAILED(hr) )
    {
        goto exit;
    }      

     
    //
    // Schedule deferred execute CA
    //

    hr = MsiUtilScheduleDeferredAction( hInstall,
                                        L"IISExecuteCA",
                                        cadata.QueryData() );
    if ( FAILED(hr) )
    {
        goto exit;
    }

    hr = NOERROR;


exit:
    if ( fCoInit )
    {
        CoUninitialize();
    }
    
    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    IISLogClose();

    return hr;
}
/********************************************************************
IISScheduleUninstall CA - CUSTOM ACTION ENTRY POINT for reading IIS custom
table settings into CA Data

********************************************************************/
UINT
WINAPI
IISScheduleUninstallCA(
    IN  MSIHANDLE   hInstall
    )
{
    HRESULT hr = S_OK;
    CA_DATA_WRITER cadata;
    BOOL bWriteToShared = FALSE;
    BOOL fCoInit = FALSE;
    
    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    if ( SUCCEEDED( CoInitializeEx( NULL, COINIT_MULTITHREADED ) ) )
    {
        fCoInit = TRUE;
    }
    
    //
    //See if we are going to update shared config
    // 
    hr = CheckInstallToSharedConfig( hInstall, &bWriteToShared ); 
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }  
    if ( !bWriteToShared )
    {
        //do not update config for this module
        //ExecuteCA will not be scheduled.
    } 

    if( bWriteToShared )
    {    
        //
        // Do the Config Uninstall actions
        //
        hr = ScheduleUnInstallModuleCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }    
        hr = ScheduleUnRegisterUIModuleCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
        hr = ScheduleUnInstallHandlerCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }  
        hr = ScheduleUnRegisterSectionSchemaCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
        hr = ScheduleUnInstallCgiRestrictionsCA( hInstall, &cadata );
        if ( FAILED(hr) )
        {
            goto exit;
        }
    }
    //
    // Do the Non-Config Uninstall actions
    //    
      
          
    //
    // Schedule deferred execute CA
    //

    hr = MsiUtilScheduleDeferredAction( hInstall,
                                        L"IISExecuteCA",
                                        cadata.QueryData() );
    if ( FAILED(hr) )
    {
        goto exit;
    }

    hr = NOERROR;


exit:
    if ( fCoInit )
    {
        CoUninitialize();
    }
    
    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    IISLogClose();

    //
    // Don't fail while uninstalling.
    //
    return NOERROR;
}
/********************************************************************
IISExecuteCA - CUSTOM ACTION ENTRY POINT for writing IIS custom
table settings to iis config

********************************************************************/
UINT
WINAPI
IISExecuteCA(
    IN  MSIHANDLE   hInstall
    )
{
    HRESULT hr = NOERROR;
    CA_DATA_READER cadata;
    INT icaType = 0; 
    BOOL fCoInit = FALSE; 
     
    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    if ( SUCCEEDED( CoInitializeEx( NULL, COINIT_MULTITHREADED ) ) )
    {
        fCoInit = TRUE;
    }        
    //
    // Retrieve parameters from ca data
    //

    hr = cadata.LoadDeferredCAData( hInstall );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    while ( SUCCEEDED(hr = cadata.Read( &icaType )) )
    {
        switch (icaType )
        {
            case IIS_INSTALL_MODULE:
            {
                hr = ExecuteInstallModuleCA(  &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_UNINSTALL_MODULE:
            {
                hr = ExecuteUnInstallModuleCA(  &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_INSTALL_UIMODULE:
            {
                hr = ExecuteRegisterUIModuleCA(  &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_UNINSTALL_UIMODULE:
            {
                hr = ExecuteUnRegisterUIModuleCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_INSTALL_HANDLER:
            {
                hr = ExecuteInstallHandlerCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_UNINSTALL_HANDLER:
            {
                hr = ExecuteUnInstallHandlerCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_INSTALL_SECTIONSCHEMA:
            {
                hr = ExecuteRegisterSectionSchemaCA( &cadata );
                 if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_UNINSTALL_SECTIONSCHEMA:
            {
                hr = ExecuteUnRegisterSectionSchemaCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_INSTALL_TRACEAREA:
            {
                hr = ExecuteRegisterTraceAreaCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_INSTALL_MOFFILE:
            {
                hr = ExecuteRegisterMofFileCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_INSTALL_DEFAULTS:
            {
                hr = ExecuteInstallSectionDefaultsCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_INSTALL_SECTION_ADDITIONS:
            {
                hr = ExecuteInstallSectionAdditionsCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_INSTALL_CGIRESTRICTIONS:
            {
                hr = ExecuteInstallCgiRestrictionsCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }
            case IIS_UNINSTALL_CGIRESTRICTIONS:
            {
                hr = ExecuteInstallCgiRestrictionsCA( &cadata );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                break;
            }            
            default:
            {
                //unknown execute CA action type
                hr = E_UNEXPECTED;
                goto exit;
            }
        }
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:

    if ( fCoInit )
    {
        CoUninitialize();
    }
    
    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    IISLogClose();
    
    return hr;
}

/********************************************************************
 BeginTransaction - CUSTOM ACTION ENTRY POINT for backing up
 config

  Input:  deferred CustomActionData - BackupName
********************************************************************/
UINT
WINAPI
IISBeginTransactionCA(
    IN  MSIHANDLE   
    )
    
{
    HRESULT hr = S_OK;
    STACK_STRU( wzConfigSource, MAX_PATH );
    STACK_STRU( wzConfigCopy, MAX_PATH );
    DWORD dwSize = 0;

    if( IsWow64() )
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\Sysnative\\inetsrv\\config\\applicationHost.config",
                                      wzConfigSource.QueryStr(),
                                      MAX_PATH
                                      );
    }
    else
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\applicationHost.config",
                                      wzConfigSource.QueryStr(),
                                      MAX_PATH
                                      );
    }
    if ( dwSize == 0 )
    {
        //ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
        goto exit;
    }
    wzConfigSource.SyncWithBuffer();
    hr = wzConfigCopy.Copy(  wzConfigSource );

    //add ca action as extension

    hr = wzConfigCopy.Append( L".");

    hr = wzConfigCopy.Append( IIS_CONFIG_BACKUP_EXT);

    if ( !::CopyFileW(wzConfigSource.QueryStr(), wzConfigCopy.QueryStr(), FALSE) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        //ExitOnFailure2(hr, "Failed to copy config backup %S -> %S", wzConfigSource, wzConfigCopy);
    }

    if( IsWow64() )
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\Sysnative\\inetsrv\\config\\administration.config",
                                      wzConfigSource.QueryStr(),
                                      MAX_PATH
                                      );
    }
    else
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\administration.config",
                                      wzConfigSource.QueryStr(),
                                      MAX_PATH
                                      );
    }
    
    if ( dwSize == 0 )
    {
        //ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
        goto exit;
    }
    wzConfigSource.SyncWithBuffer();
    hr = wzConfigCopy.Copy(  wzConfigSource );

    //add ca action as extension

    hr = wzConfigCopy.Append( L".");

    hr = wzConfigCopy.Append( IIS_CONFIG_BACKUP_EXT);

    if ( !::CopyFileW(wzConfigSource.QueryStr(), wzConfigCopy.QueryStr(), FALSE) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        //ExitOnFailure2(hr, "Failed to copy config backup %S -> %S", wzConfigSource, wzConfigCopy);
    }


exit:
    
    return S_OK;

}


/********************************************************************
 RollbackTransaction - CUSTOM ACTION ENTRY POINT for unbacking up
 config

  Input:  deferred CustomActionData - BackupName
********************************************************************/
UINT
WINAPI
IISRollbackTransactionCA(
    IN  MSIHANDLE   
    )
{

    HRESULT hr = S_OK;
    STACK_STRU( wzConfigSource, MAX_PATH );
    STACK_STRU( wzConfigCopy, MAX_PATH );
    DWORD dwSize = 0;


    if( IsWow64() )
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\Sysnative\\inetsrv\\config\\applicationHost.config",
                                      wzConfigSource.QueryStr(),
                                      MAX_PATH 
                                      );
    }
    else
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\applicationHost.config",
                                      wzConfigSource.QueryStr(),
                                      MAX_PATH 
                                      );
    }                             
    if ( dwSize == 0 )
    {
        //ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
        goto exit;
    }
    wzConfigSource.SyncWithBuffer();
    hr = wzConfigCopy.Copy( wzConfigSource );

    //add ca action as extension

    hr = wzConfigCopy.Append( L"." );

    hr = wzConfigCopy.Append( IIS_CONFIG_BACKUP_EXT );

    //rollback copy is reverse of start transaction
    if( !::CopyFileW( wzConfigCopy.QueryStr(), wzConfigSource.QueryStr(), FALSE) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        //ExitOnFailure(hr, "failed to restore config backup");
    }

    if ( !::DeleteFileW( wzConfigCopy.QueryStr() ) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        //ExitOnFailure(hr, "failed to delete config backup");
    }

    if( IsWow64() )
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\Sysnative\\inetsrv\\config\\administration.config",
                                      wzConfigSource.QueryStr(),
                                      MAX_PATH
                                      );
    }
    else
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\administration.config",
                                      wzConfigSource.QueryStr(),
                                      MAX_PATH
                                      );
    }
    if ( dwSize == 0 )
    {
        //ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
        goto exit;
    }
    wzConfigSource.SyncWithBuffer();
    hr = wzConfigCopy.Copy( wzConfigSource );

    //add ca action as extension

    hr = wzConfigCopy.Append( L"." );

    hr = wzConfigCopy.Append( IIS_CONFIG_BACKUP_EXT );

    //rollback copy is reverse of start transaction
    if( !::CopyFileW( wzConfigCopy.QueryStr(), wzConfigSource.QueryStr(), FALSE) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        //ExitOnFailure(hr, "failed to restore config backup");
    }

    if ( !::DeleteFileW( wzConfigCopy.QueryStr() ) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        //ExitOnFailure(hr, "failed to delete config backup");
    }


exit:
    
    return S_OK;

}
/********************************************************************
 CommitTransaction - CUSTOM ACTION ENTRY POINT for unbacking up
 config

  Input:  deferred CustomActionData - BackupName
********************************************************************/
UINT
WINAPI
IISCommitTransactionCA(
    IN  MSIHANDLE   
    )
{
    HRESULT hr = S_OK;
    STACK_STRU( wzConfigCopy, MAX_PATH );
    DWORD dwSize = 0;



    // Config AdminMgr changes already committed, just
    // delete backup config file.

    if( IsWow64() )
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\Sysnative\\inetsrv\\config\\applicationHost.config",
                                      wzConfigCopy.QueryStr(),
                                      MAX_PATH
                                      );
    }
    else
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\applicationHost.config",
                                      wzConfigCopy.QueryStr(),
                                      MAX_PATH
                                      );
    }
    if ( dwSize == 0 )
    {
        //ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
        goto exit;
    }
    wzConfigCopy.SyncWithBuffer();

    hr = wzConfigCopy.Append( L"." );

    hr = wzConfigCopy.Append( IIS_CONFIG_BACKUP_EXT);

    if( !::DeleteFileW(wzConfigCopy.QueryStr() ) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        //ExitOnFailure(hr, "failed to delete config backup");
    }

    if( IsWow64() )
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\Sysnative\\inetsrv\\config\\administration.config",
                                      wzConfigCopy.QueryStr(),
                                      MAX_PATH
                                      );
    }
    else
    {
        dwSize = ExpandEnvironmentStringsW(L"%windir%\\system32\\inetsrv\\config\\administration.config",
                                      wzConfigCopy.QueryStr(),
                                      MAX_PATH
                                      );
    }
    if ( dwSize == 0 )
    {
        //ExitWithLastError(hr, "failed to get ExpandEnvironmentStrings");
        goto exit;
    }
    wzConfigCopy.SyncWithBuffer();

    hr = wzConfigCopy.Append( L"." );

    hr = wzConfigCopy.Append( IIS_CONFIG_BACKUP_EXT);

    if( !::DeleteFileW(wzConfigCopy.QueryStr() ) )
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        //ExitOnFailure(hr, "failed to delete config backup");
    }


exit:
    
    return S_OK;

}
