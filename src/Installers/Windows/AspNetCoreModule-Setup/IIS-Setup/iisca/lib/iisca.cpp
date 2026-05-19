// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

#define ROOT_CONFIG_PATH   L"MACHINE/WEBROOT/APPHOST"

#define MAX_NAME 256
#define _UNITEXT(quote) L##quote
#define UNITEXT(quote) _UNITEXT(quote)

HRESULT
GetFullTypeFromAssemblyTable(
    IN      MSIHANDLE       hDatabase,
    IN      CONST WCHAR *   szComponent,
    IN      CONST WCHAR *   szTypeName,
    IN OUT  STRU *          pstrFullType
    );

BOOL IsSectionInAdminConfig(
    IN CONST WCHAR * szIsInAdminConfig
    )
{
    if( 0 == _wcsicmp(szIsInAdminConfig, L"yes") )
    {
        return TRUE;
    }
    else
    {
        return FALSE;
    }
}

HRESULT
ScheduleInstallModuleCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{
    //
    //  If the module being installed includes the optional TypeName,
    //  then the we'll test the component to insure that the module
    //  is a .Net module.  If so, then we will not install the module
    //  in <globalModules> and we'll include the TypeName and strong
    //  name info when we install in the <modules>.
    //

    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );
    STACK_STRU( strTemp, 128 );
    STACK_STRU( strComponent, 128 );
    STACK_STRU( strTypeName, 128 );
    STACK_STRU( strFullType, 128 );

    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISGlobalModule`.`Name`, "
            L"`IISGlobalModule`.`File_`, "
            L"`IISGlobalModule`.`PreCondition`, "
            L"`File`.`Component_`, "
            L"`IISGlobalModule`.`TypeName` "
        L"FROM `IISGlobalModule`, `File` "
        L"WHERE `File`.`File`=`IISGlobalModule`.`File_`";

    enum { CA_MODULE_NAME = 1, 
           CA_MODULE_IMAGE, 
           CA_MODULE_PRECONDITION, 
           CA_MODULE_COMPONENT,
           CA_MODULE_TYPENAME };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;
      
    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    // does table exists
    //
    UINT er = ERROR_SUCCESS;
        er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISGlobalModule");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }       
    
    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_MODULE_COMPONENT,
                                     &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
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
            goto exit;
        }

        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {

            cadata->Write( IIS_INSTALL_MODULE ); 
            
            // Get the module name
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_MODULE_NAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            //
            // CA_MODULE_IMAGE is the name of the File element, need to
            // resolve it to the full path by formatting it as [#ModuleDll]
            //

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_MODULE_IMAGE,
                                         &strTemp );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = strData.SafeSnwprintf( L"[#%s]", strTemp.QueryStr() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilFormatString( hInstall,
                                      &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_MODULE_PRECONDITION,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            //
            // Get the type name (optional)
            // If the type name is present, then this is a .Net and does not have
            // to be registered in the <globalModules>
            //
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_MODULE_TYPENAME,
                                         &strTypeName );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            if ( strTypeName.QueryCCH() > 0 )
            {
                // TypeName is present.  Get the assembly info
                hr = GetFullTypeFromAssemblyTable( hDatabase,
                                                   strComponent.QueryStr(),
                                                   strTypeName.QueryStr(),
                                                   &strFullType );

                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
            }
            else
            {
                strFullType.Reset();
            }

            hr = cadata->Write( strFullType.QueryStr(), strFullType.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }         

        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}


HRESULT
ScheduleUnInstallModuleCA(
    IN  MSIHANDLE   hInstall,
    IN CA_DATA_WRITER * cadata   
    )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );

    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISGlobalModule`.`Name`, "
            L"`File`.`Component_`, "
            L"`IISGlobalModule`.`TypeName` "
        L"FROM `IISGlobalModule`, `File` "
        L"WHERE `File`.`File`=`IISGlobalModule`.`File_`";

    enum { CA_MODULE_NAME = 1, 
           CA_MODULE_COMPONENT,
           CA_MODULE_TYPENAME };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;



    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
        er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISGlobalModule");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {        
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_MODULE_COMPONENT,
                                     &strData );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        status = MsiGetComponentStateW( hInstall,
                                        strData.QueryStr(),
                                        &installStateCurrent,
                                        &installStateAction );
        if ( ERROR_SUCCESS != status )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            goto exit;
        }

        if ( MsiUtilIsUnInstalling( installStateCurrent, installStateAction ) )
        {

            cadata->Write( IIS_UNINSTALL_MODULE );


            hr = MsiUtilRecordGetString( hRecord,
                                         CA_MODULE_NAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_MODULE_TYPENAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}


HRESULT
ExecuteInstallModuleCA(
    IN CA_DATA_READER  * cadata   
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szName = NULL;
    WCHAR * szImage = NULL;
    WCHAR * szPreCondition = NULL;
    WCHAR * szType = NULL;


    hr = cadata->Read( &szName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    hr = cadata->Read( &szImage );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szPreCondition );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szType );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Install the module
    //

    hr = InstallModule( szName, 
                        szImage, 
                        szPreCondition,
                        szType );

    if ( hr == HRESULT_FROM_WIN32( ERROR_ALREADY_EXISTS ) )
    {
        //
        // We'll quietly accept a module already exists.
        // This will happen if a component has multiple features
        // that each have a module.
        // If a feature is omitted on the initial install,
        // and added later using Change, the features that
        // were initially installed will show up in the
        // ScheduleInstallModuleCA with install 
        // INSTALLSTATE_UNKNOWN, which will trigger a reinstall.
        // Reinstall will result in ERROR_ALREADY_EXISTS.
        //
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION,
                    L"Module: '%s' already installed.",
                    szName);

        hr = S_OK;
    }

    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }


    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );
        
    return hr;
}


