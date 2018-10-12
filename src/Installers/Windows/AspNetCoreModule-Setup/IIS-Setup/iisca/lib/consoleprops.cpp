// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

#define CheckAndGoToExit(hr,sev,msg,prop) if (FAILED(hr)) { DBGERROR_HR(hr); IISLogWrite(sev, msg, prop, hr);goto exit;}
#define CheckAndReturnHr(hr,sev,msg,prop) if (FAILED(hr)) { DBGERROR_HR(hr); IISLogWrite(sev, msg, prop, hr);return hr;}


HRESULT
GetConsoleIntProperty(
					  IN MSIHANDLE hRecord,
					  IN UINT field,
					  __inout CA_DATA_WRITER * cadata)
{
	HRESULT hr = S_OK;

	UINT dwValue = 0;
	hr = MsiUtilRecordGetInteger( hRecord,
		field,
		&dwValue );
	if ( FAILED(hr) )
	{
		DBGERROR_HR(hr);
		IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", field, hr);
		return hr;
	}

	hr = cadata->Write(dwValue);
	if ( FAILED(hr) )
	{
		DBGERROR_HR(hr);
		IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error writing custom action data, hr=0x%x", hr);
		return hr;
	}

	return hr;
}

HRESULT
GetConsoleProperties( 
					 IN      MSIHANDLE       hRecord,
					 __inout CA_DATA_WRITER * cadata)
{
	HRESULT hr = S_OK;

	/*STACK_STRU( strTextColour, 128 );
	STACK_STRU( strBackgroundColour, 128 );*/

	enum {
		CA_CONSOLEPROPS_QUCIKEDIT = 5,
		CA_CONSOLEPROPS_INSERTMODE,
		CA_CONSOLEPROPS_WINDOWWIDTH,
		CA_CONSOLEPROPS_WINDOWHEIGHT,
		CA_CONSOLEPROPS_BUFFERWIDTH,
		CA_CONSOLEPROPS_BUFFERHEIGHT,
	/*	CA_CONSOLEPROPS_TEXTCOLOUR,
		CA_CONSOLEPROPS_BACKGROUNDCOLOUR*/
	};

	// GET QuickEdit int
	hr = GetConsoleIntProperty( hRecord,
		CA_CONSOLEPROPS_QUCIKEDIT,
		cadata );
	if ( FAILED(hr) ) { return hr;}

	// GET InsertMode
	hr = GetConsoleIntProperty( hRecord,
		CA_CONSOLEPROPS_INSERTMODE,
		cadata );
	if ( FAILED(hr) ) { return hr;}


	// GET WindowWidth
	hr = GetConsoleIntProperty( hRecord,
		CA_CONSOLEPROPS_WINDOWWIDTH,
		cadata );
	if ( FAILED(hr) ) { return hr;}


	// GET WindowHeight
	hr = GetConsoleIntProperty( hRecord,
		CA_CONSOLEPROPS_WINDOWHEIGHT,
		cadata );
	if ( FAILED(hr) ) { return hr;}


	// GET BufferWidth
	hr = GetConsoleIntProperty( hRecord,
		CA_CONSOLEPROPS_BUFFERWIDTH,
		cadata );
	if ( FAILED(hr) ) { return hr;}


	// GET BufferHeight
	hr = GetConsoleIntProperty( hRecord,
		CA_CONSOLEPROPS_BUFFERHEIGHT,
		cadata );
	if ( FAILED(hr) ) { return hr;}

	//// Get Text colours
	//hr = MsiUtilRecordGetString( hRecord,
	//	CA_CONSOLEPROPS_TEXTCOLOUR,
	//	&strTextColour );

	//CheckAndReturnHr(hr, SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", CA_CONSOLEPROPS_TEXTCOLOUR);

	//hr = cadata->Write( strTextColour.QueryStr(), strTextColour.QueryCCH() );
	//if ( FAILED(hr) )
	//{
	//	DBGERROR_HR(hr);
	//	IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error writing custom action data, hr=0x%x", hr);
	//	return hr;
	//}

	//// Get Background colours
	//hr = MsiUtilRecordGetString( hRecord,
	//	CA_CONSOLEPROPS_BACKGROUNDCOLOUR,
	//	&strBackgroundColour );

	//CheckAndReturnHr(hr, SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", CA_CONSOLEPROPS_TEXTCOLOUR);

	//hr = cadata->Write( strBackgroundColour.QueryStr(), strBackgroundColour.QueryCCH() );
	//if ( FAILED(hr) )
	//{
	//	DBGERROR_HR(hr);
	//	IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error writing custom action data, hr=0x%x", hr);
	//	return hr;
	//}

	return hr;
}

