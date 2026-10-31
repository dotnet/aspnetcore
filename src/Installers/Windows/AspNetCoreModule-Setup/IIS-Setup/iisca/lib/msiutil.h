// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

HRESULT
MsiUtilGetProperty(
    IN      MSIHANDLE       hInstall,
    __in    PCWSTR          szName,
    __inout STRU *          pstrProperty
    );

HRESULT
MsiUtilScheduleDeferredAction(
    IN      MSIHANDLE       hInstall,
    __in    PCWSTR          szAction,
    __in    PCWSTR          szData
    );

HRESULT 
MsiUtilRecordGetInteger(
    IN      MSIHANDLE       hRecord,
    IN      UINT            field,
    __inout UINT *          pInt
    );

HRESULT
MsiUtilRecordGetString(
    IN      MSIHANDLE       hRecord,
    IN      UINT            field,
    __inout STRU *          pstr
    );

HRESULT
MsiUtilRecordReadStreamIntoFile(
    IN      MSIHANDLE       hRecord,
    IN      UINT            field,
    IN      PCWSTR          szFileName
    );

HRESULT
MsiUtilFormatString(
    IN      MSIHANDLE           hInstall,
    __inout STRU *              pstrData
    );

class CA_DATA_WRITER
{
public:

    CA_DATA_WRITER()
    {
    }
    
    HRESULT
    Write(
        CONST WCHAR * sz
    )
    {
        return WriteInternal( sz, (INT)wcslen(sz) );
    }

    HRESULT
    Write(
        CONST WCHAR * sz,
        INT cch
    )
    {
        return WriteInternal( sz, cch );
    }

    HRESULT
    Write(
        INT n
    )
    {
        HRESULT hr;

        WCHAR buffer[20];
        hr = StringCchPrintfW( buffer,
                               sizeof(buffer)/sizeof(buffer[0]),
                               L"%d",
                               n );
        if( FAILED(hr) )
        {
            return hr;
        }

        return WriteInternal( buffer, (INT)wcslen(buffer) );
    }

    CONST WCHAR *
    QueryData() const
    {
        return _data.QueryStr();
    }

protected:

    HRESULT
    WriteInternal(
        CONST WCHAR * sz,
        INT n
    );

    STRU    _data;
};

class CA_DATA_READER
{
public:

    CA_DATA_READER() :
        _current( NULL )
    {
    }

    ~CA_DATA_READER()
    {
    }

    HRESULT
    LoadDeferredCAData(
        MSIHANDLE   hInstall
    )
    {
        HRESULT hr = MsiUtilGetProperty( hInstall, L"CustomActionData", &_strCustomActionData );
        _current = _strCustomActionData.QueryStr();
        return hr;
    }

    HRESULT
    Read(
        __deref_out_z WCHAR ** psz
    )
    {
        *psz = ExtractString();
        if( !*psz )
        {
            return HRESULT_FROM_WIN32( ERROR_NO_MORE_ITEMS );
        }
        return S_OK;
    }

    HRESULT
    Read(
        INT * pi
    )
    {
        CONST WCHAR * sz = ExtractString();
        if( !sz )
        {
            return HRESULT_FROM_WIN32( ERROR_NO_MORE_ITEMS );
        }
        *pi = wcstol( sz, NULL, 10 );
        return S_OK;
    }

private:

    WCHAR *
    ExtractString();

    STRU    _strCustomActionData;
    WCHAR * _current;
};


BOOL
MsiUtilIsReInstalling(
        INSTALLSTATE isInstalled,
        INSTALLSTATE isAction
        );

BOOL
MsiUtilIsInstalling(
        INSTALLSTATE isInstalled,
        INSTALLSTATE isAction
        );

BOOL
MsiUtilIsUnInstalling(
        INSTALLSTATE isInstalled,
        INSTALLSTATE isAction
        );

HRESULT
GenerateTempFileName(
    __in    PCWSTR      szPrefix,
    __in    PCWSTR      szExtension,
    __inout STRU *      pstr
);