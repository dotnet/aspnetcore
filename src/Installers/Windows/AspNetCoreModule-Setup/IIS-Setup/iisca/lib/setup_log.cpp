// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"
#include "ntassert.h"
//
// SETUP_LOG class, used only by MsiLogXXX functions not used directly by CA code
//
class SETUP_LOG
{

public:

    SETUP_LOG(
        VOID
        )
    {
        _severityThreshold = SETUP_LOG_SEVERITY_INFORMATION;
    }

    ~SETUP_LOG(
        VOID
        )
    {
    }

    VOID
    Initialize(
        IN MSIHANDLE    hInstall,
        IN LPCWSTR      pszCAName
    );

    VOID
    Close(
        VOID
        );

    VOID
    Write(
        IN CONST SETUP_LOG_SEVERITY setupLogSeverity,
        IN LPCWSTR                  pszLogMessageFormat
        );

private:

    MSIHANDLE           _hInstall;
    SETUP_LOG_SEVERITY  _severityThreshold;
    STRU                _strSCANamePrefix;
    
private:
    HRESULT
    WriteMSIMessage(
        IN STRU * pstrLogMessage
        );

    HRESULT
    SETUP_LOG::FmtCAName(
        IN STRU * pstrFmtCAName
        );
};   


VOID
SETUP_LOG::Initialize(
    IN MSIHANDLE    hInstall,
    IN LPCWSTR      pszCAName    
    )
/*++

Routine Description:

    Initialization. Opens handle to the log file and writes a message.

Return Value:

    HRESULT

--*/
{
    STACK_STRU ( strMsiRegKey, 64 );   
    STACK_STRU ( strStartLogMessage, MAX_PATH );
    STACK_STRU ( strMsiLoggingValue, 32 );
    DWORD dwBufLen    = strMsiRegKey.QuerySizeCCH(); 
    DWORD status      = ERROR_SUCCESS;
    HKEY  hKey        = NULL;
    
    //for checking logging level
    WORD i;

    //
    //Set MSI handle & CA name
    //
    
    _hInstall = hInstall; 

    //
    //Prefix message with CAname
    //
    _strSCANamePrefix.Copy( L"IISCA " );
    _strSCANamePrefix.Append( pszCAName );
    _strSCANamePrefix.Append( L" : " );

    //
    //test MsiLogging property MSI 4.0+
    //     
    
    (VOID) MsiUtilGetProperty( hInstall, L"MsiLogging", &strMsiLoggingValue );

    if( !strMsiLoggingValue.IsEmpty() )
    {
        WCHAR * szValueBuf = strMsiLoggingValue.QueryStr();
        if( wcschr( szValueBuf, L'v' ) || wcschr( szValueBuf, L'V' ) )
        {
            _severityThreshold =  SETUP_LOG_SEVERITY_DEBUG ;
        }
    } 

    if ( _severityThreshold !=  SETUP_LOG_SEVERITY_DEBUG )
    {
        //
        // Last chance - check Logging Registry Key/value
        //

        status = RegOpenKeyEx( HKEY_LOCAL_MACHINE,
                                L"SOFTWARE\\Policies\\Microsoft\\Windows\\Installer",
                                0,  
                                KEY_QUERY_VALUE, 
                                &hKey );

        if ( status == ERROR_SUCCESS )
        {
            status = RegQueryValueEx( hKey,
                                        L"Logging",
                                        NULL,
                                        NULL,
                                       (LPBYTE) strMsiRegKey.QueryStr(),
                                        &dwBufLen);

            strMsiRegKey.SyncWithBuffer( );  
        }
   
        if ( !( status != ERROR_SUCCESS ) || ( dwBufLen > strMsiRegKey.QuerySizeCCH() ) )
        {
            //
            //Have Logging key Value.. we will log something
            //
            // Check logging flags for verbosity 'v'
            //
            for ( i = 0; i < strMsiRegKey.QueryCCH() ; i++ )
            {
                if ( strMsiRegKey.QueryStr()[ i ] == L'V' || strMsiRegKey.QueryStr()[ i ] == L'v' )
                {
                    _severityThreshold =  SETUP_LOG_SEVERITY_DEBUG ;
                    break;
                }
            }
        }
    }
     
    //
    //  write the start message, prifix message with CAname
    //
    strStartLogMessage.Append( _strSCANamePrefix.QueryStr() );

    strStartLogMessage.Append( L"Begin CA Setup" );

    //
    // write the message
    //
    
    WriteMSIMessage( &strStartLogMessage );


    if ( hKey != NULL )
    {
        RegCloseKey( hKey );
        hKey = NULL;
    }

    return;
}

VOID
SETUP_LOG::Close(
    VOID
    )

