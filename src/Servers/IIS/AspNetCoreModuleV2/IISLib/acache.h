// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "percpu.h"

class ALLOC_CACHE_HANDLER
{
public:

    ALLOC_CACHE_HANDLER(
    );

    ~ALLOC_CACHE_HANDLER(
    );

    HRESULT
    Initialize(
        DWORD       cbSize,
        LONG        nThreshold
    );

    LPVOID
    Alloc(
    );

    VOID
    Free(
        __in LPVOID pMemory
    );


private:

    VOID
    CleanupLookaside(
    );

    DWORD
    QueryDepthForAllSLists(
    );

    LONG                    m_nThreshold;
    DWORD                   m_cbSize;

    PER_CPU<SLIST_HEADER> * m_pFreeLists;

    //
    // Total heap allocations done over the lifetime.
    // Note that this is not interlocked, it is just a hint for debugging.
    //
    volatile LONG           m_nTotal;

    LONG                    m_nFillPattern;

public:

    static
    HRESULT
    StaticInitialize(
    );
    
    static
    VOID
    StaticTerminate(
    );

    static
    BOOL
    IsPageheapEnabled();

private:

    static LONG             sm_nFillPattern;
    static HANDLE           sm_hHeap;
};


// You can use ALLOC_CACHE_HANDLER as a per-class allocator
// in your C++ classes.  Add the following to your class definition:
//
//  protected:
//      static ALLOC_CACHE_HANDLER* sm_palloc;
//  public:
//      static void*  operator new(size_t s)
//      {
//        IRTLASSERT(s == sizeof(C));
//        IRTLASSERT(sm_palloc != NULL);
//        return sm_palloc->Alloc();
//      }
//      static void   operator delete(void* pv)
//      {
//        IRTLASSERT(pv != NULL);
//        if (sm_palloc != NULL)
//            sm_palloc->Free(pv);
//      }
//
// Obviously, you must initialize sm_palloc before you can allocate
// any objects of this class.
//
// Note that if you derive a class from this base class, the derived class
// must also provide its own operator new and operator delete.  If not, the
// base class's allocator will be called, but the size of the derived
// object will almost certainly be larger than that of the base object.
// Furthermore, the allocator will not be used for arrays of objects
// (override operator new[] and operator delete[]), but this is a
// harder problem since the allocator works with one fixed size.