HRESULT
ExecuteUnInstallModuleCA(
    IN  CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szName = NULL;
    WCHAR * szType = NULL;



   hr = cadata->Read( &szName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }   
    hr = cadata->Read( &szType );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = UnInstallModule( szName, szType );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}


HRESULT
ScheduleRegisterSectionSchemaCA(
    IN  MSIHANDLE   hInstall,
    IN CA_DATA_WRITER * cadata
    )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );

    CONST WCHAR * szQuery =
        L"SELECT "
                L"`IISConfigSections`.`Name`, "
                L"`IISConfigSections`.`File_`, "
                L"`IISConfigSections`.`OverrideModeDefault`, "
                L"`IISConfigSections`.`AllowDefinition`, "
                L"`IISConfigSections`.`Type`, "
				L"`IISConfigSections`.`InAdminConfig`, "
                L"`File`.`Component_` "
        L"FROM `IISConfigSections`, `File` "
        L"WHERE `File`.`File`=`IISConfigSections`.`File_`";

    enum
    {
        CA_SECTION_NAME = 1,
        CA_SCHEMA_FILE,
        CA_SECTION_OVERRIDEMODE,
        CA_SECTION_ALLOWDEF,
        CA_SECTION_TYPE,
		CA_SECTION_INADMINCONFIG,
        CA_SCHEMA_COMPONENT
    };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;

    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exists
    //
    UINT er = ERROR_SUCCESS;
    er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISConfigSections");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }   
    
    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_SCHEMA_COMPONENT,
                                     &strData );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        status = MsiGetComponentStateW( hInstall,
                                        strData.QueryStr(),
                                        &installStateCurrent,
                                        &installStateAction );
        if ( ERROR_SUCCESS != status )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            goto exit;
        }

        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {

            cadata->Write( IIS_INSTALL_SECTIONSCHEMA ); 

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_SECTION_NAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_SECTION_OVERRIDEMODE,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_SECTION_ALLOWDEF,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_SECTION_TYPE,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

			hr = MsiUtilRecordGetString( hRecord,
                                         CA_SECTION_INADMINCONFIG,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }    
        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }

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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
}


HRESULT
ScheduleUnRegisterSectionSchemaCA(
    IN  MSIHANDLE   hInstall,
    IN CA_DATA_WRITER * cadata    
    )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );

    CONST WCHAR * szQuery =
        L"SELECT "
			L"`IISConfigSections`.`Name`, "
			L"`IISConfigSections`.`InAdminConfig`, "
			L"`File`.`Component_` "
        L"FROM `IISConfigSections`, `File` "
        L"WHERE `File`.`File`=`IISConfigSections`.`File_`";

    enum { 
		CA_SECTION_NAME = 1,
		CA_SECTION_INADMINCONFIG,
		CA_SCHEMA_COMPONENT };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;


    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exists
    //
    UINT er = ERROR_SUCCESS;
    er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISConfigSections");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }    
    
    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_SCHEMA_COMPONENT,
                                     &strData );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        status = MsiGetComponentStateW( hInstall,
                                        strData.QueryStr(),
                                        &installStateCurrent,
                                        &installStateAction );
        if ( ERROR_SUCCESS != status )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            goto exit;
        }

        if ( MsiUtilIsUnInstalling( installStateCurrent, installStateAction ) )
        {
            cadata->Write( IIS_UNINSTALL_SECTIONSCHEMA );

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_SECTION_NAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

			hr = MsiUtilRecordGetString( hRecord,
                                         CA_SECTION_INADMINCONFIG,
				                         &strData );

			if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}


HRESULT
ExecuteRegisterSectionSchemaCA(
    IN CA_DATA_READER * cadata    
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szSectionName = NULL;
    WCHAR * szOverrideMode = NULL;
    WCHAR * szAllowDefinition = NULL;
    WCHAR * szType = NULL;
	WCHAR * szIsInAdminConfig = NULL;

    
    //
    // Retrieve parameters from ca data
    //
    hr = cadata->Read( &szSectionName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }   
    hr = cadata->Read( &szOverrideMode );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szAllowDefinition );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szType );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

	hr = cadata->Read( &szIsInAdminConfig );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Register the section
    //

    hr = RegisterSectionSchema( IsSectionInAdminConfig(szIsInAdminConfig),
								szSectionName,
                                szOverrideMode,
                                szAllowDefinition,
                                szType );

    if ( hr == HRESULT_FROM_WIN32( ERROR_ALREADY_EXISTS ) )
    {
        //
        // We'll quietly accept a section name already exists.
        // This will happen if a package has multiple features
        // that each have a section.
        // If a feature is omitted on the initial install,
        // and added later using Change, the features that
        // were initially installed will show up in the
        // ScheduleRegisterSectionSchemaCA with install 
        // INSTALLSTATE_UNKNOWN, which will trigger a reinstall.
        // Reinstall will result in ERROR_ALREADY_EXISTS.
        //
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION,
                    L"Section name: '%s' already exists.",
                    szSectionName);

        hr = S_OK;
    }

    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:


    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
}


HRESULT
ExecuteUnRegisterSectionSchemaCA(
    IN CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szSectionName = NULL;
    WCHAR * szIsInAdminConfig = NULL;


    hr = cadata->Read( &szSectionName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }    
	hr = cadata->Read( &szIsInAdminConfig );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

	hr = UnRegisterSectionSchema( IsSectionInAdminConfig(szIsInAdminConfig),
								  szSectionName );
    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR,
                L"Failed to unregister section schema for section: '%s', hr=0x%x .",
                szSectionName,
                hr );
        DBGERROR_HR(hr);

        //
        // We need to keep going because this is an uninstall action
        // and should be resilient to missing elements.
        //
        hr = S_OK;
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:


    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
}

