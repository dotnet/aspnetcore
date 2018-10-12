// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.h"

#ifndef NT_SUCCESS
#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)
#endif

#ifndef STATUS_SOME_NOT_MAPPED
//
// MessageId: STATUS_SOME_NOT_MAPPED
//
// MessageText:
//
// Some of the information to be translated has not been translated.
//
#define STATUS_SOME_NOT_MAPPED           ((NTSTATUS)0x00000107L)
#endif

#ifndef STATUS_NONE_MAPPED
#define STATUS_NONE_MAPPED               ((NTSTATUS)0xc0000073L)
#endif


HRESULT
IsVistaOrGreater(
    __out BOOL & fIsVistaOrGreater
)
/*++

Routine Description:

    Return TRUE if we are running in a Server SKU,
    otherwise return FALSE.

Arguments:

    pfIsServer - The return value.

Return Value:

    BOOL

--*/
{
    HRESULT         hr               = S_OK;
    OSVERSIONINFOEX osVersionInfoEx  = { 0 };
    DWORDLONG       dwlConditionMask = 0;
    BOOL            fReturn          = FALSE;

    fIsVistaOrGreater = FALSE;
    osVersionInfoEx.dwOSVersionInfoSize = sizeof( osVersionInfoEx );
    osVersionInfoEx.dwMajorVersion = 6;
  
    VER_SET_CONDITION( dwlConditionMask, VER_MAJORVERSION, VER_GREATER_EQUAL );

    fReturn = VerifyVersionInfo(
        &osVersionInfoEx, 
        VER_MAJORVERSION,
        dwlConditionMask );

    //
    // If the function fails, the return value is zero 
    // and GetLastError returns an error code other than ERROR_OLD_WIN_VERSION
    //
    if ( fReturn == FALSE && GetLastError() != ERROR_OLD_WIN_VERSION )
    {
        hr = HRESULT_FROM_WIN32 ( GetLastError() );
        DBGERROR_HR( hr );
        goto Finished;
    }

    fIsVistaOrGreater = ( fReturn );

Finished:

    return hr;

}

HRESULT
CreateDirectory(
    __in LPCWSTR pszFileName
)
{
    HRESULT hr = S_OK;
    DWORD   dwFileAttributes = 0;

    dwFileAttributes = GetFileAttributes( pszFileName );
    if ( dwFileAttributes == INVALID_FILE_ATTRIBUTES )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );

        if ( hr == HRESULT_FROM_WIN32( ERROR_FILE_NOT_FOUND ) ||
             hr == HRESULT_FROM_WIN32( ERROR_PATH_NOT_FOUND ) )
        {
            hr = S_OK;

            //
            // Create the folder.
            //
            if ( !CreateDirectory( pszFileName, NULL ) )
            {
                hr = HRESULT_FROM_WIN32( GetLastError() );
                DBGERROR_HR( hr );
                goto Finished;
            }
        }
        else
        {
            DBGERROR_HR( hr );
        }
    }

Finished:

    return hr;
}