UINT 
WINAPI 
ScheduleSetConsolePropertiesCA(
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
		L"`IISShortcutConsoleProperties`.`Shortcut_`, "
		L"`Shortcut`.`Component_`, "
		L"`Shortcut`.`Name`, "
		L"`Directory`.`Directory`, "
		L"`IISShortcutConsoleProperties`.`QuickEdit`, "
		L"`IISShortcutConsoleProperties`.`InsertMode`, "
		L"`IISShortcutConsoleProperties`.`WindowWidth`, "
		L"`IISShortcutConsoleProperties`.`WindowHeight`, "
		L"`IISShortcutConsoleProperties`.`BufferWidth`, "
		L"`IISShortcutConsoleProperties`.`BufferHeight` "
		/*L"`IISShortcutConsoleProperties`.`TextColor`, "
		L"`IISShortcutConsoleProperties`.`BackgroundColor` "*/
		L"FROM `IISShortcutConsoleProperties`, `Shortcut`, `Directory`  "
		L"WHERE `IISShortcutConsoleProperties`.`Shortcut_`=`Shortcut`.`Shortcut` "
		L"AND `Shortcut`.`Directory_`=`Directory`.`Directory`";


	CA_DATA_WRITER cadata;
	INSTALLSTATE installStateCurrent;
	INSTALLSTATE installStateAction;

	BOOL scheduleDefferedCA = FALSE;

	enum { 
		CA_CONSOLEPROPS_SHORTCUT = 1,
		CA_CONSOLEPROPS_COMPONENT,
		CA_CONSOLEPROPS_SHORTCUTNAME,
		CA_CONSOLEPROPS_DIRECTORY,
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
			CA_CONSOLEPROPS_COMPONENT,
			&strComponent );
		CheckAndGoToExit(hr, SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", CA_CONSOLEPROPS_COMPONENT);

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
			// Get Shortcut location
			{
				// GET: directory where shortcut is
				// Get the directory id for the shortcut
				hr = MsiUtilRecordGetString( hRecord,
					CA_CONSOLEPROPS_DIRECTORY,
					&strDirectoryId );

				CheckAndGoToExit(hr, SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", CA_CONSOLEPROPS_DIRECTORY);

				// Get directory path
				hr = MsiUtilGetProperty(hInstall, strDirectoryId.QueryStr(), &strDirectoryName ); 

				CheckAndGoToExit(hr, SETUP_LOG_SEVERITY_ERROR, L"Error getting value for directory record %s, hr=0x%x", strDirectoryId.QueryStr());

				IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
					L"Shortcut Directory: '%s'.", 
					strDirectoryName.QueryStr());
				// ENDGET: directory where shortcut is


				// GET: Short and Long names of shortcuts
				hr = MsiUtilRecordGetString( hRecord,
					CA_CONSOLEPROPS_SHORTCUTNAME,
					&strShortcutName );

				CheckAndGoToExit(hr, SETUP_LOG_SEVERITY_ERROR, L"Error getting column %d from record, hr=0x%x", CA_CONSOLEPROPS_SHORTCUTNAME);


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

						// Get the console properties
						hr = GetConsoleProperties(hRecord, &cadata);
						if( FAILED(hr))
						{
							DBGERROR_HR(hr);
							IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting console properties, hr=0x%x", hr);
							goto exit;
						}

						// Only schedule custom action if needed
						scheduleDefferedCA = TRUE;
					}
				}
				// ENDGET: Short and Long names of shortcuts
			}
			// End OF getting shortcut location
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
			L"ExecuteSetConsoleProperties",
			cadata.QueryData() );
		if ( FAILED(hr) )
		{
			DBGERROR_HR(hr);
			IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error scheduling custom action, hr=0x%x", hr);
			goto exit;
		}

		IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
			L"Custom action ExecuteSetConsoleProperties scheduled");
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

void IntializeConsoleProps(
						   IN NT_CONSOLE_PROPS* pConsoleProps
						   )
{
	if (!pConsoleProps)
	{
		return;
	}

	pConsoleProps->dbh.cbSize = sizeof(NT_CONSOLE_PROPS);
	pConsoleProps->dbh.dwSignature = NT_CONSOLE_PROPS_SIG;
	pConsoleProps->bFullScreen = false;
	pConsoleProps->uHistoryBufferSize = 50;
	pConsoleProps->uNumberOfHistoryBuffers = 4;
	pConsoleProps->uCursorSize = 25;

	pConsoleProps->ColorTable[0] = RGB(0,0,0); 
	pConsoleProps->ColorTable[1] = RGB(0,0,128);
	pConsoleProps->ColorTable[2] = RGB(0,128,0);
	pConsoleProps->ColorTable[3] = RGB(0,128,128);
	pConsoleProps->ColorTable[4] = RGB(128,0,0);
	pConsoleProps->ColorTable[5] = RGB(128,0,128);
	pConsoleProps->ColorTable[6] = RGB(128,128,0);
	pConsoleProps->ColorTable[7] = RGB(192,192,192);
	pConsoleProps->ColorTable[8] = RGB(128,128,128);
	pConsoleProps->ColorTable[9] = RGB(0,0,255);
	pConsoleProps->ColorTable[10] = RGB(0,255,0);
	pConsoleProps->ColorTable[11] = RGB(0,255,255);
	pConsoleProps->ColorTable[12] = RGB(255,0,0);
	pConsoleProps->ColorTable[13] = RGB(255,0,255);
	pConsoleProps->ColorTable[14] = RGB(255,255,0);
	pConsoleProps->ColorTable[15] = RGB(255,255,255);

	pConsoleProps->wPopupFillAttribute = ((15 << 4) | 3);
}


