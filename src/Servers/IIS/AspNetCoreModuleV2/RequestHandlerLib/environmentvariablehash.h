// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#define HOSTING_STARTUP_ASSEMBLIES_ENV_STR          L"ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"
#define HOSTING_STARTUP_ASSEMBLIES_VALUE            L"Microsoft.AspNetCore.Server.IISIntegration"
#define ASPNETCORE_IIS_AUTH_ENV_STR                 L"ASPNETCORE_IIS_HTTPAUTH"
#define ASPNETCORE_IIS_WEBSOCKETS_SUPPORTED_ENV_STR L"ASPNETCORE_IIS_WEBSOCKETS_SUPPORTED"
#define ASPNETCORE_IIS_PHYSICAL_PATH_ENV_STR        L"ASPNETCORE_IIS_PHYSICAL_PATH"
#define ASPNETCORE_ANCM_HTTPS_PORT_ENV_STR          L"ASPNETCORE_ANCM_HTTPS_PORT"
#define ASPNETCORE_IIS_AUTH_WINDOWS                 L"windows;"
#define ASPNETCORE_IIS_AUTH_BASIC                   L"basic;"
#define ASPNETCORE_IIS_AUTH_ANONYMOUS               L"anonymous;"
#define ASPNETCORE_IIS_AUTH_NONE                    L"none"
#define ANCM_PREFER_ENVIRONMENT_VARIABLES_ENV_STR   L"ANCM_PREFER_ENVIRONMENT_VARIABLES"

//
// The key used for hash-table lookups, consists of the port on which the http process is created.
//

class ENVIRONMENT_VAR_ENTRY
{
public:
    ENVIRONMENT_VAR_ENTRY():
        _cRefs(1)
    {
    }

    HRESULT
    Initialize(
        PCWSTR      pszName,
        PCWSTR      pszValue
    )
    {
        HRESULT hr = S_OK;
        if (FAILED(hr = _strName.Copy(pszName)) ||
            FAILED(hr = _strValue.Copy(pszValue)))
        {
        }
        return hr;
    }

    VOID
    Reference() const
    {
        InterlockedIncrement(&_cRefs);
    }

    VOID
    Dereference() const
    {
        if (InterlockedDecrement(&_cRefs) == 0)
        {
            delete this;
        }
    }

    PWSTR  const
    QueryName()
    {
        return _strName.QueryStr();
    }

    PWSTR const
    QueryValue()
    {
        return _strValue.QueryStr();
    }

private:
    ~ENVIRONMENT_VAR_ENTRY()
    {
    }

    STRU                _strName;
    STRU                _strValue;
    mutable LONG        _cRefs;
};


class ENVIRONMENT_VAR_HASH : public HASH_TABLE<ENVIRONMENT_VAR_ENTRY, PWSTR>
{
public:
    ENVIRONMENT_VAR_HASH()
    {
    }

    PWSTR
    ExtractKey(
        ENVIRONMENT_VAR_ENTRY *   pEntry
    )
    {
        return pEntry->QueryName();
    }

    DWORD
    CalcKeyHash(
        PWSTR   pszName
    )
    {
        return HashStringNoCase(pszName);
    }

    BOOL
    EqualKeys(
        PWSTR   pszName1,
        PWSTR   pszName2
    )
    {
        return (_wcsicmp(pszName1, pszName2) == 0);
    }

    VOID
    ReferenceRecord(
        ENVIRONMENT_VAR_ENTRY *   pEntry
    )
    {
        pEntry->Reference();
    }

    VOID
    DereferenceRecord(
        ENVIRONMENT_VAR_ENTRY *   pEntry
    )
    {
        pEntry->Dereference();
    }


private:
    ENVIRONMENT_VAR_HASH(const ENVIRONMENT_VAR_HASH &);
    void operator=(const ENVIRONMENT_VAR_HASH &);
};

struct ENVIRONMENT_VAR_HASH_DELETER
{
    void operator ()(ENVIRONMENT_VAR_HASH* hashTable) const
    {
        hashTable->Clear();
        delete hashTable;
    }
};

struct ENVIRONMENT_VAR_ENTRY_DELETER
{
    void operator ()(ENVIRONMENT_VAR_ENTRY* entry) const
    {
        entry->Dereference();
    }
};