HRESULT
CanonicalizeAssemblyVersion(
    STRU & strValue
)
{
    //
    // Converts 7.1.2.000 to 7.1.2.0
    //

    HRESULT hr = S_OK;
    LPCWSTR psz = wcschr( strValue.QueryStr(), L'.' );
    DWORD dwDotCount = 0;

    //
    // Find the 3rd '.'
    //
    while ( psz != NULL )
    {
        dwDotCount++;
        LPCWSTR psz2 = wcschr( psz + 1, L'.' );
        if ( psz2 == NULL )
        {
            break;
        }
        psz = psz2;
    }

    if ( dwDotCount == 3 && psz != NULL )
    {
        //
        // Convert "000" to integer and then back to string.
        //

        psz ++;
        DWORD dw = _wtoi( psz );
        STACK_STRU( strTemp, 16 );

        hr = strTemp.SafeSnwprintf( L"%u", dw );
        if ( SUCCEEDED( hr ) )
        {
            ptrdiff_t diff = psz - strValue.QueryStr();

            _ASSERTE( diff >= 0  );

            hr = strValue.SetLen( (DWORD) diff );
            if ( FAILED( hr ) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            hr = strValue.Append(strTemp);
            if ( FAILED( hr ) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
        }
    }

exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}

HRESULT
GetFullTypeFromAssemblyTable(
    IN      MSIHANDLE       hDatabase,
    IN      CONST WCHAR *   szComponent,
    IN      CONST WCHAR *   szTypeName,
    IN OUT  STRU *          pstrFullType
    )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;
    BOOL fRecordFound = FALSE;
    BOOL fVersion = FALSE;

    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strQuery, 128 );
    STACK_STRU( strPropName, 64 );
    STACK_STRU( strPropValueName, 64 );
    STACK_STRU( strPropVersion, 64 );
    STACK_STRU( strPropCulture, 64 );
    STACK_STRU( strPropKeyToken, 64 );

    CONST WCHAR * szQueryTemplate =
        L"SELECT `Name`, `Value` "
        L"FROM `MsiAssemblyName`  "
        L"WHERE `Component_`='%s'";

    CONST WCHAR * szFullTypeTempl = L"%s, %s, Version=%s, Culture=%s, PublicKeyToken=%s";
        // szTypeName, name, version, culture, publicKeyToken

    enum { CA_ASSEMBLY_PROP_NAME = 1, CA_ASSEMBLY_PROP_VALUE };

    hr = strQuery.SafeSnwprintf( szQueryTemplate, szComponent );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiDatabaseOpenViewW( hDatabase, strQuery.QueryStr(), &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    STRU * pstr;

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        fRecordFound = TRUE;
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_ASSEMBLY_PROP_NAME,
                                     &strPropName );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        pstr = NULL;

        if ( 0 == wcscmp( strPropName.QueryStr(), L"name" ) )
        {
            pstr = &strPropValueName;
        }
        else if (  0 == wcscmp( strPropName.QueryStr(), L"version" ) )
        {
            fVersion = TRUE;
            pstr = &strPropVersion;
        }
        else if (  0 == wcscmp( strPropName.QueryStr(), L"culture" ) )
        {
            pstr = &strPropCulture;
        }
        else if (  0 == wcscmp( strPropName.QueryStr(), L"publicKeyToken" ) )
        {
            pstr = &strPropKeyToken;
        }

        if ( pstr )
        {
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_ASSEMBLY_PROP_VALUE,
                                         pstr );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            if ( fVersion )
            {
                hr = CanonicalizeAssemblyVersion( *pstr );
                if ( FAILED( hr ) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
                fVersion = FALSE;
            }
        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
    }

    hr = pstrFullType->SafeSnwprintf( szFullTypeTempl,
                                      szTypeName,
                                      strPropValueName.QueryStr(),
                                      strPropVersion.QueryStr(),
                                      strPropCulture.QueryStr(),
                                      strPropKeyToken.QueryStr()
                                      );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:
    _ASSERTE( fRecordFound );

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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}


HRESULT
ScheduleRegisterUIModuleCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strComponent, 128 );
    STACK_STRU( strAssemblyInfoComponent, 128 );
    STACK_STRU( strData, 128 );
    STACK_STRU( strFullType, 128 );

    const STRU * pstrComponentName = NULL;

    CONST WCHAR * szQuery =
        L"SELECT "
                L"`IISUIModule`.`Name`, "
                L"`IISUIModule`.`TypeName`, "
                L"`IISUIModule`.`Component_` ,"
                L"`IISUIModule`.`AssemblyInfoComponent_` ,"
                L"`IISUIModule`.`RegisterInModulesSection` ,"
                L"`IISUIModule`.`PrependToList` "
        L"FROM `IISUIModule`  ";

    enum { 
		CA_UIMODULE_NAME = 1, 
		CA_UIMODULE_TYPE, 
		CA_UIMODULE_COMPONENT, 
		CA_UIMODULE_ASSEMBLYINFOCOMPONENT, 
		CA_UIMODULE_REGISTER,
        CA_UIMODULE_PREPEND
	};

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;


    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
    er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISUIModule");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }
    
    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_UIMODULE_COMPONENT,
                                     &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
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
            goto exit;
        }

        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {

            cadata->Write( IIS_INSTALL_UIMODULE ); 

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_UIMODULE_NAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_UIMODULE_ASSEMBLYINFOCOMPONENT,
                                         &strAssemblyInfoComponent );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            // use the AssemblyInfoComponent_ to get the module assembly information
            // fall back to using the component if the value is null.
            if ( strAssemblyInfoComponent.QueryCCH() > 0 )
            {
                pstrComponentName = &strAssemblyInfoComponent;
            }
            else
            {
                pstrComponentName = &strComponent;
            }
            _ASSERTE( pstrComponentName != NULL );

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_UIMODULE_TYPE,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = GetFullTypeFromAssemblyTable( hDatabase,
                                               pstrComponentName->QueryStr(),
                                               strData.QueryStr(),
                                               &strFullType );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strFullType.QueryStr(), strFullType.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord, 
                                         CA_UIMODULE_REGISTER,
                                         &strData );
            if (FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord, 
                                         CA_UIMODULE_PREPEND,
                                         &strData );
            if (FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }

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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
}


