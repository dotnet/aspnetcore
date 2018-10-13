// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

class MY_OBJ
{
 public:
     MY_OBJ(PCWSTR pstr)
       : _pstr(pstr)
     {}

     PCWSTR GetString()
     {
         return _pstr;
     }

  private:
     PCWSTR _pstr;
};

class MY_HASH : public HASH_TABLE<MY_OBJ,PCWSTR>
{
 public:
    VOID
    ReferenceRecord(
        MY_OBJ *   //pRecord
    )
    {}

    VOID
    DereferenceRecord(
        MY_OBJ *   //pRecord
    )
    {}

    PCWSTR
    ExtractKey(
        MY_OBJ *   pRecord
    )
    {
        return pRecord->GetString();
    }

    DWORD
    CalcKeyHash(
        PCWSTR        key
    )
    {
        return HashString(key);
    }

    BOOL
    EqualKeys(
        PCWSTR        key1,
        PCWSTR        key2
    )
    {
        return (wcscmp(key1, key2) == 0);
    }
};

void TestHash();