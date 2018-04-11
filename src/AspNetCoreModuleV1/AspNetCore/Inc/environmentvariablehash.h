// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

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
    {}

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

    static
    VOID
    CopyToMultiSz(
        ENVIRONMENT_VAR_ENTRY *   pEntry,
        PVOID                     pvData
    )
    {
        STRU     strTemp;
        MULTISZ   *pMultiSz = static_cast<MULTISZ *>(pvData);
        DBG_ASSERT(pMultiSz);
        DBG_ASSERT(pEntry);
        strTemp.Copy(pEntry->QueryName());
        strTemp.Append(pEntry->QueryValue());
        pMultiSz->Append(strTemp.QueryStr());
    }

    static
    VOID
    CopyToTable(
        ENVIRONMENT_VAR_ENTRY *   pEntry,
        PVOID                     pvData
    )
    {
        // best effort copy, ignore the failure
        ENVIRONMENT_VAR_ENTRY *   pNewEntry = new ENVIRONMENT_VAR_ENTRY();
        if (pNewEntry != NULL)
        {
            pNewEntry->Initialize(pEntry->QueryName(), pEntry->QueryValue());
            ENVIRONMENT_VAR_HASH *pHash = static_cast<ENVIRONMENT_VAR_HASH *>(pvData);
            DBG_ASSERT(pHash);
            pHash->InsertRecord(pNewEntry);
            // Need to dereference as InsertRecord references it now
            pNewEntry->Dereference();
        }
    }

private:
    ENVIRONMENT_VAR_HASH(const ENVIRONMENT_VAR_HASH &);
    void operator=(const ENVIRONMENT_VAR_HASH &);
};