HRESULT
ScheduleUnRegisterUIModuleCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strComponent, 128 );
    STACK_STRU( strAssemblyInfoComponent, 128 );
    STACK_STRU( strData, 128 );
    STACK_STRU( strFullType, 128 );

    const STRU * pstrComponentName = NULL;

    CONST WCHAR * szQuery =
        L"SELECT "
                L"`IISUIModule`.`Name`, "
                L"`IISUIModule`.`TypeName`, "
                L"`IISUIModule`.`Component_` ,"
                L"`IISUIModule`.`AssemblyInfoComponent_` "
        L"FROM `IISUIModule`  ";

    enum { 
		CA_UIMODULE_NAME = 1, 
		CA_UIMODULE_TYPE, 
		CA_UIMODULE_COMPONENT, 
		CA_UIMODULE_ASSEMBLYINFOCOMPONENT, 
	};

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;


    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
    er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISUIModule");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_UIMODULE_COMPONENT,
                                     &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
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
            goto exit;
        }

        if ( MsiUtilIsUnInstalling( installStateCurrent, installStateAction ) )
        {

            cadata->Write( IIS_UNINSTALL_UIMODULE );
    
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_UIMODULE_NAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_UIMODULE_ASSEMBLYINFOCOMPONENT,
                                         &strAssemblyInfoComponent );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            // use the AssemblyInfoComponent_ to get the module assembly information
            // fall back to using the component if the value is null.
            if ( strAssemblyInfoComponent.QueryCCH() > 0 )
            {
                pstrComponentName = &strAssemblyInfoComponent;
            }
            else
            {
                pstrComponentName = &strComponent;
            }
            _ASSERTE( pstrComponentName != NULL );

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_UIMODULE_TYPE,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = GetFullTypeFromAssemblyTable( hDatabase,
                                               pstrComponentName->QueryStr(),
                                               strData.QueryStr(),
                                               &strFullType );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strFullType.QueryStr(), strFullType.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }

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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
}


HRESULT
ExecuteRegisterUIModuleCA(
    IN  CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szUIModuleName = NULL;
    WCHAR * szUIModuleTypeInfo = NULL;
    WCHAR * szUIModuleRegister = NULL;
    WCHAR * szUIModulePrepend = NULL;

    hr = cadata->Read( &szUIModuleName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    hr = cadata->Read( &szUIModuleTypeInfo );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szUIModuleRegister );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szUIModulePrepend );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Register the section
    //

    hr = RegisterUIModule( szUIModuleName,
                           szUIModuleTypeInfo,
                           szUIModuleRegister,
                           szUIModulePrepend );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:


    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}