HRESULT
AddExplicitAccessToFileDacl(
    __in LPWSTR                 pszFilePath,
    DWORD                       cExplicitAccess,
    EXPLICIT_ACCESS             rgExplicitAccess[]
)
/*++

Routine Description:

    Add EXPLICIT_ACCESS entries to a file DACL

Arguments:

    pszFilePath - The path to the file where the DACL will be modified
    cExplicitAccess - The count of EXPLICIT_ACCESS entires to add
    rgExplicitAccess - The EXPLICIT_ACCESS entries

Return Value:

    HRESULT

--*/
{
    HRESULT                     hr                          = S_OK;
    DWORD                       dwError                     = ERROR_SUCCESS;
    PSECURITY_DESCRIPTOR        pSecurityDescriptor         = NULL;
    PACL                        pOldFileDacl                = NULL;
    PACL                        pNewFileDacl                = NULL;

    if ( pszFilePath == NULL ||
         cExplicitAccess == 0 ||
         rgExplicitAccess == NULL )
    {
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
        goto Finished;
    }

    //
    // Get the current file DACL
    //
    dwError = GetNamedSecurityInfo( pszFilePath,
                                    SE_FILE_OBJECT,
                                    DACL_SECURITY_INFORMATION,
                                    NULL,                       // ppsidOwner
                                    NULL,                       // ppsidGroup
                                    &pOldFileDacl,
                                    NULL,                       // ppSacl
                                    &pSecurityDescriptor );
    if ( dwError != ERROR_SUCCESS )
    {
        hr = HRESULT_FROM_WIN32( dwError );
        goto Finished;
    }

    //
    // Create a new DACL with the EXPLICIT_ACCESS entries
    //

    dwError = SetEntriesInAcl( cExplicitAccess,
                               rgExplicitAccess,
                               pOldFileDacl,
                               &pNewFileDacl );
    if ( dwError != ERROR_SUCCESS )
    {
        hr = HRESULT_FROM_WIN32( dwError );
        goto Finished;
    }

    //
    // Write the new DACL
    //

    dwError = SetNamedSecurityInfo( pszFilePath,
                                    SE_FILE_OBJECT,
                                    DACL_SECURITY_INFORMATION,
                                    NULL,                       // psidOwner
                                    NULL,                       // psidGroup
                                    pNewFileDacl,
                                    NULL );                     // pSacl
    if ( dwError != ERROR_SUCCESS )
    {
        hr = HRESULT_FROM_WIN32( dwError );
        goto Finished;
    }

Finished:

    if ( pNewFileDacl != NULL )
    {
        LocalFree( pNewFileDacl );
        pNewFileDacl = NULL;
    }

    if ( pSecurityDescriptor != NULL )
    {
        LocalFree( pSecurityDescriptor );
        pSecurityDescriptor = NULL;
    }

    return hr;
}

HRESULT
GrantFileAccessToIisIusrs(
    __in LPWSTR                 pszFilePath,
    DWORD                       dwAccessMask,
    DWORD                       dwInheritance
)
/*++

Routine Description:

    This method gives the IIS_IUSRS group access to a specified file 
    path.  In the case that this is executed on a domain controller,
    this method will grant access explicitly to Local Service and
    Network Service instead.

Arguments:

    pszFilePath - The path where access is granted
    dwAccessMask - Desired access to grant
    dwInhertiance - Inheritance for access mask

Return Value:

    HRESULT

--*/
{
    HRESULT                     hr                      = S_OK;
    DWORD                       cExplicitAccess         = 1;
    PSID                        pSidUserAccount         = NULL;
    DWORD                       cbUserAccount           = 0;
    EXPLICIT_ACCESS             rgExplicitAccess[ 1 ];

    if ( pszFilePath == NULL )
    {
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
        goto Finished;
    }

    // Zero out the struct
    ZeroMemory( &(rgExplicitAccess[0]), sizeof( EXPLICIT_ACCESS ) );

    // Get the SID for IIS_IUSRS
    if ( CreateWellKnownSid( WinBuiltinIUsersSid,
                             NULL,                      // DomainSid
                             pSidUserAccount,
                             &cbUserAccount ) )
    {
        //
        // We are expecing a FALSE return value since we
        // are obtaining required buffer sizes.
        //
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_DATA );
        goto Finished;
    }

    hr = HRESULT_FROM_WIN32( GetLastError() );
    if ( hr != HRESULT_FROM_WIN32( ERROR_INSUFFICIENT_BUFFER ) )
    {
        goto Finished;
    }
    hr = S_OK;

    pSidUserAccount = (PSID) LocalAlloc( LPTR, cbUserAccount );
    if ( pSidUserAccount == NULL )
    {
        hr = HRESULT_FROM_WIN32( ERROR_NOT_ENOUGH_MEMORY );
        goto Finished;
    }

    if ( !CreateWellKnownSid( WinBuiltinIUsersSid,
                              NULL,                     // DomainSid
                              pSidUserAccount,
                              &cbUserAccount ) )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        goto Finished;
    }

    // Build TRUSTEE with the IIS_IUSRS SID
    BuildTrusteeWithSid( &(rgExplicitAccess[0].Trustee), pSidUserAccount );
    rgExplicitAccess[0].grfAccessPermissions = dwAccessMask;
    rgExplicitAccess[0].grfAccessMode = GRANT_ACCESS;
    rgExplicitAccess[0].grfInheritance = dwInheritance;

    // Add access to file DACL
    hr = AddExplicitAccessToFileDacl( pszFilePath,
                                      cExplicitAccess,
                                      rgExplicitAccess );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

