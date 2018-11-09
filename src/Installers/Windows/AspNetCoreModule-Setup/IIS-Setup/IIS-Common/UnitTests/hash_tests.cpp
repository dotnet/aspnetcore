// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.hxx"
#include "hashtable.h"
#include "hashfn.h"
#include "my_hash.h"

VOID
CountHash(
    MY_OBJ *    , //pRecord,
    PVOID pVoid
)
{
    DWORD * pActualCount = (DWORD*) pVoid;
    ++(*pActualCount);
}


#pragma managed

using namespace Microsoft::VisualStudio::TestTools::UnitTesting;

[TestClass]
public ref class HashTableTests
{
public:

    [TestMethod]
    void AddTwoRecordsTest()
    {
        MY_HASH hash;
        HRESULT hr;
        hr = hash.Initialize(32);

        Assert::AreEqual(S_OK, hr, L"Invalid hash table initialization");

        MY_OBJ one(L"one");
        hr = hash.InsertRecord(&one);
        Assert::AreEqual(S_OK, hr, L"Cannot add element 'one'");

        MY_OBJ two(L"two");
        hr = hash.InsertRecord(&two);
        Assert::AreEqual(S_OK, hr, L"Cannot add element 'two'");

        DWORD ActualCount = 0;
        hash.Apply(CountHash, &ActualCount);
        Assert::AreEqual((DWORD)2, ActualCount, L"ActualCount != 2");

        hash.Clear();
    }
};
