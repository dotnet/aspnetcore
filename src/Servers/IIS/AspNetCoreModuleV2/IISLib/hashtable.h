// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <crtdbg.h>
#include "rwlock.h"
#include "prime.h"

template <class _Record>
class HASH_NODE
{
    template <class _Record, class _Key>
    friend class HASH_TABLE;

    HASH_NODE(
        _Record *       pRecord,
        DWORD           dwHash
    ) : _pNext (NULL),
        _pRecord (pRecord),
        _dwHash (dwHash)
    {}

    ~HASH_NODE()
    {
        _ASSERTE(_pRecord == NULL);
    }

 private:
    // Next node in the hash table look-aside
    HASH_NODE<_Record> *_pNext;

    // actual record
    _Record *           _pRecord;

    // hash value
    DWORD               _dwHash;
};

template <class _Record, class _Key>
class HASH_TABLE
{
protected:
    typedef BOOL
    (PFN_DELETE_IF)(
        _Record *           pRecord,
        PVOID               pvContext
    );

    typedef VOID
    (PFN_APPLY)(
        _Record *           pRecord,
        PVOID               pvContext
    );

public:
    HASH_TABLE(
        VOID
    )
      : _ppBuckets( NULL ),
        _nBuckets( 0 ),
        _nItems( 0 )
    {
    }

    virtual
    ~HASH_TABLE();

    virtual
    VOID
    ReferenceRecord(
        _Record *   pRecord
    ) = 0;

    virtual
    VOID
    DereferenceRecord(
        _Record *   pRecord
    ) = 0;

    virtual
    _Key
    ExtractKey(
        _Record *   pRecord
    ) = 0;

    virtual
    DWORD
    CalcKeyHash(
        _Key        key
    ) = 0;

    virtual
    BOOL
    EqualKeys(
        _Key        key1,
        _Key        key2
    ) = 0;

    DWORD
    Count(
        VOID
    ) const;

    bool
    IsInitialized(
        VOID
    ) const;

    virtual
    VOID
    Clear();

    HRESULT
    Initialize(
        DWORD           nBucketSize
    );

    virtual
    VOID
    FindKey(
        _Key        key,
        _Record **  ppRecord
    );

    virtual
    HRESULT
    InsertRecord(
        _Record *   pRecord
    );

    virtual
    VOID
    DeleteKey(
        _Key        key
    );

    virtual
    VOID
    DeleteIf(
        PFN_DELETE_IF       pfnDeleteIf,
        PVOID               pvContext
    );

    VOID
    Apply(
        PFN_APPLY           pfnApply,
        PVOID               pvContext
    );

private:

    __success(*ppNode != NULL && return != FALSE)
    BOOL
    FindNodeInternal(
        _Key                    key,
        DWORD                   dwHash,
        __deref_out
        HASH_NODE<_Record> **   ppNode,
        __deref_opt_out
        HASH_NODE<_Record> ***  pppPreviousNodeNextPointer = NULL
    );

    VOID
    DeleteNode(
        HASH_NODE<_Record> *    pNode
    )
    {
        if (pNode->_pRecord != NULL)
        {
            DereferenceRecord(pNode->_pRecord);
            pNode->_pRecord = NULL;
        }

        delete pNode;
    }

    VOID
    RehashTableIfNeeded(
        VOID
    );

    HASH_NODE<_Record> **   _ppBuckets;
    DWORD                   _nBuckets;
    DWORD                   _nItems;
    //
    // Allow to use lock object in const methods.
    //
    mutable
    CWSDRWLock              _tableLock;
};