Finished:

    return hr;
}

HRESULT
GetStringSddlFromFile(
    __in LPWSTR     pszFileName,
    __out STRU &    strFileSddl,
    __in BOOL       fCreateIfDoesNotExist
)
/*++

Routine Description:

   Returns the DACL in the format SDDL for the 
   specified file or directory.
   
Arguments:

    pszFileName - The file or directory path from to get the DACL.
    strFileSddl - The file's security descriptor as string.

Return Value:

    HRESULT

--*/
{
    HRESULT                 hr                  = S_OK;
    DWORD                   dwResult            = ERROR_SUCCESS;
    PACL                    pFileObjectAcl      = NULL;
    PSECURITY_DESCRIPTOR    pSecurityDescriptor = NULL;
    LPWSTR                  pszSddl             = NULL;
    ULONG                   cchSddlLen          = 0;

    if ( fCreateIfDoesNotExist )
    {
        hr = CreateDirectory( pszFileName );
        if ( FAILED( hr ) )
        {
            DBGERROR_HR( hr );
            goto Finished;
        }
    }

    dwResult = GetNamedSecurityInfo(
                pszFileName,
                SE_FILE_OBJECT,
                DACL_SECURITY_INFORMATION,
                NULL,
                NULL,
                &pFileObjectAcl,
                NULL,
                &pSecurityDescriptor );
    if ( dwResult != ERROR_SUCCESS )
    {
        hr = HRESULT_FROM_WIN32( dwResult );
        DBGERROR_HR( hr );
        goto Finished;
    }  

    if ( ! ConvertSecurityDescriptorToStringSecurityDescriptor(
                pSecurityDescriptor,
                SDDL_REVISION_1,
                DACL_SECURITY_INFORMATION,
                &pszSddl,
                &cchSddlLen ) )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR( hr );
        goto Finished;
    }
   
    hr = strFileSddl.Copy( pszSddl );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

Finished:

    if ( pSecurityDescriptor != NULL )
    {
        LocalFree( pSecurityDescriptor );
        pSecurityDescriptor = NULL;
    }

    if ( pszSddl != NULL )
    {
        LocalFree( pszSddl );
        pszSddl = NULL;
    }

    return hr;
}

VOID
FreeStringSids(
    __in DWORD                      cStringSids,
    __in_bcount(cStringSids) LPWSTR rgszStringSids[]
)
/*++

Routine Description:

   Frees the memory allocated by ConvertAccountNamesToStringSids.
   
Arguments:

    cStringSids   - The number of string in the array rgszStringSids.
    rgszStringSids - An array of SIDs represented as string.

Return Value:

    HRESULT

--*/
{
    if ( rgszStringSids == NULL )
    {
        return;
    }
    
    for ( DWORD dwIndex = 0; dwIndex < cStringSids; dwIndex ++ )
    {
        LocalFree( rgszStringSids [ dwIndex ] );
    }

    LocalFree ( rgszStringSids );
}