HRESULT
ExecuteUnRegisterUIModuleCA(
    IN  CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szUIModuleName = NULL;
    WCHAR * szUIModuleTypeInfo = NULL;

    hr = cadata->Read( &szUIModuleName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    hr = cadata->Read( &szUIModuleTypeInfo );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = UnRegisterUIModule( szUIModuleName, szUIModuleTypeInfo );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}

HRESULT
ScheduleRegisterTraceAreaCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );
    STACK_STRU( strComponent, 128 );

    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISTraceArea`.`Component_`, "
            L"`IISTraceArea`.`ProviderName`, "
            L"`IISTraceArea`.`ProviderGuid`, "
            L"`IISTraceArea`.`AreaName`, "
            L"`IISTraceArea`.`AreaValue` "
        L"FROM `IISTraceArea` ";

    enum { CA_COMPONENT = 1,
           CA_PROVIDER_NAME, 
           CA_PROVIDER_GUID, 
           CA_AREA_NAME,
           CA_AREA_VALUE };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;

    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
    er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISTraceArea");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_COMPONENT,
                                     &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
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
            goto exit;
        }

        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {
            cadata->Write( IIS_INSTALL_TRACEAREA );
            
            // Get the values
            for ( DWORD Index = CA_PROVIDER_NAME; Index <= CA_AREA_VALUE; Index++ )
            {
                hr = MsiUtilRecordGetString( hRecord,
                                             Index,
                                             &strData );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }

                hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
                if ( FAILED(hr) )
                {
                    DBGERROR_HR(hr);
                    goto exit;
                }
            }

        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}


HRESULT
ExecuteRegisterTraceAreaCA(
    IN  CA_DATA_READER * cadata
)
{
    HRESULT hr = NOERROR;

    WCHAR * szTraceProviderName = NULL;
    WCHAR * szTraceProviderGuid = NULL;
    WCHAR * szAreaName = NULL;
    WCHAR * szAreaValue = NULL;


    hr = cadata->Read( &szTraceProviderName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }    
    hr = cadata->Read( &szTraceProviderGuid );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szAreaName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szAreaValue );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Register the section
    //
    hr = RegisterTraceArea( szTraceProviderName,
                            szTraceProviderGuid,
                            szAreaName,
                            szAreaValue );
    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR,
                    L"Failed to register trace area (Provider: '%s'; "
                    L"Guid: '%s'; AreaName: '%s'; AreaValue: '%s') hr=0x%x",
                    szTraceProviderName,
                    szTraceProviderGuid,
                    szAreaName,
                    szAreaValue,
                    hr );
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_SUCCESS;
}

/// The error messages are from setstrings.wxl.  
/// The integer parameter is used to to get the string. 
UINT
LogMsiCustomActionError(
    IN MSIHANDLE hInstall,
    UINT messageId
    ) 
{
   	PMSIHANDLE pLogger = MsiCreateRecord(1);
    if ( pLogger == NULL )
    {
        return ERROR_INSTALL_FAILURE;
    }

    MsiRecordSetInteger( pLogger, 1, messageId ); 
    MsiProcessMessage( hInstall, INSTALLMESSAGE_ERROR, pLogger );

    if ( pLogger != NULL )
    {
        MsiCloseHandle( pLogger );
        pLogger = NULL;
    }

    return ERROR_INSTALL_FAILURE;
}


HRESULT
ScheduleRegisterMofFileCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
)
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );

    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISTraceArea`.`BinaryName_`, "
            L"`Binary`.`Data`, "
			L"`IISTraceArea`.`Component_` "
        L"FROM `IISTraceArea`, `Binary` "
        L"WHERE `Binary`.`Name`=`IISTraceArea`.`BinaryName_`";

    enum { CA_BINARY_NAME = 1,
           CA_FILE_DATA,
           CA_MOF_COMPONENT };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;


    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
    er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISTraceArea");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }
   
    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_MOF_COMPONENT,
                                     &strData );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        status = MsiGetComponentStateW( hInstall,
                                        strData.QueryStr(),
                                        &installStateCurrent,
                                        &installStateAction );
        if ( ERROR_SUCCESS != status )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            goto exit;
        }

        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {

            cadata->Write( IIS_INSTALL_MOFFILE );

            STACK_STRU( strBinaryName, 128 );
            STACK_STRU( strMofFilePath, MAX_PATH );

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_BINARY_NAME,
                                         &strBinaryName );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = GenerateTempFileName( strBinaryName.QueryStr(),
                                       L"mof", 
                                       &strMofFilePath);
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = MsiUtilRecordReadStreamIntoFile( hRecord,
                                                  CA_FILE_DATA, 
                                                  strMofFilePath.QueryStr());
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strMofFilePath.QueryStr(), strMofFilePath.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            if ( hRecord )
            {
                MsiCloseHandle( hRecord );
                hRecord = NULL;
            }
        }
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}

HRESULT
ExecuteRegisterMofFileCA(
    IN  CA_DATA_READER * cadata
)
{
    HRESULT hr = NOERROR;

    WCHAR * szMofFileName = NULL;

    hr = cadata->Read( &szMofFileName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }  
    //
    // Register the section
    //
    hr = RegisterMofFile( szMofFileName );
    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR,
                    L"Failed to register MOF file (File name: '%s') hr=0x%x",
                    szMofFileName,
                    hr );
        DBGERROR_HR(hr);

        //
        // Continue setup, this is not a fatal error.
        //
        hr = S_OK;

        goto exit;
    }
    else
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION,
                    L"MOF file '%s' registered.",
                    szMofFileName );
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return (SUCCEEDED(hr)) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
}

