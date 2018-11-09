// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

UINT 
WINAPI 
ScheduleMakeShortcutElevatedCA(
                               IN MSIHANDLE hInstall
                               )
{
    HRESULT hr = S_OK;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strShortcut, 128 );
    STACK_STRU( strDirectoryId, 256 ); // Directory Identifier
    STACK_STRU( strDirectoryName, 256 );
    STACK_STRU( strComponent, 128 );
    STACK_STRU( strShortcutName, 256 );
    STACK_STRU( strData,  256);


    CONST WCHAR * szQuery =
        L"SELECT "
        L"`IISElevatedShortcut`.`Shortcut_`, "
        L"`Shortcut`.`Component_`, "
        L"`Shortcut`.`Name`, "
        L"`Directory`.`Directory` "
        L"FROM `IISElevatedShortcut`, `Shortcut`, `Directory`  "
        L"WHERE `IISElevatedShortcut`.`Shortcut_`=`Shortcut`.`Shortcut` "
        L"AND `Shortcut`.`Directory_`=`Directory`.`Directory`";


    CA_DATA_WRITER cadata;
    INSTALLSTATE installStateCurrent;
    INSTALLSTATE installStateAction;

    BOOL scheduleDefferedCA = FALSE;

    enum { 
        CA_ELEVATESC_SHORTCUT = 1,
        CA_ELEVATESC_COMPONENT,
        CA_ELEVATESC_SHORTCUTNAME,
        CA_ELEVATESC_DIRECTORY,
    };

    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

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
        hr = MsiUtilRecordGetString( hRecord,
                                    CA_ELEVATESC_COMPONENT,
                                    &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", CA_ELEVATESC_COMPONENT, hr);
            goto exit;
        }

        status = MsiGetComponentStateW( hInstall,
                            strComponent.QueryStr(),
                            &installStateCurrent,
                            &installStateAction );

        if ( ERROR_SUCCESS != status )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting state for component %s, hr=0x%x", strComponent.QueryStr(), hr);
            goto exit;
        }

        // Only run if the comonent is installing or reinstalling
        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {
            // GET: directory where shortcut is
            // Get the directory id for the shortcut
            hr = MsiUtilRecordGetString( hRecord,
                CA_ELEVATESC_DIRECTORY,
                &strDirectoryId );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", CA_ELEVATESC_DIRECTORY, hr);
                goto exit;
            }

            // Get directory path
            hr = MsiUtilGetProperty(hInstall, strDirectoryId.QueryStr(), &strDirectoryName ); 
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting value for directory record %s, hr=0x%x", strDirectoryId.QueryStr(), hr);
                goto exit;
            }

            IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                L"Shortcut Directory: '%s'.", 
                      strDirectoryName.QueryStr());
            // ENDGET: directory where shortcut is


            // GET: Short and Long names of shortcuts
            hr = MsiUtilRecordGetString( hRecord,
                CA_ELEVATESC_SHORTCUTNAME,
                &strShortcutName );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", CA_ELEVATESC_SHORTCUTNAME, hr);
                goto exit;
            }

            // strShortcutName contains the short and long file names of the shortcut
            
            PCWSTR pszPrevious = strShortcutName.QueryStr();
            // Append the shortcut name to the directory and write to cadata
            // Write the short and long shortcut paths
            for ( DWORD i = 0; i < strShortcutName.QueryCCH() + 1; i++ )
            {
                if ( strShortcutName.QueryStr()[ i ] == L'|' ||
                     strShortcutName.QueryStr()[ i ] == L'\0' )
                {
                    strShortcutName.QueryStr()[ i ] = L'\0';

                    strData.Copy( strDirectoryName );
                    strData.Append(L"\\");
                    strData.Append(pszPrevious);
                    strData.Append(L".lnk");

                    IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                        L"Potential shortcut path: %s", 
                        strData.QueryStr());

                    hr = cadata.Write( strData.QueryStr(), strData.QueryCCH() );
                    if ( FAILED(hr) )
                    {
                        DBGERROR_HR(hr);
                        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error writing custom action data, hr=0x%x", hr);
                        goto exit;
                    }

                    pszPrevious = strShortcutName.QueryStr() + i + 1;
                    
                    // Only schedule custom action if needed
                    scheduleDefferedCA = TRUE;
                }
            }

            // ENDGET: Short and Long names of shortcuts
        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }

    }

    if ( scheduleDefferedCA )
    {
        hr = MsiUtilScheduleDeferredAction( hInstall,
                                            L"ExecuteMakeShortcutElevated",
                                            cadata.QueryData() );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error scheduling custom action, hr=0x%x", hr);
            goto exit;
        }

        IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                        L"Custom action ExecuteMakeShortcutElevated scheduled");
    }

exit:

    if ( hRecord )
    {
        MsiCloseHandle( hRecord );
        hRecord = NULL;
    }

    if ( hView )
    {
        MsiCloseHandle( hView );
        hView = NULL;
    }

    if ( hDatabase )
    {
        MsiCloseHandle( hDatabase );
        hDatabase = NULL;
    }

    IISLogClose();

    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_SUCCESS;
}

UINT 
WINAPI 
ExecuteMakeShortcutElevatedCA(
                              IN MSIHANDLE hInstall
                              )
{
    HRESULT hr = S_OK;

    BOOL    fCoInit = FALSE;

    WCHAR * szShortcutFile = NULL;

    CA_DATA_READER cadata;

    IISLogInitialize(hInstall, UNITEXT(__FUNCTION__));

    //
    // Retrieve parameters from ca data
    //

    hr = cadata.LoadDeferredCAData( hInstall );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error retrieving custom action data, hr=0x%x", hr);
        goto exit;
    }

    if ( SUCCEEDED( hr = CoInitialize(NULL) ) )
    {
        fCoInit = TRUE;
    }

    while ( SUCCEEDED(hr = cadata.Read( &szShortcutFile )) )
    {
        if(GetFileAttributes(szShortcutFile) != 0xFFFFFFFF)
        {
            IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                L"Shortcut %s exists", szShortcutFile);

            // Get shell
            CComPtr<IPersistFile> sppf;

            if (FAILED(hr = sppf.CoCreateInstance(L"lnkfile")) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error get ShellLink com object, hr=0x%x", hr);
                goto exit;
            }

            // Load shortcut file
            if (FAILED(hr = sppf->Load(szShortcutFile, STGM_READWRITE)))
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error loading shortcut file %s, hr=0x%x", szShortcutFile, hr);
                goto exit;
            }

            CComQIPtr<IShellLinkDataList> spdl(sppf);
            if (!spdl)
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            DWORD dwFlags;
            if (FAILED(hr = spdl->GetFlags(&dwFlags))) 
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting shortcut flags, hr=0x%x", hr);
                goto exit;
            }

            // Add "run as ..." flag
            dwFlags |= SLDF_RUNAS_USER;
            if (FAILED(hr = spdl->SetFlags(dwFlags)))
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error setting SLDF_RUNAS_USER flag, hr=0x%x", hr);
                goto exit;
            }

            // Save file.
            if (FAILED(hr = sppf->Save(NULL, TRUE)))
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error saving changes to shortcut, hr=0x%x", hr);
                goto exit;
            }

            IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
                L"Successfully added SLDF_RUNAS_USER flag to shortcut %s", 
                szShortcutFile);

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

    IISLogClose();
    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_SUCCESS;
}