HRESULT
ConvertAccountNamesToStringSids (
    __in DWORD                                  dwNameCount,
    __in_ecount(dwNameCount) LPWSTR             rgszNames[],
    __deref_out_ecount(*pcStringSids) LPWSTR**  StringSids,
    __out DWORD*                                pcStringSids 
)
/*++

Routine Description:

    Converts a list of local users or groups to string SIDs.
    The StringSids array must be freed via the FreeStringSids function

Arguments:

    dwNameCount  - Number of names to convert.

    rgszNames    - Array of pointers to Domain\Member strings

    StringSids   - Returns a pointer to an array of pointers to String SIDs.
                   The array should be freed via FreeStringSids.

    pcStringSids - The number of String SIDs translated.

Return Value:

    HRESULT

--*/
{
    HRESULT                     hr                      = S_OK;
    NTSTATUS                    ntStatus                = ERROR_SUCCESS;
    LSA_HANDLE                  hPolicy                 = NULL;
    LSA_OBJECT_ATTRIBUTES       ObjectAttributes        = {0};
    PUNICODE_STRING             pUnicodeNames           = NULL;
    PLSA_REFERENCED_DOMAIN_LIST pReferencedDomainList   = NULL;
    PLSA_TRANSLATED_SID2        pTranslatedSid          = NULL;
    SIZE_T                      cchLength               = 0;
    LPWSTR *                    rgszStringSids          = NULL;
    DWORD                       cStringSids             = 0;
    DWORD                       dwStringIndex           = 0;

    HMODULE                     hModule = NULL;

    typedef NTSTATUS
    (NTAPI * PFN_LSALOOKUPNAMES2) (
        __in LSA_HANDLE PolicyHandle,
        __in ULONG Flags, // Reserved
        __in ULONG Count,
        __in PLSA_UNICODE_STRING Names,
        __out PLSA_REFERENCED_DOMAIN_LIST *ReferencedDomains,
        __out PLSA_TRANSLATED_SID2 *Sids
    );

    PFN_LSALOOKUPNAMES2 pfn = NULL;

    hModule = LoadLibrary( L"Advapi32.dll" );
    if ( hModule == NULL )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR( hr );
        goto Finished;
    }
    
    pfn = ( PFN_LSALOOKUPNAMES2 ) GetProcAddress( hModule, "LsaLookupNames2" );
    if ( pfn == NULL )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR( hr );
        if ( hr == HRESULT_FROM_WIN32( ERROR_PROC_NOT_FOUND ) )
        {
            //
            // This must be an OS before Windows 2003.
            //
            hr = S_OK;
        }    
        goto Finished;
    }
    
    //
    // Open the local LSA database
    //
    ntStatus = LsaOpenPolicy( NULL,             // Open the local policy
                              &ObjectAttributes,
                              POLICY_LOOKUP_NAMES,
                              &hPolicy ) ;

    if ( !NT_SUCCESS( ntStatus ) ) 
    {
        hr = HRESULT_FROM_NT( ntStatus );
        DBGERROR_HR( hr );
        goto Finished;
    }
    
    //
    // Convert the names to unicode strings
    //
    pUnicodeNames = (PUNICODE_STRING) LocalAlloc(
                           LMEM_FIXED,
                           sizeof(UNICODE_STRING) * dwNameCount );

    if ( pUnicodeNames == NULL ) 
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto Finished;
    }

    for ( DWORD dwIndex = 0; dwIndex < dwNameCount; dwIndex++ ) 
    {
        cchLength = wcslen( rgszNames[dwIndex] ) * sizeof( *rgszNames[dwIndex] );
        if ( ( cchLength + sizeof( UNICODE_NULL ) ) > USHRT_MAX )
        {
            hr = HRESULT_FROM_WIN32 ( ERROR_ARITHMETIC_OVERFLOW );
            goto Finished;
        }

        pUnicodeNames[dwIndex].Buffer = rgszNames[dwIndex];         
        pUnicodeNames[dwIndex].Length = ( USHORT ) cchLength;
        pUnicodeNames[dwIndex].MaximumLength = ( USHORT ) ( cchLength + sizeof( UNICODE_NULL ) );
    }

    //
    // Convert the names to sids
    //
    ntStatus = (*pfn) ( hPolicy,
                        0, // Flags
                        dwNameCount,
                        pUnicodeNames,
                        &pReferencedDomainList,
                        &pTranslatedSid );

    if ( !NT_SUCCESS( ntStatus ) ) 
    {
        hr = HRESULT_FROM_WIN32( ntStatus );
        DBGERROR_HR( hr );
        goto Finished;
    }

    //
    // Some of the names could not be translated.
    // This is an informational-level return value.
    //
    if ( ntStatus == STATUS_SOME_NOT_MAPPED ) 
    {
        hr = HRESULT_FROM_WIN32( ntStatus );
        DBGERROR_HR( hr );
        hr = S_OK;
    }

    //
    // Count the number of SIDs retrieved
    //
    for ( DWORD dwIndex = 0; dwIndex < dwNameCount; dwIndex++ )
    {
        if ( pTranslatedSid[dwIndex].Sid != NULL )
        {
            cStringSids ++;
        }
    }
    
    //
    // Allocate the SID list to return
    //
    rgszStringSids = (LPWSTR *) LocalAlloc(
                           LMEM_FIXED,
                           sizeof( LPWSTR ) * cStringSids );

    if ( rgszStringSids == NULL )
    {
        hr = E_OUTOFMEMORY;
        DBGERROR_HR( hr );
        goto Finished;
    }

    //
    // Construct a string SID for each name
    //
    dwStringIndex = 0;
    for ( DWORD dwIndex = 0; dwIndex < dwNameCount; dwIndex++ ) 
    {
        if ( pTranslatedSid[dwIndex].Sid != NULL )
        {
            if ( ! ConvertSidToStringSid(
                pTranslatedSid  [dwIndex].Sid,
                &rgszStringSids [dwStringIndex] ) )
            {
                hr = HRESULT_FROM_WIN32( GetLastError() );
                DBGERROR_HR( hr );
                goto Finished;
            }
            dwStringIndex ++;
        }
    }