template <class _Record, class _Key>
HRESULT
HASH_TABLE<_Record,_Key>::Initialize(
    DWORD   nBuckets
)
{
    HRESULT hr = S_OK;

    if ( nBuckets == 0 )
    {
        hr = E_INVALIDARG;
        goto Failed;
    }

    if (nBuckets >= MAXDWORD/sizeof(HASH_NODE<_Record> *))
    {
        hr = E_INVALIDARG;
        goto Failed;
    }

    _ASSERTE(_ppBuckets == NULL );
    if ( _ppBuckets != NULL )
    {
        hr = E_INVALIDARG;
        goto Failed;
    }

    hr = _tableLock.Init();
    if ( FAILED( hr ) )
    {
        goto Failed;
    }

    _ppBuckets = (HASH_NODE<_Record> **)HeapAlloc(
                            GetProcessHeap(),
                            HEAP_ZERO_MEMORY,
                            nBuckets*sizeof(HASH_NODE<_Record> *));
    if (_ppBuckets == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
        goto Failed;
    }
    _nBuckets = nBuckets;

    return S_OK;

Failed:

    if (_ppBuckets)
    {
        HeapFree(GetProcessHeap(),
                 0,
                 _ppBuckets);
        _ppBuckets = NULL;
    }

    return hr;
}


template <class _Record, class _Key>
HASH_TABLE<_Record,_Key>::~HASH_TABLE()
{
    if (_ppBuckets == NULL)
    {
        return;
    }

    _ASSERTE(_nItems == 0);

    HeapFree(GetProcessHeap(),
             0,
             _ppBuckets);
    _ppBuckets = NULL;
    _nBuckets = 0;
}

template< class _Record, class _Key>
DWORD
HASH_TABLE<_Record,_Key>::Count() const
{
    return _nItems;
}

template< class _Record, class _Key>
bool
HASH_TABLE<_Record,_Key>::IsInitialized(
    VOID
) const
{
    return _ppBuckets != NULL;
}


template <class _Record, class _Key>
VOID
HASH_TABLE<_Record,_Key>::Clear()
{
    HASH_NODE<_Record> *pCurrent;
    HASH_NODE<_Record> *pNext;

    // This is here in the off cases where someone instantiates a hashtable
    // and then does an automatic "clear" before its destruction WITHOUT
    // ever initializing it.
    if ( ! _tableLock.QueryInited() )
    {
        return;
    }

    _tableLock.ExclusiveAcquire();

    for (DWORD i=0; i<_nBuckets; i++)
    {
        pCurrent = _ppBuckets[i];
        _ppBuckets[i] = NULL;
        while (pCurrent != NULL)
        {
            pNext = pCurrent->_pNext;
            DeleteNode(pCurrent);
            pCurrent = pNext;
        }
    }

    _nItems = 0;
    _tableLock.ExclusiveRelease();
}

template <class _Record, class _Key>
__success(*ppNode != NULL && return != FALSE)
BOOL
HASH_TABLE<_Record,_Key>::FindNodeInternal(
    _Key                    key,
    DWORD                   dwHash,
    __deref_out
    HASH_NODE<_Record> **   ppNode,
    __deref_opt_out
    HASH_NODE<_Record> ***  pppPreviousNodeNextPointer
)
/*++
  Return value indicates whether the item is found
  key, dwHash - key and hash for the node to find
  ppNode - on successful return, the node found, on failed return, the first
  node with hash value greater than the node to be found
  pppPreviousNodeNextPointer - the pointer to previous node's _pNext

  This routine may be called under either read or write lock
--*/
{
    HASH_NODE<_Record> **ppPreviousNodeNextPointer;
    HASH_NODE<_Record> *pNode;
    BOOL fFound = FALSE;

    ppPreviousNodeNextPointer = _ppBuckets + (dwHash % _nBuckets);
    pNode = *ppPreviousNodeNextPointer;
    while (pNode != NULL)
    {
        if (pNode->_dwHash == dwHash)
        {
            if (EqualKeys(key,
                          ExtractKey(pNode->_pRecord)))
            {
                fFound = TRUE;
                break;
            }
        }
        else if (pNode->_dwHash > dwHash)
        {
            break;
        }

        ppPreviousNodeNextPointer = &(pNode->_pNext);
        pNode = *ppPreviousNodeNextPointer;
    }

    __analysis_assume( (pNode == NULL && fFound == FALSE) ||
                       (pNode != NULL && fFound == TRUE ) );
    *ppNode = pNode;
    if (pppPreviousNodeNextPointer != NULL)
    {
        *pppPreviousNodeNextPointer = ppPreviousNodeNextPointer;
    }
    return fFound;
}

