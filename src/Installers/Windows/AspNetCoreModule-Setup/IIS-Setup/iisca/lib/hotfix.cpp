// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"
#include "wuerror.h"

#define REBOOT_REQUIRED_REGKEY   L"Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce"
#define REBOOT_REGVALUE          L"IIS Extensions Reboot Required"

HRESULT ExecuteCommandLine(
    IN      PCWSTR      szCommandLine,
    IN      DWORD       dwTimeout,
    __out   LPDWORD     pExitCode

    )
{
    HRESULT hr = NOERROR;
    UINT status = 0;
    PROCESS_INFORMATION oProcInfo = {0};
    STARTUPINFOW oStartInfo = {0};
    DWORD dwExitCode = ERROR_SUCCESS;
    STACK_STRU( strModifiableCommandLine, MAX_PATH);

    oStartInfo.cb = sizeof ( STARTUPINFOW );
    oProcInfo.hThread = INVALID_HANDLE_VALUE;
    oProcInfo.hProcess = INVALID_HANDLE_VALUE;

    _ASSERTE ( szCommandLine );
    _ASSERTE ( pExitCode );

    hr = strModifiableCommandLine.Copy ( szCommandLine );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error copying command line, hr=0x%x", hr);
        goto exit;
    }

    IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Launching process with command line %s", strModifiableCommandLine.QueryStr());

	status = CreateProcessW(NULL,
		strModifiableCommandLine.QueryStr(), // command line
		NULL, // security info
		NULL, // thread info
		TRUE, // inherit handles
		GetPriorityClass(GetCurrentProcess()), // creation flags
		NULL, // environment
		NULL, // cur dir
		&oStartInfo,
		&oProcInfo);

    if ( 0 == status )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error creating process, hr=0x%x", hr);
        goto exit;
    }

    status = WaitForSingleObject( oProcInfo.hProcess, dwTimeout );
    if ( status == WAIT_FAILED ) 
    {
        IISLogWrite(SETUP_LOG_SEVERITY_WARNING, L"Process wait failed, hr=0x%x", HRESULT_FROM_WIN32 ( GetLastError() ) );
        // but we can still continue
    }

    status = GetExitCodeProcess ( oProcInfo.hProcess, &dwExitCode );
    if ( 0 == status )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting exit code for process, hr=0x%x", hr);
        goto exit;
    }

    IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Process returned with exit code %d", dwExitCode);
    *pExitCode = dwExitCode;

exit:
    if ( INVALID_HANDLE_VALUE != oProcInfo.hThread )
    {
        CloseHandle ( oProcInfo.hThread );
        oProcInfo.hThread = INVALID_HANDLE_VALUE;
    }

    if ( INVALID_HANDLE_VALUE != oProcInfo.hProcess )
    {
        CloseHandle ( oProcInfo.hProcess );
        oProcInfo.hProcess = INVALID_HANDLE_VALUE;
    }

    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }

    return hr;
}


HRESULT 
InstallWindowsHotfixQuietly(
    IN      PCWSTR      szHotFixPath,
    __out   BOOL *      pbRebootRequired
)
{
    HRESULT hr = NOERROR;
    UINT status = 0;
    STACK_STRU( strCommandLine, MAX_PATH);
    DWORD cch = 0;
    DWORD exitCode = 0;

    _ASSERTE(szHotFixPath);
    _ASSERTE(pbRebootRequired);

    *pbRebootRequired = FALSE;
    cch = strCommandLine.QuerySizeCCH();

    status = GetSystemDirectoryW(strCommandLine.QueryStr(), cch);
    if ( status > cch)
    {
        cch = status;
        hr = strCommandLine.Resize( ++cch );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error resizing buffer, hr=0x%x", hr);
            goto exit;
        }

        status = GetSystemDirectoryW(strCommandLine.QueryStr(), cch);
    }

    if ( 0 == status )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting system folder path, hr=0x%x", hr);
        goto exit;
    }

    strCommandLine.SyncWithBuffer();

    hr = strCommandLine.Append(L"\\wusa.exe /quiet /norestart \"");
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error apppending wusa, hr=0x%x", hr);
        goto exit;
    }

    hr = strCommandLine.Append(szHotFixPath);
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error apppending hotfixpath, hr=0x%x", hr);
        goto exit;
    }

    hr = strCommandLine.Append(L"\"");
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error apppending end quote, hr=0x%x", hr);
        goto exit;
    }

    hr = ExecuteCommandLine ( strCommandLine.QueryStr(), INFINITE, &exitCode);
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error running the hotfix installer, hr=0x%x", hr);
        goto exit;
    }

    //
    // handle return codes based on WinSE team info regarding WUSA.exe
    //
    switch ( exitCode )
    {
        case ERROR_SUCCESS:
            // install succeeded, continue.
            hr = S_OK;
            break;
        case ERROR_SUCCESS_REBOOT_INITIATED:
        case ERROR_SUCCESS_REBOOT_REQUIRED:
        case WU_S_REBOOT_REQUIRED:
            *pbRebootRequired = TRUE;
            hr = S_OK;
            break;
        case WU_S_ALREADY_INSTALLED:
            // WUSA.exe can return the above codes if this DWORD registry value is set:
            // HKLM\Software\Microsoft\Windows\CurrentVersion\WUSA\ExtendedReturnCode 
            hr = S_OK;
            break;
        case S_FALSE:
            // WUSA.exe returns this when the MSU is already installed, or when its not applicable, continue.
            hr = S_OK;
            break;

        //
        // need cases to handle new WU_S_ALREADY_INSTALLED and WU_E_NOT_APPLICABLE error codes
        //

        default:
            // the installation failed, abort
            hr = HRESULT_FROM_WIN32 ( ERROR_INSTALL_FAILURE );
            goto exit;
    }

