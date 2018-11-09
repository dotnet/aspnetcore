// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.hxx"
#include "hybrid_array.h"

//
// Cannot support mixed native/managed code for BUFFER class
// because of alignment. We need to run the test as native.
//
#include <mstest.h>


void HybridArrayTest()
{
    HRESULT hr;

    {
        HYBRID_ARRAY<void *, 32> arrPointers;
        Assert::AreEqual<SIZE_T>(32, arrPointers.QueryCapacity(), L"Invalid initial length");
    }

    {
        HYBRID_ARRAY<int, 2> arrIntegers;
        int SourceArray[] = {1, 2, 3, 4};
        hr = arrIntegers.Copy( SourceArray );
        Assert::AreEqual(S_OK, hr, L"Copy failed.");
        Assert::AreEqual(_countof(SourceArray), arrIntegers.QueryCapacity());
    }

    {
        HYBRID_ARRAY<int, 2> arrIntegers;
        int* pOriginal = arrIntegers.QueryArray();
        int SourceArray[] = {1, 2, 3, 4};
        hr = arrIntegers.Copy( SourceArray );
        int* pNew = arrIntegers.QueryArray();
        Assert::AreEqual(S_OK, hr, L"Copy failed.");
        Assert::AreEqual(_countof(SourceArray), arrIntegers.QueryCapacity(), L"Size should be like source");
        Assert::AreNotEqual((__int64)pNew, (__int64)pOriginal, L"Pointer should be different");

        Assert::AreEqual(1, arrIntegers[0], L"Index 0 failed.");
        Assert::AreEqual(2, arrIntegers.QueryItem(1), L"Index 1 failed.");
        Assert::AreEqual(3, arrIntegers.QueryItem(2), L"Index 2 failed.");
        Assert::AreEqual(4, arrIntegers[3], L"Index 3 failed.");
    }

    {
        HYBRID_ARRAY<int, 2> arrIntegers;
        hr = arrIntegers.EnsureCapacity(100, false); 
        Assert::AreEqual(S_OK, hr, L"Copy failed.");
        Assert::AreEqual<SIZE_T>(100, arrIntegers.QueryCapacity());
    }

    {
        HYBRID_ARRAY<int, 2> arrIntegers;
        arrIntegers[0] = 123;
        arrIntegers[1] = 999;
        hr = arrIntegers.EnsureCapacity(100, true /*copy previous*/); 
        Assert::AreEqual(S_OK, hr, L"Copy failed.");
        Assert::AreEqual<SIZE_T>(100, arrIntegers.QueryCapacity());
        Assert::AreEqual(123, arrIntegers[0], L"Index resize 0 failed.");
        Assert::AreEqual(999, arrIntegers[1], L"Index resize 1 failed.");

    }

    {
        HYBRID_ARRAY<int, 2> arrIntegers;
        arrIntegers[0] = 123;
        arrIntegers[1] = 999;
        hr = arrIntegers.EnsureCapacity(100, true /*copy previous*/, true /*trivial assign*/); 
        Assert::AreEqual(S_OK, hr, L"Copy failed.");
        Assert::AreEqual<SIZE_T>(100, arrIntegers.QueryCapacity());
        Assert::AreEqual(123, arrIntegers[0], L"Index resize trivial 0 failed.");
        Assert::AreEqual(999, arrIntegers[1], L"Index resize trivial 1 failed.");

    }
}


#pragma managed

using namespace Microsoft::VisualStudio::TestTools::UnitTesting;

[TestClass]
public ref class ArrayTests
{
public:

    [TestMethod]
    void HybridArrayTest()
    {
        ::HybridArrayTest();
    }
 
};