{
    STACK_STRU ( strEndLogMessage, MAX_PATH );

    strEndLogMessage.Append( _strSCANamePrefix.QueryStr() );
    strEndLogMessage.Append( L"End CA Setup" );

    //
    // write the message
    //
    
    WriteMSIMessage( &strEndLogMessage);

    return;
}

VOID
SETUP_LOG::Write(
    IN SETUP_LOG_SEVERITY     setupLogSeverity,
    IN LPCWSTR                pszLogMessage
    )
/*++

Routine Description:

    Write a formatted message to the log file.  Note that the message
    will not be written to the log file if the severity is below that
    of the current threshold.

Arguments:

    setupLogSeverity - Severity of the message
    pszLogMessageFormat - Format of the log message  

--*/
{
    STACK_STRU ( struLogMessage, MAX_PATH );

    DBG_ASSERT( pszLogMessage);
    
    if ( pszLogMessage == NULL )
    {
        goto Exit;
    }

    //
    // If this message does not have high enough severity, we are done.
    //
    if ( setupLogSeverity < _severityThreshold )
    {
        goto Exit;
    }
 
    //
    //Prefix message with CAname
    //
    
    struLogMessage.Append( _strSCANamePrefix.QueryStr() );

    //
    // Prefix the high severity messages so we can easily spot errors.
    //

    if ( setupLogSeverity == SETUP_LOG_SEVERITY_WARNING )
    {
        struLogMessage.Append( L"< WARNING! > " );
    }
    else if ( setupLogSeverity == SETUP_LOG_SEVERITY_ERROR )
    {
        struLogMessage.Append( L"< !!ERROR!! > " );
    }

    struLogMessage.Append( pszLogMessage );

    //
    // Write to the log file 
    //

    WriteMSIMessage( &struLogMessage );

Exit:

    return;
}

HRESULT
SETUP_LOG::WriteMSIMessage(
    STRU *  pstrLogMessage
    )

{
    HRESULT      hr = S_OK;
    PMSIHANDLE   msiRow; 
    UINT         status = 0;
    //
    // get an MSI record
    //  
    
    //consider passing a parameterized string here
    msiRow = MsiCreateRecord( 1 );
    if ( msiRow == NULL )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto Exit;
    }    

    //
    // put message in msi record
    //   

    status = MsiRecordSetStringW( msiRow, 1, (LPCWSTR)pstrLogMessage->QueryStr() );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto Exit;
    }

    //
    // send message to MSI log file
    //      

    status = MsiProcessMessage( _hInstall, INSTALLMESSAGE_INFO, msiRow );
    if ( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32( status );
        DBGERROR_HR(hr);
        goto Exit;
    }

Exit:

    return hr;
}

//
//Use static global instance of SETUP_LOG class
//
static SETUP_LOG * g_pSetupLog = NULL;

//
//MSI Log safe functions, used directly by CA code
//
VOID
IISLogInitialize(
    IN MSIHANDLE    hInstall,
    IN LPCWSTR      pszCAName    
    )    
{
    HRESULT hr  = S_OK; 
  
    //debug assert that it is always null. becaues previous custom action should have cleaned it up
    DBG_ASSERT( g_pSetupLog == NULL );
    
    if ( g_pSetupLog == NULL )
    {
       g_pSetupLog = new SETUP_LOG();
       if ( g_pSetupLog == NULL )
       {       
            hr = HRESULT_FROM_WIN32( ERROR_NOT_ENOUGH_MEMORY );
            DBGERROR_HR(hr);  
            goto Exit;
       }
    }

    //
    // init 
    //    
 
    g_pSetupLog->Initialize( hInstall, pszCAName );

Exit:    
    return;  
}


VOID
IISLogClose(
    VOID
    )
{   
    if ( !g_pSetupLog )
    { 
        goto Exit;      
    }

    //
    // do the call
    //
    
    g_pSetupLog->Close();
    
    //
    //Done with SETUP_LOG instance
    //
    
    delete g_pSetupLog;
    g_pSetupLog = NULL;

Exit:      
    
    return ;
}

 
VOID
IISLogWrite(
    IN SETUP_LOG_SEVERITY   setupLogSeverity,
    IN LPCWSTR              pszLogMessageFormat,
    ...
    )
{
    va_list argsList;
    va_start ( argsList, pszLogMessageFormat );

    STACK_STRU ( struLogMessage, 128 );
    //debug assert that it is not null
    DBG_ASSERT( g_pSetupLog );
    
    if ( !g_pSetupLog )
    { 
        goto Exit;      
    }   
    
    //
    // Form struLogMessage so that we can write a single string
    //    
                            
    struLogMessage.SafeVsnwprintf( pszLogMessageFormat, argsList );                          
                               
    g_pSetupLog->Write( setupLogSeverity, struLogMessage.QueryStr() );

Exit:
    va_end ( argsList );
    return;
}