HRESULT
ScheduleInstallHandlerCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{

    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );
    STACK_STRU( strComponent, 128 );


    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISGlobalHandler`.`Name`, "
            L"`IISGlobalHandler`.`Component_`, "
            L"`IISGlobalHandler`.`Path`, "
            L"`IISGlobalHandler`.`Verb`, "
            L"`IISGlobalHandler`.`Type`, "
            L"`IISGlobalHandler`.`Index`, "
            L"`IISGlobalHandler`.`Modules`, "
            L"`IISGlobalHandler`.`ScriptProcessor`, "
            L"`IISGlobalHandler`.`ResourceType`, "
            L"`IISGlobalHandler`.`RequiredAccess`, "
            L"`IISGlobalHandler`.`PreCondition` "
        L"FROM `IISGlobalHandler` ";

    enum { CA_HANDLER_NAME = 1, 
           CA_HANDLER_COMPONENT,
           CA_HANDLER_PATH,
           CA_HANDLER_VERB,
           CA_HANDLER_TYPE,
           CA_HANDLER_INDEX,
           CA_HANDLER_MODULES,
           CA_HANDLER_SCRIPTPROCESSOR,
           CA_HANDLER_RESOURCETYPE,
           CA_HANDLER_REQUIREDACCESS,
           CA_HANDLER_PRECONDITION
         };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;

    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
    er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISGlobalHandler");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_HANDLER_COMPONENT,
                                     &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
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
            goto exit;
        }

        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {
 
            cadata->Write( IIS_INSTALL_HANDLER );
 
            // Get the Handler name
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_NAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            //
            // Get handler Path
            //

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_PATH,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            //
            //Get handler Verb
            //
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_VERB,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            //
            // Get handler Type 
            //
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_TYPE,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            //
            // Get handler Index 
            //
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_INDEX,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }            
            //
            // Get handler Modules 
            //
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_MODULES,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
             //
            // Get handler Script Processor 
            //
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_SCRIPTPROCESSOR,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }           
             //
            // Get handler ResourceType 
            //
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_RESOURCETYPE,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }           
            //
            // Get handler RequiredAccess 
            //
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_REQUIREDACCESS,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }                        
            //
            // Get handler PreCondition 
            //
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_PRECONDITION,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }            

        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );
        
    return hr;
}


HRESULT
ScheduleUnInstallHandlerCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );

    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISGlobalHandler`.`Name`, "
            L"`IISGlobalHandler`.`Component_` "
        L"FROM `IISGlobalHandler` ";

    enum { CA_HANDLER_NAME = 1, 
           CA_HANDLER_COMPONENT
         };


    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;

    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
    er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISGlobalHandler");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_HANDLER_COMPONENT,
                                     &strData );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        status = MsiGetComponentStateW( hInstall,
                                        strData.QueryStr(),
                                        &installStateCurrent,
                                        &installStateAction );
        if ( ERROR_SUCCESS != status )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            goto exit;
        }

        if ( MsiUtilIsUnInstalling( installStateCurrent, installStateAction ) )
        {

            cadata->Write( IIS_UNINSTALL_HANDLER );

            hr = MsiUtilRecordGetString( hRecord,
                                         CA_HANDLER_NAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
}


HRESULT
ExecuteInstallHandlerCA(
    IN  CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szName = NULL;
    WCHAR * szPath = NULL;
    WCHAR * szVerb = NULL;
    WCHAR * szType = NULL;
    WCHAR * szIndex = NULL;
    WCHAR * szModules = NULL;
    WCHAR * szScriptProcessor = NULL;
    WCHAR * szResourceType = NULL;   
    WCHAR * szRequiredAccess = NULL;
    WCHAR * szPreCondition = NULL;

    ULONG ulIndex = HANDLER_INDEX_FIRST;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;

    
    hr = CoCreateInstance(__uuidof(AppHostWritableAdminManager),
                          NULL,
                          CLSCTX_INPROC_SERVER,
                          __uuidof(IAppHostWritableAdminManager),
                          (VOID **)&pAdminMgr);
    if (FAILED(hr))
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, 
                    L"CoCreateInstance failed 0x%08x", hr);
        DBGERROR_HR(hr);
        goto exit;
    } 
    

    hr = cadata->Read( &szName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }    
    hr = cadata->Read( &szPath );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szVerb );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szType );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szIndex );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    if( 0 == _wcsicmp( szIndex, L"FIRST" ) )
    {
        ulIndex = HANDLER_INDEX_FIRST;
    }
    else if ( 0 == _wcsicmp( szIndex, L"LAST" ) ) 
    {
        ulIndex = HANDLER_INDEX_FIRST;
    }
    else if ( 0 == _wcsicmp( szIndex, L"BEFORE_STATICFILE" ) )
    {
        ulIndex = HANDLER_INDEX_BEFORE_STATICFILE;        
    }

    hr = cadata->Read( &szModules );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szScriptProcessor );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szResourceType );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = cadata->Read( &szRequiredAccess );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    
    hr = cadata->Read( &szPreCondition );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }                     
    
    //
    // Install the Handler
    //
    hr = RegisterHandler( pAdminMgr,
                          ROOT_CONFIG_PATH,
                          ulIndex,
                          szName,
                          szPath,
                          szVerb,
                          szType,
                          szModules,
                          szScriptProcessor,
                          szResourceType,
                          szRequiredAccess,
                          szPreCondition
                          );

    if ( hr == HRESULT_FROM_WIN32( ERROR_ALREADY_EXISTS ) )
    {
        //
        // We'll quietly accept a handler already exists.
        //
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION,
                    L"Handler: '%s' already installed.",
                    szName);

        hr = S_OK;
    }

    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }
    
    //
    // Update config
    //
    hr = pAdminMgr->CommitChanges();
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:


    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
}


HRESULT
ExecuteUnInstallHandlerCA(
    IN  CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szName = NULL;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;
 
        
    hr = CoCreateInstance(__uuidof(AppHostWritableAdminManager),
                          NULL,
                          CLSCTX_INPROC_SERVER,
                          __uuidof(IAppHostWritableAdminManager),
                          (VOID **)&pAdminMgr);
    if (FAILED(hr))
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, 
                    L"CoCreateInstance failed 0x%08x", hr);
        DBGERROR_HR(hr);
        goto exit;
    }


    hr = cadata->Read( &szName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    hr = UnRegisterHandler( pAdminMgr, ROOT_CONFIG_PATH, szName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }

    //
    // Update config
    //
    hr = pAdminMgr->CommitChanges();
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    
exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
}