exit:
    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }

    return hr;
}
UINT
__stdcall
ExecuteCleanUpWindowsHotfixCA(
    IN      MSIHANDLE   hInstall
)
{
    HRESULT hr = NOERROR;
    UINT status = 0;
    CA_DATA_READER cadata;
    WCHAR * szHotFixName = NULL;    

    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    hr = cadata.LoadDeferredCAData( hInstall );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error retrieving custom action data, hr=0x%x", hr);
        goto exit;
    }

    while ( SUCCEEDED(hr = cadata.Read( &szHotFixName )) )
    {
        status = 0;
        status = DeleteFileW( szHotFixName );
        
        if( FAILED(status) )
        {
            hr = HRESULT_FROM_WIN32( GetLastError() );
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error deleting hotfix temp file '%s', hr=0x%x", szHotFixName, hr);
            //eat this error and try and delete other temp files
            hr = NOERROR;
        }
        else
        {
            IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Success deleting hotfix temp file '%s'", szHotFixName );
        }
    }
    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }
    
exit:
    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }

    IISLogClose();
    //do not fail commit or rollback transaction for this
    return ERROR_SUCCESS;    
}

UINT
__stdcall
ScheduleInstallWindowsHotfixCA(
    IN      MSIHANDLE   hInstall
)
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    OSVERSIONINFOEXW osVersionInfo = {0};

    PMSIHANDLE hDatabase;
    PMSIHANDLE hView;
    PMSIHANDLE hRecord;
    BOOL bDeferredRequired = FALSE;

    CA_DATA_WRITER  cadata;

    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISWindowsHotfix`.`Name`, "
            L"`IISWindowsHotfix`.`OSMajorVersion`, "
            L"`IISWindowsHotfix`.`OSMinorVersion`, "
            L"`IISWindowsHotfix`.`SPMajorVersion`, "
            L"`IISWindowsHotfix`.`Condition`, "
            L"`Binary`.`Data` "
        L"FROM `IISWindowsHotfix`, `Binary` "
        L"WHERE `IISWindowsHotfix`.`BinaryName_`=`Binary`.`Name`";
    
    enum { CA_HOTFIX_NAME = 1, 
           CA_HOTFIX_OSMAJORVERSION,
           CA_HOTFIX_OSMINORVERSION,
           CA_HOTFIX_SPMAJORVERSION,
           CA_HOTFIX_CONDITION,
           CA_HOTFIX_BINARYDATA
    };

    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    osVersionInfo.dwOSVersionInfoSize = sizeof( OSVERSIONINFOEXW );
#pragma warning( push )
#pragma warning( disable : 4996)
    status = GetVersionEx( (LPOSVERSIONINFOW) &osVersionInfo );
#pragma warning( pop ) 

    if ( !status )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting Windows version, hr=0x%x", hr);
        DBGERROR_HR(hr);
        goto exit;
    }

    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting MSI database, hr=0x%x", hr);
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error opening View, hr=0x%x", hr);
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error executing view, hr=0x%x", hr);
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        UINT dwTargetOSMajorVersion = 0;
        UINT dwTargetOSMinorVersion = 0;
        UINT dwTargetSPMajorVersion = 0;
        STACK_STRU( strHotFixName, 128 );
        BOOL isApplicable = TRUE; // Applicable by default

        hr = MsiUtilRecordGetString( hRecord,
                                     CA_HOTFIX_NAME,
                                     &strHotFixName );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", CA_HOTFIX_NAME, hr);
            goto exit;
        }

        IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                      L"Checking applicability of Hotfix '%s'.", 
                      strHotFixName.QueryStr());

        hr = MsiUtilRecordGetInteger( hRecord,
                                      CA_HOTFIX_OSMAJORVERSION,
                                      &dwTargetOSMajorVersion);
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"No OS Major Version available.", CA_HOTFIX_OSMAJORVERSION, hr);
            // This column may be NULL
            hr = S_FALSE;
        }
        
        hr = MsiUtilRecordGetInteger( hRecord,
                                      CA_HOTFIX_OSMINORVERSION,
                                      &dwTargetOSMinorVersion);
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"No OS Minor Version available.");
            // This column may be NULL
            hr = S_FALSE;
        }
        
        hr = MsiUtilRecordGetInteger( hRecord,
                                      CA_HOTFIX_SPMAJORVERSION,
                                      &dwTargetSPMajorVersion);
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"No OS SP Major Version available.");
            // This column may be NULL
            hr = S_FALSE;
        }

        if ( hr == S_FALSE )
        {
            // No OS Version information available
            hr = S_OK;
        }
        else
        {            
            IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                          L"OS Version: Actual '%d.%d'. Hotfix Target '%d.%d'.", 
                          osVersionInfo.dwMajorVersion, 
                          osVersionInfo.dwMinorVersion, 
                          dwTargetOSMajorVersion, 
                          dwTargetOSMinorVersion);

            IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                          L"OS Service Pack Level: Actual '%d'. Hotfix Target '%d'.", 
                          osVersionInfo.wServicePackMajor, 
                          dwTargetSPMajorVersion);

            if ( osVersionInfo.dwMajorVersion != dwTargetOSMajorVersion || 
                 osVersionInfo.dwMinorVersion != dwTargetOSMinorVersion)
            {
                IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"OS Version Mismatch! Will not apply the hotfix.");
                isApplicable = FALSE;
            }
            else if(osVersionInfo.wServicePackMajor != dwTargetSPMajorVersion)
            {
                IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"OS SP Level Mismatch! Will not apply the hotfix.");
                isApplicable = FALSE;
            }
            else
            {
                IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"OS Versions match! Will try to apply the hotfix.");
                isApplicable = TRUE;
            }
        }

        if ( isApplicable )
        {
            STACK_STRU( strCondition, 128 );
            MSICONDITION Condition;

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HOTFIX_CONDITION,
                                         &strCondition );
            if ( FAILED(hr) )
            {
                 IISLogWrite(SETUP_LOG_SEVERITY_ERROR, 
                             L"Error gettting column %d from record, hr=0x%x",
                             CA_HOTFIX_CONDITION,
                             hr);
                 goto exit;
            }

            Condition = MsiEvaluateCondition( hInstall, strCondition.QueryStr() );

            switch( Condition )
            {
            case MSICONDITION_ERROR:
                hr = E_UNEXPECTED;
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, 
                            L"Cannot evaluate hotfix install condition \"%s\", hr=0x%x",
                            strCondition.QueryStr(),
                            hr );
                goto exit;

            case MSICONDITION_FALSE:
                IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Condition evaluation returned false! Will not apply the hotfix.");		
                isApplicable = FALSE;
                break;
            
            case MSICONDITION_TRUE:
                IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Condition evaluation returned true! Will try to apply the hotfix.");		
                isApplicable = TRUE;
                break;
            
            case MSICONDITION_NONE:

                IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"No condition available to evaluate.");		

                break;
            }
        }

        if ( isApplicable )
        {
            //apply the hotfix
            STACK_STRU( hotFixFilePath, MAX_PATH * 2 );

            hr = GenerateTempFileName ( strHotFixName.QueryStr(), 
                                        L"msu", 
                                        &hotFixFilePath);
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error generating temp file name for the hotfix, hr=0x%x", hr);
                goto exit;
            }
            
            hr = MsiUtilRecordReadStreamIntoFile ( hRecord,
                                                   CA_HOTFIX_BINARYDATA, 
                                                   hotFixFilePath.QueryStr());
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error streaming binary data into file, hr=0x%x", hr);
                goto exit;
            }
            else
            {
                IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Streamed hotfix '%s' into file '%s'.", strHotFixName.QueryStr(), hotFixFilePath.QueryStr());
            }

            bDeferredRequired = TRUE;
            hr = cadata.Write( hotFixFilePath.QueryStr(), hotFixFilePath.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error writing custom action data, hr=0x%x", hr);
                goto exit;
            }
        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
    }

    if( bDeferredRequired )
    {
        //schedule CA to install the msu files
        hr = MsiUtilScheduleDeferredAction( hInstall,
                                            L"ExecuteInstallWindowsHotfix",
                                            cadata.QueryData() );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error scheduling custom action ExecuteInstallWindowsHotfix, hr=0x%x", hr);
            goto exit;
        }

        IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                        L"Custom action ExecuteInstallWindowsHotfix scheduled");
                                 

        //schedule CA to delete the msu files if rollback transaction initiated
        hr = MsiUtilScheduleDeferredAction( hInstall,
                                            L"RollbackCleanUpWindowsHotfix",
                                            cadata.QueryData() );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error scheduling custom action RollbackCleanUpWindowsHotfix, hr=0x%x", hr);
            goto exit;
        }

        IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                        L"Custom action RollbackCleanUpWindowsHotfix scheduled");

        //schedule CA to delete the msu files if commit transaction initiated
        hr = MsiUtilScheduleDeferredAction( hInstall,
                                            L"CommitCleanUpWindowsHotfix",
                                            cadata.QueryData() );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error scheduling custom action CommitCleanUpWindowsHotfix, hr=0x%x", hr);
            goto exit;
        }

        IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                        L"Custom action CommitCleanUpWindowsHotfix scheduled");

    }

exit:
    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }

    IISLogClose();

    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
}


UINT
__stdcall
ExecuteInstallWindowsHotfixCA(
    IN      MSIHANDLE   hInstall
)
{
    HRESULT hr = NOERROR;
    CA_DATA_READER cadata;
    WCHAR * szHotFixName = NULL;
    BOOL rebootRequired = FALSE;
    HKEY regKey = NULL;
    LONG status = ERROR_SUCCESS;

    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    hr = cadata.LoadDeferredCAData( hInstall );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error retrieving custom action data, hr=0x%x", hr);
        goto exit;
    }

    while ( SUCCEEDED(hr = cadata.Read( &szHotFixName )) )
    {
        BOOL requiresReboot = FALSE;
        hr = InstallWindowsHotfixQuietly ( szHotFixName , &requiresReboot );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error installing hotfix '%s', hr=0x%x", szHotFixName, hr);
            goto exit;
        }
        if ( requiresReboot )
        {
            rebootRequired = TRUE;
        }
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

    if ( rebootRequired )
    {        
        status = RegOpenKeyEx(HKEY_LOCAL_MACHINE,
            REBOOT_REQUIRED_REGKEY,
            0,
            KEY_SET_VALUE,
            &regKey);
        if(status != ERROR_SUCCESS)
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error opening the reboot registry key, hr=0x%x", hr);
            goto exit;
        }

        status = RegSetValueEx(regKey,
            REBOOT_REGVALUE,
            0,
            REG_SZ,
            (PBYTE)L"",
            4);
        if(status != ERROR_SUCCESS)
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error creating the reboot registry value, hr=0x%x", hr);
            goto exit;
        }

        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Created a registry key to signal reboot is required");
    }

exit:
    if(regKey != NULL)
    {
        RegCloseKey(regKey);
        regKey = NULL;
    }

    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }

    IISLogClose();
    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
}

UINT
__stdcall
ScheduleRebootIfRequiredCA(
    IN      MSIHANDLE   hInstall
)
{
    HRESULT hr = NOERROR;
    HKEY regKey = NULL;
    LONG status = ERROR_SUCCESS;

    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    status = RegOpenKeyEx(HKEY_LOCAL_MACHINE,
        REBOOT_REQUIRED_REGKEY,
        0,
        KEY_QUERY_VALUE,
        &regKey);
    if(status != ERROR_SUCCESS)
    {
        hr = HRESULT_FROM_WIN32(status);
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error opening the reboot registry key, hr=0x%x", hr);
        goto exit;
    }

    status = RegQueryValueEx(regKey,
        REBOOT_REGVALUE,
        0,
        NULL,
        NULL,
        NULL);
    switch(status)
    {
    case ERROR_SUCCESS:
        status = MsiSetMode(hInstall,
            MSIRUNMODE_REBOOTATEND,
            TRUE);
        if( status != ERROR_SUCCESS )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error setting reboot required for the installation, hr=0x%x", hr);
            goto exit;
        }
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Signalled the installer to reboot at the end of the installation.");
        break;

    case ERROR_FILE_NOT_FOUND:
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"No reboot is required by the IIS custom actions.");
        hr = S_OK;
        break;

    default:
        hr = HRESULT_FROM_WIN32(status);
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting the IIS registry key reboot value, hr=0x%x", hr);
        goto exit;
    }

exit:
    if(regKey != NULL)
    {
        RegCloseKey(regKey);
        regKey = NULL;
    }

    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }

    IISLogClose();

    //dont report an error here. the install has completed so its too late to report a failure
    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_SUCCESS;

}