Finished:

    if ( pUnicodeNames != NULL )
    {
        LocalFree( pUnicodeNames );
        pUnicodeNames = NULL;
    }

    if ( pReferencedDomainList != NULL )
    {
        LsaFreeMemory( pReferencedDomainList );
        pReferencedDomainList = NULL;
    }

    if ( pTranslatedSid != NULL )
    {
        LsaFreeMemory( pTranslatedSid );
        pTranslatedSid = NULL;
    }

    if ( hPolicy != NULL )
    {
        LsaClose( hPolicy );
        hPolicy = NULL;
    }

    //
    // If the translation wasn't successful,
    // free any partial translation.
    //

    if ( FAILED( hr ) )
    {
        if ( rgszStringSids != NULL ) 
        {
            FreeStringSids( cStringSids, rgszStringSids );
            rgszStringSids = NULL;
        }
    }

    //
    // Assign the output parameters
    //
    (*StringSids)   = rgszStringSids;
    (*pcStringSids) = cStringSids;

    if ( hModule != NULL )
    {
        FreeLibrary( hModule );
        hModule = NULL;
    }

    return  hr;
}

HRESULT
SetFileSddl(
    __in LPCWSTR pszSddl,
    __in LPWSTR pszPath
)
{
    HRESULT hr = S_OK;
    SECURITY_ATTRIBUTES sa = { 0 };
    DWORD status = 0;
    BOOL fIsVistaOrGreater = FALSE;

    sa.nLength = sizeof( sa );
    sa.bInheritHandle = FALSE;
    if ( ! ConvertStringSecurityDescriptorToSecurityDescriptor(
            pszSddl,
            SDDL_REVISION_1,
            &sa.lpSecurityDescriptor,
            NULL))
    {
        hr = HRESULT_FROM_WIN32 ( GetLastError() );
        DBGERROR_HR( hr );
        goto Finished;
    }
    
    hr = IsVistaOrGreater ( fIsVistaOrGreater );
    if ( FAILED(hr) )
    {
        fIsVistaOrGreater = FALSE;
        hr = S_OK;
    }

    if ( fIsVistaOrGreater )
    {
        status = SetNamedSecurityInfo( pszPath,
                                       SE_FILE_OBJECT,
                                       DACL_SECURITY_INFORMATION,
                                       NULL,
                                       NULL,
                                       (PACL) sa.lpSecurityDescriptor,
                                       NULL );
        if ( status != ERROR_SUCCESS )
        {
            hr = HRESULT_FROM_WIN32 (status  );
            DBGERROR_HR( hr );
            goto Finished;
        }
    }
    else
    {
        if ( ! SetFileSecurity( pszPath,
                                DACL_SECURITY_INFORMATION,
                               (PSECURITY_DESCRIPTOR) sa.lpSecurityDescriptor ) )
        {
            hr = HRESULT_FROM_WIN32( GetLastError() );
            DBGERROR_HR( hr );
            goto Finished;
        }
    }
   

Finished:

    if ( sa.lpSecurityDescriptor != NULL )
    {
        LocalFree( sa.lpSecurityDescriptor );
        sa.lpSecurityDescriptor = NULL;
    }

    return hr;
}