HRESULT
ScheduleInstallSectionDefaultsCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{

    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );
    STACK_STRU( strComponent, 128 );
    STACK_STRU( strName, 128 );
    
    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISConfigSectionDefaults`.`Name`, "
            L"`IISConfigSectionDefaults`.`SectionName`, "
            L"`IISConfigSectionDefaults`.`Component_`, "
            L"`Binary`.`Data` "
        L"FROM `IISConfigSectionDefaults`, `Binary` "
        L"WHERE `IISConfigSectionDefaults`.`BinaryName_`=`Binary`.`Name`";


    enum { CA_DEFAULTS_NAME = 1, 
           CA_DEFAULTS_SECTIONNAME,
           CA_DEFAULTS_COMPONENT,
           CA_DEFAULTS_BINARYDATA
         };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;


    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
        er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISConfigSectionDefaults");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }


    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_DEFAULTS_COMPONENT,
                                     &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
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
            goto exit;
        }

        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {
        
            cadata->Write( IIS_INSTALL_DEFAULTS );        
                
            // Get the record name
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_DEFAULTS_NAME,
                                         &strName );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            // Get the Section  name
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_DEFAULTS_SECTIONNAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            //
            // Get File from binary, write to temp file
            //
            STACK_STRU( strFilePath, MAX_PATH * 2 );

            hr = GenerateTempFileName ( strName.QueryStr(), 
                                        L"def", 
                                        &strFilePath);
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error generating temp file name for the hotfix, hr=0x%x", hr);
                goto exit;
            }
            
            hr = MsiUtilRecordReadStreamIntoFile ( hRecord,
                                                   CA_DEFAULTS_BINARYDATA, 
                                                   strFilePath.QueryStr());
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error streaming binary data into file, hr=0x%x", hr);
                goto exit;
            }

            hr = cadata->Write( strFilePath.QueryStr(), strFilePath.QueryCCH() );
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
    
}
HRESULT
ExecuteInstallSectionDefaultsCA(
    IN  CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szSectionName = NULL;
    WCHAR * szTempFileName = NULL;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;

    
    hr = CoCreateInstance(__uuidof(AppHostWritableAdminManager),
                          NULL,
                          CLSCTX_INPROC_SERVER,
                          __uuidof(IAppHostWritableAdminManager),
                          (VOID **)&pAdminMgr);
    if (FAILED(hr))
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, 
                    L"CoCreateInstance failed 0x%08x", hr);
        DBGERROR_HR(hr);
        goto exit;
    } 
    

    hr = cadata->Read( &szSectionName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }    
    hr = cadata->Read( &szTempFileName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    
    //
    // Set Section Defaults from temp file created during schedule
    //
    hr = ResetConfigSectionFromFile( szTempFileName, szSectionName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    // delete temp file. No error checking.
    //
    ::DeleteFileW( szTempFileName );

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }
    
    //
    // Update config
    //
    hr = pAdminMgr->CommitChanges();
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
    
}

HRESULT
ScheduleInstallSectionAdditionsCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{

    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );
    STACK_STRU( strComponent, 128 );
    STACK_STRU( strName, 128 );
    
    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISConfigSectionAdditions`.`Name`, "
            L"`IISConfigSectionAdditions`.`SectionName`, "
            L"`IISConfigSectionAdditions`.`Component_`, "
            L"`Binary`.`Data` "
        L"FROM `IISConfigSectionAdditions`, `Binary` "
        L"WHERE `IISConfigSectionAdditions`.`BinaryName_`=`Binary`.`Name`";


    enum { CA_SECTION_ADDITION_NAME = 1, 
           CA_SECTION_ADDITION_SECTIONNAME,
           CA_SECTION_ADDITION_COMPONENT,
           CA_SECTION_ADDITION_BINARYDATA
         };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;


    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    //  Does table exist
    //
    UINT er = ERROR_SUCCESS;
        er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISConfigSectionAdditions");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }


    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_SECTION_ADDITION_COMPONENT,
                                     &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
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
            goto exit;
        }

        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {
        
            cadata->Write( IIS_INSTALL_SECTION_ADDITIONS);        
                
            // Get the record name
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_SECTION_ADDITION_NAME,
                                         &strName );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            // Get the Section  name
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_SECTION_ADDITION_SECTIONNAME,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }

            //
            // Get File from binary, write to temp file
            //
            STACK_STRU( strFilePath, MAX_PATH * 2 );

            hr = GenerateTempFileName ( strName.QueryStr(), 
                                        L"def", 
                                        &strFilePath);
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error generating temp file name for the hotfix, hr=0x%x", hr);
                goto exit;
            }
            
            hr = MsiUtilRecordReadStreamIntoFile ( hRecord,
                                                   CA_SECTION_ADDITION_BINARYDATA, 
                                                   strFilePath.QueryStr());
            if( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error streaming binary data into file, hr=0x%x", hr);
                goto exit;
            }

            hr = cadata->Write( strFilePath.QueryStr(), strFilePath.QueryCCH() );
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
    
}
HRESULT
ExecuteInstallSectionAdditionsCA(
    IN  CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szSectionName = NULL;
    WCHAR * szTempFileName = NULL;

    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;

    
    hr = CoCreateInstance(__uuidof(AppHostWritableAdminManager),
                          NULL,
                          CLSCTX_INPROC_SERVER,
                          __uuidof(IAppHostWritableAdminManager),
                          (VOID **)&pAdminMgr);
    if (FAILED(hr))
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, 
                    L"CoCreateInstance failed 0x%08x", hr);
        DBGERROR_HR(hr);
        goto exit;
    } 
    

    hr = cadata->Read( &szSectionName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }    
    hr = cadata->Read( &szTempFileName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    
    //
    // Set Section Defaults from temp file created during schedule
    //
    hr = AppendConfigSectionFromFile( szTempFileName, szSectionName );
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    // delete temp file. No error checking.
    //
    ::DeleteFileW( szTempFileName );

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }
    
    //
    // Update config
    //
    hr = pAdminMgr->CommitChanges();
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
    
}

