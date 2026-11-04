// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"
#include <Wbemidl.h>

HRESULT
RegisterMofFile(
    __in PWSTR pszFileName
)
{
    HRESULT                     hr = S_OK;
    WBEM_COMPILE_STATUS_INFO    CompileStatusInfo;
    CComPtr<IMofCompiler>       pCompiler;

    hr = CoCreateInstance( _uuidof(MofCompiler), 
                           0, 
                           CLSCTX_INPROC_SERVER,
                           _uuidof(IMofCompiler),
                           (LPVOID *) &pCompiler);
    if ( FAILED( hr ) )
    {
        //
        // Register the COM object if is not registered.
        //
        WCHAR * pszDllPath = new WCHAR[ MAX_PATH ];
        if ( pszDllPath != NULL )
        {
            if ( GetSystemDirectory( pszDllPath, MAX_PATH ) != 0 )
            {
                HRESULT (STDAPICALLTYPE *pfDllRegisterServer)(VOID);

                (VOID) StringCchCat( pszDllPath, MAX_PATH, L"\\wbem\\mofd.dll" );

                HINSTANCE hLib = LoadLibraryEx( pszDllPath,
                                                NULL,
                                                LOAD_WITH_ALTERED_SEARCH_PATH );
                if ( hLib != NULL )
                {
                    pfDllRegisterServer = (HRESULT (STDAPICALLTYPE *)(VOID))GetProcAddress(hLib, "DllRegisterServer");
                    if ( pfDllRegisterServer != NULL )
                    {
                        pfDllRegisterServer();
                        hr = CoCreateInstance( _uuidof(MofCompiler),
                                               0,
                                               CLSCTX_INPROC_SERVER,
                                               _uuidof(IMofCompiler),
                                               (LPVOID *) &pCompiler);
                    }
                    FreeLibrary( hLib );
                }
            }
            delete [] pszDllPath;
        }
        if ( FAILED( hr ) )
        {
            goto Finished;
        }
    }

    hr = pCompiler->CompileFile( pszFileName,
                                 NULL, // namespace
                                 NULL, // username
                                 NULL, // authoroty
                                 NULL, // password
                                 0,    // option flags
                                 0,    // class flags
                                 0,    // instance
                                 &CompileStatusInfo );
    if ( hr != S_OK )
    {
        //
        // Means failure.
        //
        goto Finished;
    }

Finished:

    return hr;
}