HRESULT
GetIISWPGSid(
    __out STRU & strIISWPGSid
)
{
    HRESULT hr = S_OK;
    LPWSTR*     ppszStringSids  = NULL;
    DWORD       cStringSids     = 0;

    LPWSTR rgszIisAccountsToResolve [] = { L"IIS_WPG" };

    hr = ConvertAccountNamesToStringSids( _countof( rgszIisAccountsToResolve ),
                                          rgszIisAccountsToResolve,
                                          &ppszStringSids,
                                          &cStringSids );
    if ( FAILED( hr ) )
    {       
        DBGERROR_HR( hr );
        goto Finished;
    }

    if ( cStringSids < 1 )
    {
        hr = E_INVALIDARG;
        goto Finished;
    }

    hr = strIISWPGSid.Copy( ppszStringSids[0] );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

Finished:

    if ( ppszStringSids != NULL ) 
    {
        FreeStringSids( cStringSids, ppszStringSids );
        ppszStringSids = NULL;
    }

    return hr;
}

ULONG
GetRealAclSize(
    __in PACL Acl
    )
/*++

Routine Description:

    Walks the sids in an ACL to determine it's minimum size

Arguments:

    Acl - Acl to scan
    
Return Value:

    Byte count

--*/
{
    PACE_HEADER Ace = FirstAce( Acl );
    int i = 0;
    while (i < Acl->AceCount ) {
        i++;
        Ace = NextAce( Ace );
    }

    return (ULONG) ((PBYTE) Ace - (PBYTE) Acl);

}

HRESULT
MakeAutoInheritFromParent(
    __in LPWSTR  pszPath
)
{
    HRESULT              hr = S_OK;
    DWORD                dwError;
    PACL                 Acl;
    PACE_HEADER          Ace;
    PSECURITY_DESCRIPTOR pSecurityDescriptor;
    SECURITY_INFORMATION SecurityInfo = DACL_SECURITY_INFORMATION;	

    //
    // Get the current file DACL
    //
    dwError = GetNamedSecurityInfo( pszPath,
                          SE_FILE_OBJECT,
                          SecurityInfo,
                          NULL,
                          NULL,
                          &Acl,
                          NULL,
                          &pSecurityDescriptor );
    if ( dwError != ERROR_SUCCESS )
    {
        hr = HRESULT_FROM_WIN32( dwError );
        DBGERROR_HR( hr );
        goto Finished;
    }

    //
    // Remove all the ACEs
    //      
    Ace = FirstAce( Acl );
    int i = 0;
    while ( i < Acl->AceCount )
    {
        //
        //  The Ace was not inherited
        //
        if( (Ace->AceFlags & INHERITED_ACE) == 0 )
        {
            if (!DeleteAce( Acl, i ))
            {
                hr = HRESULT_FROM_WIN32( GetLastError() );
                DBGERROR_HR( hr );
                goto Finished;
            }
        }
        else
        {
            i++;
            Ace = NextAce( Ace );
        }
    }
    Acl->AclSize = (WORD) GetRealAclSize( Acl );

    //
    // Auto-inherit
    //
    SecurityInfo |= UNPROTECTED_DACL_SECURITY_INFORMATION;

    dwError = SetNamedSecurityInfo( pszPath,
        SE_FILE_OBJECT,
        SecurityInfo,
        NULL,
        NULL,
        Acl,
        NULL );
    if ( dwError != ERROR_SUCCESS )
    {
        hr = HRESULT_FROM_WIN32( dwError );
        DBGERROR_HR( hr );
        goto Finished;
    }

Finished:

    if ( pSecurityDescriptor != NULL )
    {
        LocalFree( pSecurityDescriptor );
        pSecurityDescriptor = NULL;
    }

    return hr;
}