template <class _Record, class _Key>
VOID
HASH_TABLE<_Record,_Key>::FindKey(
    _Key                key,
    _Record **          ppRecord
)
{
    HASH_NODE<_Record> *pNode;

    *ppRecord = NULL;

    DWORD dwHash = CalcKeyHash(key);

    _tableLock.SharedAcquire();

    if (FindNodeInternal(key, dwHash, &pNode) &&
        pNode->_pRecord != NULL)
    {
        ReferenceRecord(pNode->_pRecord);
        *ppRecord = pNode->_pRecord;
    }

    _tableLock.SharedRelease();
}

template <class _Record, class _Key>
HRESULT
HASH_TABLE<_Record,_Key>::InsertRecord(
    _Record *           pRecord
)
/*++
  This method inserts a node for this record and also empty nodes for paths
  in the hierarchy leading upto this path

  The insert is done under only a read-lock - this is possible by keeping
  the hashes in a bucket in increasing order and using interlocked operations
  to actually insert the item in the hash-bucket lookaside list and the parent
  children list

  Returns HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS) if the record already exists.
  Never leak this error to the end user because "*file* already exists" may be confusing.
--*/
{
    BOOL fLocked = FALSE;
    _Key key = ExtractKey(pRecord);
    DWORD dwHash = CalcKeyHash(key);
    HRESULT hr = S_OK;
    HASH_NODE<_Record> *    pNewNode;
    HASH_NODE<_Record> *    pNextNode;
    HASH_NODE<_Record> **   ppPreviousNodeNextPointer;

    //
    // Ownership of pRecord is not transferred to pNewNode yet, so remember
    // to either set it to null before deleting pNewNode or add an extra
    // reference later - this is to make sure we do not do an extra ref/deref
    // which users may view as getting flushed out of the hash-table
    //
    pNewNode = new HASH_NODE<_Record>(pRecord, dwHash);
    if (pNewNode == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
        goto Finished;
    }

    _tableLock.SharedAcquire();
    fLocked = TRUE;

    do
    {
        //
        // Find the right place to add this node
        //
        if (FindNodeInternal(key, dwHash, &pNextNode, &ppPreviousNodeNextPointer))
        {
            //
            // If node already there, return error
            //
            pNewNode->_pRecord = NULL;
            DeleteNode(pNewNode);

            //
            // We should never leak this error to the end user
            // because "file already exists" may be confusing.
            //
            hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
            goto Finished;
        }

        //
        // If another node got inserted in between, we will have to retry
        //
        pNewNode->_pNext = pNextNode;
    } while (InterlockedCompareExchangePointer((PVOID *)ppPreviousNodeNextPointer,
                                               pNewNode,
                                               pNextNode) != pNextNode);
    // pass ownership of pRecord now
    if (pRecord != NULL)
    {
        ReferenceRecord(pRecord);
        pRecord = NULL;
    }
    InterlockedIncrement((LONG *)&_nItems);

Finished:

    if (fLocked)
    {
        _tableLock.SharedRelease();
    }

    if (SUCCEEDED(hr))
    {
        RehashTableIfNeeded();
    }

    return hr;
}

template <class _Record, class _Key>
VOID
HASH_TABLE<_Record,_Key>::DeleteKey(
    _Key        key
)
{
    HASH_NODE<_Record> *pNode;
    HASH_NODE<_Record> **ppPreviousNodeNextPointer;

    DWORD dwHash = CalcKeyHash(key);

    _tableLock.ExclusiveAcquire();

    if (FindNodeInternal(key, dwHash, &pNode, &ppPreviousNodeNextPointer))
    {
        *ppPreviousNodeNextPointer = pNode->_pNext;
        DeleteNode(pNode);
        _nItems--;
    }

    _tableLock.ExclusiveRelease();
}