HRESULT
ScheduleInstallCgiRestrictionsCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{

    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );
    STACK_STRU( strComponent, 128 );
    STACK_STRU( strName, 128 );
    
    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISCgiRestriction`.`Name`, "
            L"`IISCgiRestriction`.`Component_`, "
            L"`IISCgiRestriction`.`Path`, "
            L"`IISCgiRestriction`.`Allowed`, "
            L"`IISCgiRestriction`.`GroupId`, "
            L"`IISCgiRestriction`.`Description` "
        L"FROM `IISCgiRestriction` ";

    enum { CA_CGI_NAME = 1, 
           CA_CGI_COMPONENT,
           CA_CGI_PATH,
           CA_CGI_ALLOWED,
           CA_CGI_GROUPID,
           CA_CGI_DESC
         };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;


    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
        er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISCgiRestriction");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }

    cadata->Write( IIS_INSTALL_CGIRESTRICTIONS );

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_CGI_COMPONENT,
                                     &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
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
            goto exit;
        }

        if ( MsiUtilIsInstalling( installStateCurrent, installStateAction ) ||
            MsiUtilIsReInstalling( installStateCurrent, installStateAction ) )
        {        

            // Get the Path
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_CGI_PATH,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            // Get the Allowed
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_CGI_ALLOWED,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            // Get the GroupId
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_CGI_GROUPID,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }  
            // Get the Description
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_CGI_DESC,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }  
        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;

}

HRESULT
ScheduleUnInstallCgiRestrictionsCA(
    IN  MSIHANDLE   hInstall,
    IN  CA_DATA_WRITER * cadata
    )
{

    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    MSIHANDLE hDatabase = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hRecord = NULL;

    STACK_STRU( strData, 128 );
    STACK_STRU( strComponent, 128 );
    STACK_STRU( strName, 128 );
    
    CONST WCHAR * szQuery =
        L"SELECT "
            L"`IISCgiRestriction`.`Name`, "
            L"`IISCgiRestriction`.`Component_`, "
            L"`IISCgiRestriction`.`Path`, "
        L"FROM `IISCgiRestriction` ";

    enum { CA_CGI_NAME = 1, 
           CA_CGI_COMPONENT,
           CA_CGI_PATH,
           CA_CGI_ALLOWED,
           CA_CGI_GROUPID,
           CA_CGI_DESC
         };

    INSTALLSTATE    installStateCurrent;
    INSTALLSTATE    installStateAction;

    hDatabase = MsiGetActiveDatabase( hInstall );
    if ( !hDatabase )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }
    //
    //Does table exist
    //
    UINT er = ERROR_SUCCESS;
        er = ::MsiDatabaseIsTablePersistentW(hDatabase, L"IISCgiRestriction");
    if (MSICONDITION_TRUE != er)
    {
        IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' Table not found, exiting",
        UNITEXT(__FUNCTION__)
        );
        goto exit;        
    }


    cadata->Write( IIS_UNINSTALL_CGIRESTRICTIONS );

    status = MsiDatabaseOpenViewW( hDatabase, szQuery, &hView );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiViewExecute( hView, NULL );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto exit;
    }

    while ( ERROR_SUCCESS == MsiViewFetch( hView, &hRecord ) )
    {
        hr = MsiUtilRecordGetString( hRecord,
                                     CA_CGI_COMPONENT,
                                     &strComponent );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
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
            goto exit;
        }

        if ( MsiUtilIsUnInstalling( installStateCurrent, installStateAction ) )
        {        

            // Get the Path
            hr = MsiUtilRecordGetString( hRecord,
                                         CA_CGI_PATH,
                                         &strData );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
            hr = cadata->Write( strData.QueryStr(), strData.QueryCCH() );
            if ( FAILED(hr) )
            {
                DBGERROR_HR(hr);
                goto exit;
            }
        }

        if ( hRecord )
        {
            MsiCloseHandle( hRecord );
            hRecord = NULL;
        }
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

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );


    return hr;
    
}


HRESULT
ExecuteInstallCgiRestrictionsCA(
    IN  CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szPath = NULL;
    WCHAR * szAllowed = NULL;
    WCHAR * szGroupId = NULL;
    WCHAR * szDescription = NULL;
    BOOL fAllowed = FALSE; 
        
    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;

    
    hr = CoCreateInstance(__uuidof(AppHostWritableAdminManager),
                          NULL,
                          CLSCTX_INPROC_SERVER,
                          __uuidof(IAppHostWritableAdminManager),
                          (VOID **)&pAdminMgr);
    if (FAILED(hr))
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, 
                    L"CoCreateInstance failed 0x%08x", hr);
        DBGERROR_HR(hr);
        goto exit;
    } 
    

    while ( SUCCEEDED(hr = cadata->Read( &szPath )) )
    {

        hr = cadata->Read( &szAllowed );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }
        hr = cadata->Read( &szGroupId );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }        
        hr = cadata->Read( &szDescription );
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }   
        
        if ( 0 == wcscmp( szAllowed, L"true" ) )
        {
            fAllowed = TRUE;
        }
        else if  ( 0 == wcscmp( szAllowed, L"false" ) )
        {
            fAllowed = FALSE;        
        }
        
        hr = RegisterCgiRestriction(
                pAdminMgr,
                ROOT_CONFIG_PATH,
                szPath,
                fAllowed,
                szGroupId,
                szDescription
                );        
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }               
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }
    
    //
    // Update config
    //
    hr = pAdminMgr->CommitChanges();
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:


    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
    
}


HRESULT
ExecuteUnInstallCgiRestrictionsCA(
    IN  CA_DATA_READER * cadata
    )
{
    HRESULT hr = NOERROR;

    WCHAR * szPath = NULL;
        
    CComPtr<IAppHostWritableAdminManager>   pAdminMgr;

    
    hr = CoCreateInstance(__uuidof(AppHostWritableAdminManager),
                          NULL,
                          CLSCTX_INPROC_SERVER,
                          __uuidof(IAppHostWritableAdminManager),
                          (VOID **)&pAdminMgr);
    if (FAILED(hr))
    {
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, 
                    L"CoCreateInstance failed 0x%08x", hr);
        DBGERROR_HR(hr);
        goto exit;
    } 
    

    while ( SUCCEEDED(hr = cadata->Read( &szPath )) )
    {
        hr = UnRegisterCgiRestriction(
                pAdminMgr,
                ROOT_CONFIG_PATH,
                szPath,
                FALSE
                );        
        if ( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            hr = S_OK;
        }               
    }

    if ( HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS) == hr )
    {
        hr = S_OK;
    }
    
    //
    // Update config
    //
    hr = pAdminMgr->CommitChanges();
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    IISLogWrite(
        SETUP_LOG_SEVERITY_INFORMATION,
        L"CA '%s' completed with return code hr=0x%x",
        UNITEXT(__FUNCTION__),
        hr );

    return hr;
}


