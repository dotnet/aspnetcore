// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

HRESULT
IsVistaOrGreater(
    __out BOOL & fIsVistaOrGreater
);

HRESULT
CreateDirectory(
    __in LPCWSTR pszFileName
);

HRESULT
AddExplicitAccessToFileDacl(
    __in LPWSTR                 pszFilePath,
    DWORD                       cExplicitAccess,
    EXPLICIT_ACCESS             rgExplicitAccess[]
);

HRESULT
GrantFileAccessToIisIusrs(
    __in LPWSTR                 pszFilePath,
    DWORD                       dwAccessMask,
    DWORD                       dwInheritance
);

HRESULT
GetStringSddlFromFile(
    __in LPWSTR     pszFileName,
    __out STRU &    strFileSddl,
    __in BOOL       fCreateIfDoesNotExist = TRUE
);

VOID
FreeStringSids(
    __in DWORD                      cStringSids,
    __in_bcount(cStringSids) LPWSTR rgszStringSids[]
);

HRESULT
ConvertAccountNamesToStringSids (
    __in DWORD                                  dwNameCount,
    __in_ecount(dwNameCount) LPWSTR             rgszNames[],
    __deref_out_ecount(*pcStringSids) LPWSTR**  StringSids,
    __out DWORD*                                pcStringSids 
);

HRESULT
SetFileSddl(
    __in LPCWSTR pszSddl,
    __in LPWSTR pszPath
);

HRESULT
GetIISWPGSid(
    __out STRU & strIISWPGSid
);

inline PACE_HEADER
FirstAce( __in PACL Acl )
{
    return (PACE_HEADER)(((PBYTE)Acl) + sizeof( ACL ));
}

inline PACE_HEADER
NextAce( __in PACE_HEADER Ace )
{
    return (PACE_HEADER)(((PBYTE)Ace) + Ace->AceSize );
}

inline PSID
SidFromAce( __in PACE_HEADER Ace )
{
    return (PSID)&((PACCESS_ALLOWED_ACE)Ace)->SidStart;
}

ULONG
GetRealAclSize(
    __in PACL Acl
);


HRESULT
MakeAutoInheritFromParent(
    __in LPWSTR  pszPath
);

HRESULT
GrantIISWPGReadWritePermissions(
    __in LPWSTR pszPath
);

HRESULT
SetupAclsWow64(
    LPCWSTR pwszPath
);