UINT 
WINAPI 
ExecuteSetConsolePropertiesCA(
							  IN MSIHANDLE hInstall
							  )
{
	HRESULT hr = S_OK;

	BOOL    fCoInit = FALSE;

	WCHAR * szShortcutFile = NULL;
	/*WCHAR * szTextColour = NULL;
	WCHAR * szBackgroundColour = NULL;*/
	INT dwQuickEdit = 0;
	INT dwInsertMode = 0;
	INT dwWindowWidth = 120;
	INT dwWindowHeight = 50;
	INT dwBufferWidth = 120;
	INT dwBufferHeight = 3000;


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
		hr = cadata.Read( &dwQuickEdit);
		if ( FAILED(hr) )
		{
			DBGERROR_HR(hr);
			goto exit;
		}

		hr = cadata.Read( &dwInsertMode);
		if ( FAILED(hr) )
		{
			DBGERROR_HR(hr);
			goto exit;
		}

		hr = cadata.Read( &dwWindowWidth);
		if ( FAILED(hr) )
		{
			DBGERROR_HR(hr);
			goto exit;
		}

		hr = cadata.Read( &dwWindowHeight);
		if ( FAILED(hr) )
		{
			DBGERROR_HR(hr);
			goto exit;
		}

		hr = cadata.Read( &dwBufferWidth);
		if ( FAILED(hr) )
		{
			DBGERROR_HR(hr);
			goto exit;
		}

		hr = cadata.Read( &dwBufferHeight);
		if ( FAILED(hr) )
		{
			DBGERROR_HR(hr);
			goto exit;
		}

		/*hr = cadata.Read( &szTextColour);
		if ( FAILED(hr) )
		{
			DBGERROR_HR(hr);
			goto exit;
		}

		hr = cadata.Read( &szBackgroundColour);
		if ( FAILED(hr) )
		{
			DBGERROR_HR(hr);
			goto exit;
		}*/

		// Check to see if file exists
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

			NT_CONSOLE_PROPS* pConsoleProps = NULL;

			hr = spdl->CopyDataBlock(NT_CONSOLE_PROPS_SIG, (LPVOID*) &pConsoleProps);
			if ( FAILED(hr) || (NULL == pConsoleProps) )
			{
				// Create new console properties and set defaults
				pConsoleProps = (NT_CONSOLE_PROPS *)malloc(sizeof(NT_CONSOLE_PROPS));
				memset(pConsoleProps,0,sizeof(NT_CONSOLE_PROPS));
				IntializeConsoleProps(pConsoleProps);
			}

			pConsoleProps->bQuickEdit = dwQuickEdit > 0;
			pConsoleProps->bInsertMode = dwInsertMode > 0;
			pConsoleProps->dwWindowSize.X = (short) dwWindowWidth;
			pConsoleProps->dwWindowSize.Y = (short) dwWindowHeight;
			pConsoleProps->dwScreenBufferSize.X = (short) dwBufferWidth;
			pConsoleProps->dwScreenBufferSize.Y = (short) dwBufferHeight;

			pConsoleProps->ColorTable[6] = RGB(238, 237, 240); // text color
			pConsoleProps->ColorTable[5] = RGB(1, 36, 86); // backgroud color
			pConsoleProps->wFillAttribute = ((5 << 4) | 6 );


			spdl->RemoveDataBlock(NT_CONSOLE_PROPS_SIG);
			if (FAILED(hr = spdl->AddDataBlock((void *)pConsoleProps)))
			{
				DBGERROR_HR(hr);
				IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error setting NT_CONSOLE_PROPS on shortcut file %s, hr=0x%x", szShortcutFile, hr);
				goto exit;
			}

			if (FAILED(hr = sppf->Save(NULL, TRUE)))
			{
			    DBGERROR_HR(hr);
			    IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error saving changes to shortcut, hr=0x%x", hr);
			    goto exit;
			}

			IISLogWrite ( SETUP_LOG_SEVERITY_INFORMATION, 
				L"Successfully added properties to shortcut %s", 
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