HRESULT
GrantIISWPGReadWritePermissions(
    __in LPWSTR pszPath
)
{
    HRESULT     hr              = S_OK;
    STACK_STRU( strAces,        128 );
    STACK_STRU( strFileSddl,    128 );
    STACK_STRU( strFileSddlTemp, 128 );
    STACK_STRU( strIISWPGSid,   32 );

    hr = GetStringSddlFromFile( pszPath,
                                strFileSddlTemp );
    if ( FAILED( hr ) )
    {
        DBGERROR_HR( hr );
        goto Finished;
    }

    hr = GetIISWPGSid( strIISWPGSid );
    if ( FAILED( hr ) )
    {
        DBGERROR_HR( hr );
        if ( hr == STATUS_NONE_MAPPED )
        {
            //
            // No maps found.
            //
            hr = S_OK;
        }
        goto Finished;
    }
   
    //
    // Add Read/Write permissions.
    //
    hr = strAces.Copy( L"(A;OICI;0x12019f;;;" );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = strAces.Append( strIISWPGSid );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

    hr = strAces.Append( L")", 1 );
    if ( FAILED( hr ) )
    {
        goto Finished;
    }

    //
    // Insert the ACE before any other ACE.
    //
    
    //
    // Search for the first ACE '('
    //
    LPCWSTR psz = wcschr( strFileSddlTemp.QueryStr(), SDDL_ACE_BEGINC );

    //
    // Copy the first part before the first '('
    //
    hr = strFileSddl.Append( strFileSddlTemp.QueryStr(),
                             (DWORD)(psz - strFileSddlTemp.QueryStr()) );
    if ( FAILED( hr ) )
    {
        DBGERROR_HR( hr );
        goto Finished;
    }

    //
    // Copy the new ACE
    //
    hr = strFileSddl.Append( strAces );
    if ( FAILED( hr ) )
    {
        DBGERROR_HR( hr );
        goto Finished;
    }

    //
    // Copy the other ACEs.
    //
    hr = strFileSddl.Append( psz );
    if ( FAILED( hr ) )
    {
        DBGERROR_HR( hr );
        goto Finished;
    }

    hr = SetFileSddl( strFileSddl.QueryStr(),
                      pszPath );
    if ( FAILED( hr ) )
    {
        DBGERROR_HR( hr );
        goto Finished;
    }

Finished:     

    return hr;
}

HRESULT
SetupAclsWow64(
    LPCWSTR pwszPath
)
{
    HRESULT hr = S_OK;
    HMODULE hModule = NULL;
    STACK_STRU( strPath, MAX_PATH );

    typedef UINT  
    (WINAPI *PFN_GETSYSTEMWOW64DIRECTORY)
    (
        __out  LPTSTR lpBuffer,
        __in   UINT uSize
    );

    PFN_GETSYSTEMWOW64DIRECTORY pfn;

    hModule = LoadLibrary( L"kernel32.dll" );
    if ( hModule == NULL )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR( hr );
        goto Finished;
    }
    pfn = ( PFN_GETSYSTEMWOW64DIRECTORY ) GetProcAddress( hModule, "GetSystemWow64DirectoryW" );
    if ( pfn == NULL )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        DBGERROR_HR( hr );
        if ( hr == HRESULT_FROM_WIN32( ERROR_PROC_NOT_FOUND ) )
        {
            //
            // This must be an OS before Windows 2003.
            //
            hr = S_OK;
        }    
        goto Finished;
    }

    if ( ! (*pfn)( strPath.QueryStr(),
                   strPath.QuerySizeCCH() ) )
    {
        hr = HRESULT_FROM_WIN32( GetLastError() );
        if ( hr == HRESULT_FROM_WIN32( ERROR_CALL_NOT_IMPLEMENTED ) )
        {
            //
            // This is not Win64.
            //
            hr = S_OK;
        }
        else
        {
            DBGERROR_HR( hr );
        }
        goto Finished;
    }
    strPath.SyncWithBuffer();

    if (pwszPath != NULL)
    {
        hr = strPath.Append( pwszPath );
        if ( FAILED( hr ) )
        {
            goto Finished;
        }
    }

    hr = MakeAutoInheritFromParent( strPath.QueryStr() );
    if ( FAILED( hr ) )
    {
        DBGERROR_HR( hr );
        goto Finished;
    }
   
Finished:

    if ( hModule != NULL )
    {
        FreeLibrary( hModule );
        hModule = NULL;
    }

    return hr;
}
