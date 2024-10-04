// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

//
// *_HEADER_HASH maps strings to UlHeader* values
//

#define UNKNOWN_INDEX           (0xFFFFFFFF)

struct HEADER_RECORD
{
    PCSTR   _pszName;
    ULONG   _ulHeaderIndex;
};

class RESPONSE_HEADER_HASH: public HASH_TABLE<HEADER_RECORD, PCSTR>
{
public:
    RESPONSE_HEADER_HASH() 
    {}
    
    VOID
    ReferenceRecord(
        HEADER_RECORD *
    )
    {}

    VOID
    DereferenceRecord(
        HEADER_RECORD *
    )
    {}

    PCSTR
    ExtractKey(
        HEADER_RECORD * pRecord
    )
    {
        return pRecord->_pszName;
    }

    DWORD
    CalcKeyHash(
        PCSTR   key
    )
    {
        return HashStringNoCase(key);
    }

    BOOL
    EqualKeys(
        PCSTR   key1,
        PCSTR   key2
    )
    {
        return (_stricmp(key1, key2) == 0);
    }

    HRESULT
    Initialize(
        VOID
    );
    
    VOID
    Terminate(
        VOID
    );
    
    DWORD
    GetIndex(
        PCSTR             pszName
    )
    {
        HEADER_RECORD* pRecord = nullptr;

        FindKey(pszName, &pRecord);
        if (pRecord != nullptr)
        {
            return pRecord->_ulHeaderIndex;
        }

        return UNKNOWN_INDEX;
    }
    
    static
    PCSTR
    GetString(
        ULONG               ulIndex
    )
    {
        if (ulIndex < HttpHeaderResponseMaximum)
        {
            DBG_ASSERT(sm_rgHeaders[ulIndex]._ulHeaderIndex == ulIndex);
            return sm_rgHeaders[ulIndex]._pszName;
        }

        return nullptr;
    }
    
private:

    static HEADER_RECORD         sm_rgHeaders[];

    RESPONSE_HEADER_HASH(const RESPONSE_HEADER_HASH &);
    void operator=(const RESPONSE_HEADER_HASH &);
};
