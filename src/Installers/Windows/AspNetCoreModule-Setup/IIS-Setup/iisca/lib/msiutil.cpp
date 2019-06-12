// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

HRESULT
MsiUtilGetProperty(
    IN      MSIHANDLE       hInstall,
    __in    PCWSTR          szName,
    __inout STRU *          pstrProperty
    )
{
    HRESULT hr = NOERROR;
    DWORD cch = 0;
    WCHAR dummy = L'\0';
    DWORD status;
    
    pstrProperty->Reset();

    //
    // Get the length.
    //

    status = MsiGetPropertyW( hInstall,
                              szName,
                              &dummy,
                              &cch );

    if( status != ERROR_MORE_DATA )
    {
        hr = HRESULT_FROM_WIN32(status);
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Return is count of characters w/o NULL
    //

    cch++;
    
    hr = pstrProperty->Resize( cch );
    if( FAILED( hr ) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiGetPropertyW( hInstall,
                              szName,
                              pstrProperty->QueryStr(),
                              &cch );

    if( status != ERROR_SUCCESS )
    {
        hr = HRESULT_FROM_WIN32(status);
        DBGERROR_HR(hr);
        goto exit;
    }
    
    pstrProperty->SyncWithBuffer();

exit:

    return hr;
}


HRESULT
MsiUtilScheduleDeferredAction(
    IN      MSIHANDLE       hInstall,
    __in    PCWSTR          szAction,
    __in    PCWSTR          szData
    )
{
    HRESULT hr = NOERROR;
    UINT    status;

    status = MsiSetPropertyW( hInstall,
                              szAction,
                              szData );
    if( status != ERROR_SUCCESS )
    {
        hr = HRESULT_FROM_WIN32(status);
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiDoActionW( hInstall,
                           szAction );

    if( status != ERROR_SUCCESS )
    {
        hr = HRESULT_FROM_WIN32(status );
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return hr;
}

HRESULT 
MsiUtilRecordGetInteger(
    IN      MSIHANDLE       hRecord,
    IN      UINT            field,
    __inout UINT *          pInt
    )
{
    HRESULT hr = NOERROR;
    UINT tempValue = 0;

    _ASSERTE(pInt);

    tempValue = MsiRecordGetInteger(hRecord, field);

    if( MSI_NULL_INTEGER == tempValue )
    {
        hr = E_UNEXPECTED;
        DBGERROR(( DBG_CONTEXT, "Non-integer value encountered in Integer field, %08x\n", hr ));
        goto exit;
    }

    *pInt = tempValue;

exit:
    return hr;
}

HRESULT
MsiUtilRecordGetString(
    IN      MSIHANDLE       hRecord,
    IN      UINT            field,
    __inout STRU *          pstr
    )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;

    DWORD cch = pstr->QuerySizeCCH();

    status = MsiRecordGetStringW( hRecord,
                                  field,
                                  pstr->QueryStr(),
                                  &cch );

    if( ERROR_MORE_DATA == status )
    {
        hr = pstr->Resize( ++cch );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        status = MsiRecordGetStringW( hRecord,
                                      field,
                                      pstr->QueryStr(),
                                      &cch );
    }

    if( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32(status);
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pstr->SetLen( cch );
    if( FAILED(hr) )
    {
        hr = HRESULT_FROM_WIN32(status);
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return hr;
}

HRESULT
MsiUtilRecordReadStreamIntoFile(
    IN      MSIHANDLE       hRecord,
    IN      UINT            field,
    IN      PCWSTR          szFileName
                                )
{
    HRESULT hr = NOERROR;
    UINT status = ERROR_SUCCESS;
    HANDLE hOutputFile = INVALID_HANDLE_VALUE;
    
    _ASSERTE(szFileName);

    hOutputFile = CreateFileW( szFileName,
                               GENERIC_WRITE,
                               0,
                               NULL,
                               CREATE_NEW,
                               FILE_ATTRIBUTE_NORMAL,
                               NULL);
    if ( INVALID_HANDLE_VALUE == hOutputFile )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        goto exit;
    }

    do
    {
        DWORD bytesWritten = 0;
        BOOL fRet = FALSE;
        CHAR szBuffer[4096];
        DWORD cbBuf = sizeof(szBuffer);

        status = MsiRecordReadStream(hRecord, field, szBuffer, &cbBuf);
        if ( ERROR_SUCCESS != status )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            goto exit;
        }
        else if ( 0 == cbBuf ) 
        {
            //we've reached the end of the stream
            break;
        }

        fRet = WriteFile(hOutputFile, szBuffer, cbBuf, &bytesWritten, NULL);
        if( !fRet )
        {
            hr = HRESULT_FROM_WIN32(status);
            DBGERROR_HR(hr);
            goto exit;
        }

    } while (1);


exit:

    if( INVALID_HANDLE_VALUE != hOutputFile)
    {
        CloseHandle( hOutputFile );
        hOutputFile = INVALID_HANDLE_VALUE;
    }

    return hr;
}

HRESULT
MsiUtilFormatString(
    IN      MSIHANDLE           hInstall,
    __inout STRU *              pstrData
    )
{
    HRESULT     hr = NOERROR;
    UINT        status = ERROR_SUCCESS;
    MSIHANDLE   hRecord = NULL;

    hRecord = MsiCreateRecord( 1 );
    if( !hRecord )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        goto exit;
    }

    status = MsiRecordSetStringW( hRecord,
                                  0,
                                  pstrData->QueryStr() );
    if( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32(status);
        DBGERROR_HR(hr);
        goto exit;
    }

    DWORD cch = pstrData->QuerySizeCCH();

    status = MsiFormatRecordW( hInstall,
                               hRecord,
                               pstrData->QueryStr(),
                               &cch );
    if( ERROR_MORE_DATA == status )
    {
        hr = pstrData->Resize( ++cch );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            goto exit;
        }

        status = MsiFormatRecordW( hInstall,
                                   hRecord,
                                   pstrData->QueryStr(),
                                   &cch );
    }

    if( ERROR_SUCCESS != status )
    {
        hr = HRESULT_FROM_WIN32(status);
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = pstrData->SetLen( cch );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    if( hRecord )
    {
        MsiCloseHandle( hRecord );
        hRecord = NULL;
    }

    return hr;
}

WCHAR CA_DATA_DELIM[] = { '^', 0 };

//
// BUGBUG - Prefix will not handle this
// Can I really trust this data hasn't been tampered with?
//
WCHAR *
CA_DATA_READER::ExtractString()
{
    if( !_current || *_current == 0 )
    {
        return NULL;
    }

    //
    // String format is:
    //   (len - delim - data - delim) (xN) \0
    //   "3^cat^4^fish^\0"
    //

    //
    // extract length of data
    //

    WCHAR * psz = wcsstr( _current, CA_DATA_DELIM );

    _ASSERTE( psz );
    if( psz )
    {
        *psz = 0;
        INT cch = wcstol( _current, NULL, 0 );

        //
        // advance to data
        //

        psz++;

        //
        // terminate and advance to next block
        //

        _current = psz + cch;
        *_current = 0;

        _current++;
    }

    return psz;
}

HRESULT
CA_DATA_WRITER::WriteInternal(
    CONST WCHAR * sz,
    INT n
)
{
    HRESULT hr = NOERROR;

    //
    // Write out the data length
    //

    WCHAR buffer[20];
    StringCchPrintfW( buffer, sizeof(buffer)/sizeof(buffer[0]), L"%d", n );

    hr = _data.Append( buffer );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = _data.Append( CA_DATA_DELIM );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    //
    // Write out the data
    //

    hr = _data.Append( sz );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

    hr = _data.Append( CA_DATA_DELIM );
    if( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        goto exit;
    }

exit:

    return hr;
}



BOOL
MsiUtilIsInstalling(
        INSTALLSTATE isInstalled,
        INSTALLSTATE isAction
        )
{
        return (INSTALLSTATE_LOCAL == isAction ||
                INSTALLSTATE_SOURCE == isAction ||
                (INSTALLSTATE_DEFAULT == isAction &&
                 (INSTALLSTATE_LOCAL == isInstalled ||
                  INSTALLSTATE_SOURCE == isInstalled)));
}


BOOL
MsiUtilIsReInstalling(
        INSTALLSTATE isInstalled,
        INSTALLSTATE isAction
        )
{
        return ((INSTALLSTATE_LOCAL == isAction ||
                 INSTALLSTATE_SOURCE == isAction ||
                 INSTALLSTATE_DEFAULT == isAction ||
                 INSTALLSTATE_UNKNOWN == isAction) &&
                (INSTALLSTATE_LOCAL == isInstalled ||
                 INSTALLSTATE_SOURCE == isInstalled));
}


BOOL
MsiUtilIsUnInstalling(
        INSTALLSTATE isInstalled,
        INSTALLSTATE isAction
        )
{
        return ((INSTALLSTATE_ABSENT == isAction ||
                 INSTALLSTATE_REMOVED == isAction) &&
                (INSTALLSTATE_LOCAL == isInstalled ||
                 INSTALLSTATE_SOURCE == isInstalled));
}

HRESULT
GenerateTempFileName(
    __in    PCWSTR      szPrefix,
    __in    PCWSTR      szExtension,
    __inout STRU *      pstr
)
{
    HRESULT hr = NOERROR;
    UINT status = 0;
    STACK_STRU( guidName, 128 );
    GUID guid = {0};
    DWORD cch = 0;
    
    _ASSERTE(szPrefix);
    _ASSERTE(szExtension);
    _ASSERTE(pstr);

    cch = pstr->QuerySizeCCH();

    status = GetTempPathW(cch, pstr->QueryStr());
    if ( status > cch)
    {
        cch = status;
        hr = pstr->Resize( ++cch );
        if( FAILED(hr) )
        {
            DBGERROR_HR(hr);
            IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error resizing buffer, hr=0x%x", hr);
            goto exit;
        }

        status = GetTempPathW(cch, pstr->QueryStr());
    }

    if ( 0 == status )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting temp path, hr=0x%x", hr);
        goto exit;
    }

    pstr->SyncWithBuffer();

    hr = pstr->Append(L"\\");
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error apppending \\, hr=0x%x", hr);
        goto exit;
    }

    hr = pstr->Append(szPrefix);
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error appending file prefix, hr=0x%x", hr);
        goto exit;
    }
    
    cch = guidName.QuerySizeCCH();
    hr = CoCreateGuid ( &guid );
    if ( FAILED (hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error generating the GUID, hr=0x%x", hr);
        goto exit;
    }

    if ( !StringFromGUID2( guid, guidName.QueryStr(), cch ) )
    {
        hr = E_UNEXPECTED;
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error getting string from GUID, hr=0x%x", hr);
        goto exit;
    }

    guidName.SyncWithBuffer();

    hr = pstr->Append(guidName.QueryStr());
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error appending GUID, hr=0x%x", hr);
        goto exit;
    }

    hr = pstr->Append(L".");
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error appending ., hr=0x%x", hr);
        goto exit;
    }

    hr = pstr->Append(szExtension);
    if ( FAILED(hr) )
    {
        DBGERROR_HR(hr);
        IISLogWrite(SETUP_LOG_SEVERITY_ERROR, L"Error appending extension, hr=0x%x", hr);
        goto exit;
    }

exit:
    if ( FAILED(hr) )
    {
        IISLogWrite(SETUP_LOG_SEVERITY_INFORMATION, L"Error in function %s, hr=0x%x", UNITEXT(__FUNCTION__), hr);
    }
    return hr;
}