template <class _Record, class _Key>
VOID
HASH_TABLE<_Record,_Key>::DeleteIf(
    PFN_DELETE_IF               pfnDeleteIf,
    PVOID                       pvContext
)
{
    HASH_NODE<_Record> *pNode;
    HASH_NODE<_Record> **ppPreviousNodeNextPointer;

    _tableLock.ExclusiveAcquire();

    for (DWORD i=0; i<_nBuckets; i++)
    {
        ppPreviousNodeNextPointer = _ppBuckets + i;
        pNode = *ppPreviousNodeNextPointer;
        while (pNode != NULL)
        {
            //
            // Non empty nodes deleted based on DeleteIf, empty nodes deleted
            // if they have no children
            //
            if (pfnDeleteIf(pNode->_pRecord, pvContext))
            {
                *ppPreviousNodeNextPointer = pNode->_pNext;
                DeleteNode(pNode);
                _nItems--;
            }
            else
            {
                ppPreviousNodeNextPointer = &pNode->_pNext;
            }

            pNode = *ppPreviousNodeNextPointer;
        }
    }

    _tableLock.ExclusiveRelease();
}

template <class _Record, class _Key>
VOID
HASH_TABLE<_Record,_Key>::Apply(
    PFN_APPLY                   pfnApply,
    PVOID                       pvContext
)
{
    HASH_NODE<_Record> *pNode;

    _tableLock.SharedAcquire();

    for (DWORD i=0; i<_nBuckets; i++)
    {
        pNode = _ppBuckets[i];
        while (pNode != NULL)
        {
            if (pNode->_pRecord != NULL)
            {
                pfnApply(pNode->_pRecord, pvContext);
            }

            pNode = pNode->_pNext;
        }
    }

    _tableLock.SharedRelease();
}

template <class _Record, class _Key>
VOID
HASH_TABLE<_Record,_Key>::RehashTableIfNeeded(
    VOID
)
{
    HASH_NODE<_Record> **ppBuckets;
    DWORD nBuckets;
    HASH_NODE<_Record> *pNode;
    HASH_NODE<_Record> *pNextNode;
    HASH_NODE<_Record> **ppNextPointer;
    HASH_NODE<_Record> *pNewNextNode;
    DWORD               nNewBuckets;

    //
    // If number of items has become too many, we will double the hash table
    // size (we never reduce it however)
    //
    if (_nItems <= PRIME::GetPrime(2*_nBuckets))
    {
        return;
    }

    _tableLock.ExclusiveAcquire();

    nNewBuckets = PRIME::GetPrime(2*_nBuckets);

    if (_nItems <= nNewBuckets)
    {
        goto Finished;
    }

    nBuckets = nNewBuckets;
    if (nBuckets >= 0xffffffff/sizeof(HASH_NODE<_Record> *))
    {
        goto Finished;
    }
    ppBuckets = (HASH_NODE<_Record> **)HeapAlloc(
                        GetProcessHeap(),
                        HEAP_ZERO_MEMORY,
                        nBuckets*sizeof(HASH_NODE<_Record> *));
    if (ppBuckets == NULL)
    {
        goto Finished;
    }

    //
    // Take out nodes from the old hash table and insert in the new one, make
    // sure to keep the hashes in increasing order
    //
    for (DWORD i=0; i<_nBuckets; i++)
    {
        pNode = _ppBuckets[i];
        while (pNode != NULL)
        {
            pNextNode = pNode->_pNext;

            ppNextPointer = ppBuckets + (pNode->_dwHash % nBuckets);
            pNewNextNode = *ppNextPointer;
            while (pNewNextNode != NULL &&
                   pNewNextNode->_dwHash <= pNode->_dwHash)
            {
                ppNextPointer = &pNewNextNode->_pNext;
                pNewNextNode = pNewNextNode->_pNext;
            }
            pNode->_pNext = pNewNextNode;
            *ppNextPointer = pNode;

            pNode = pNextNode;
        }
    }

    HeapFree(GetProcessHeap(), 0, _ppBuckets);
    _ppBuckets = ppBuckets;
    _nBuckets = nBuckets;
    ppBuckets = NULL;

Finished:

    _tableLock.ExclusiveRelease();
}
