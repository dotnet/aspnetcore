// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <crtdbg.h>
#include "rwlock.h"
#include "prime.h"

template <class _Record>
class TREE_HASH_NODE
{
    template <class _Record>
    friend class TREE_HASH_TABLE;

 private:
    // Next node in the hash table look-aside
    TREE_HASH_NODE<_Record> *_pNext;

    // links in the tree structure
    TREE_HASH_NODE *    _pParentNode;
    TREE_HASH_NODE *    _pFirstChild;
    TREE_HASH_NODE *    _pNextSibling;

    // actual record
    _Record *           _pRecord;

    // hash value
    PCWSTR              _pszPath;
    DWORD               _dwHash;
};

template <class _Record>
class TREE_HASH_TABLE
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
    TREE_HASH_TABLE(
        BOOL    fCaseSensitive
    ) : _ppBuckets( NULL ),
        _nBuckets( 0 ),
        _nItems( 0 ),
        _fCaseSensitive( fCaseSensitive )
    {
    }

    virtual
    ~TREE_HASH_TABLE();

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
    PCWSTR
    GetKey(
        _Record *   pRecord
    ) = 0;

    DWORD
    Count()
    {
        return _nItems;
    }

    virtual
    VOID
    Clear();

    HRESULT
    Initialize(
        DWORD           nBucketSize
    );

    DWORD
    CalcHash(
        PCWSTR      pszKey
    )
    {
        return _fCaseSensitive ? HashString(pszKey) : HashStringNoCase(pszKey);
    }

    virtual
    VOID
    FindKey(
        PCWSTR      pszKey,
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
        PCWSTR      pszKey
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

    BOOL
    FindNodeInternal(
        PCWSTR                      pszKey,
        DWORD                       dwHash,
        TREE_HASH_NODE<_Record> **  ppNode,
        TREE_HASH_NODE<_Record> *** pppPreviousNodeNextPointer = NULL
    );

    HRESULT
    AddNodeInternal(
        PCWSTR                      pszPath,
        DWORD                       dwHash,
        _Record *                   pRecord,
        TREE_HASH_NODE<_Record> *   pParentNode,
        TREE_HASH_NODE<_Record> **  ppNewNode
    );

    HRESULT
    AllocateNode(
        PCWSTR                      pszPath,
        DWORD                       dwHash,
        _Record *                   pRecord,
        TREE_HASH_NODE<_Record> *   pParentNode,
        TREE_HASH_NODE<_Record> **  ppNewNode
    );

    VOID
    DeleteNode(
        TREE_HASH_NODE<_Record> *   pNode
    )
    {
        if (pNode->_pRecord != NULL)
        {
            DereferenceRecord(pNode->_pRecord);
            pNode->_pRecord = NULL;
        }

        HeapFree(GetProcessHeap(),
                 0,
                 pNode);
    }

    VOID
    DeleteNodeInternal(
        TREE_HASH_NODE<_Record> **  ppPreviousNodeNextPointer,
        TREE_HASH_NODE<_Record> *   pNode
    );

    VOID
    RehashTableIfNeeded(
        VOID
    );

    TREE_HASH_NODE<_Record> **  _ppBuckets;
    DWORD                       _nBuckets;
    DWORD                       _nItems;
    BOOL                        _fCaseSensitive;
    CWSDRWLock                  _tableLock;
};

template <class _Record>
HRESULT
TREE_HASH_TABLE<_Record>::AllocateNode(
    PCWSTR                      pszPath,
    DWORD                       dwHash,
    _Record *                   pRecord,
    TREE_HASH_NODE<_Record> *   pParentNode,
    TREE_HASH_NODE<_Record> **  ppNewNode
)
{
    //
    // Allocate enough extra space for pszPath
    //
    DWORD cchPath = (DWORD) wcslen(pszPath);
    if (cchPath >= ((0xffffffff - sizeof(TREE_HASH_NODE<_Record>))/sizeof(WCHAR) - 1))
    {
        return HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
    }
    TREE_HASH_NODE<_Record> *pNode = (TREE_HASH_NODE<_Record> *)HeapAlloc(
            GetProcessHeap(),
            HEAP_ZERO_MEMORY,
            sizeof(TREE_HASH_NODE<_Record>) + (cchPath+1)*sizeof(WCHAR));
    if (pNode == NULL)
    {
        return HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
    }

    memcpy(pNode+1, pszPath, (cchPath+1)*sizeof(WCHAR));
    pNode->_pszPath = (PCWSTR)(pNode+1);
    pNode->_dwHash = dwHash;
    pNode->_pNext = pNode->_pNextSibling = pNode->_pFirstChild = NULL;
    pNode->_pParentNode = pParentNode;
    pNode->_pRecord = pRecord;

    *ppNewNode = pNode;
    return S_OK;
}

template <class _Record>
HRESULT
TREE_HASH_TABLE<_Record>::Initialize(
    DWORD   nBuckets
)
{
    HRESULT hr = S_OK;

    if ( nBuckets == 0 )
    {
        hr = E_INVALIDARG;
        goto Failed;
    }

    hr = _tableLock.Init();
    if ( FAILED( hr ) )
    {
        goto Failed;
    }

    if (nBuckets >= 0xffffffff/sizeof(TREE_HASH_NODE<_Record> *))
    {
        hr = E_INVALIDARG;
        goto Failed;
    }

    _ppBuckets = (TREE_HASH_NODE<_Record> **)HeapAlloc(
                            GetProcessHeap(),
                            HEAP_ZERO_MEMORY,
                            nBuckets*sizeof(TREE_HASH_NODE<_Record> *));
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


template <class _Record>
TREE_HASH_TABLE<_Record>::~TREE_HASH_TABLE()
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

template <class _Record>
VOID
TREE_HASH_TABLE<_Record>::Clear()
{
    TREE_HASH_NODE<_Record> *pCurrent;
    TREE_HASH_NODE<_Record> *pNext;

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

template <class _Record>
BOOL
TREE_HASH_TABLE<_Record>::FindNodeInternal(
    PCWSTR                  pszKey,
    DWORD                   dwHash,
    TREE_HASH_NODE<_Record> **   ppNode,
    TREE_HASH_NODE<_Record> ***  pppPreviousNodeNextPointer
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
    TREE_HASH_NODE<_Record> **ppPreviousNodeNextPointer;
    TREE_HASH_NODE<_Record> *pNode;
    BOOL fFound = FALSE;

    ppPreviousNodeNextPointer = _ppBuckets + (dwHash % _nBuckets);
    pNode = *ppPreviousNodeNextPointer;
    while (pNode != NULL)
    {
        if (pNode->_dwHash == dwHash)
        {
            if (CompareStringOrdinal(pszKey,
                                     -1,
                                     pNode->_pszPath,
                                     -1,
                                     !_fCaseSensitive) == CSTR_EQUAL)
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

    *ppNode = pNode;
    if (pppPreviousNodeNextPointer != NULL)
    {
        *pppPreviousNodeNextPointer = ppPreviousNodeNextPointer;
    }
    return fFound;
}

template <class _Record>
VOID
TREE_HASH_TABLE<_Record>::FindKey(
    PCWSTR              pszKey,
    _Record **          ppRecord
)
{
    TREE_HASH_NODE<_Record> *pNode;

    *ppRecord = NULL;

    DWORD dwHash = CalcHash(pszKey);

    _tableLock.SharedAcquire();

    if (FindNodeInternal(pszKey, dwHash, &pNode) &&
        pNode->_pRecord != NULL)
    {
        ReferenceRecord(pNode->_pRecord);
        *ppRecord = pNode->_pRecord;
    }

    _tableLock.SharedRelease();
}

template <class _Record>
HRESULT
TREE_HASH_TABLE<_Record>::AddNodeInternal(
    PCWSTR                      pszPath,
    DWORD                       dwHash,
    _Record *                   pRecord,
    TREE_HASH_NODE<_Record> *   pParentNode,
    TREE_HASH_NODE<_Record> **  ppNewNode
)
/*++
  Return value is HRESULT indicating success or failure
  pszPath, dwHash, pRecord - path, hash value and record to be inserted
  pParentNode - this will be the parent of the node being inserted
  ppNewNode - on successful return, the new node created and inserted

  This function may be called under a read or write lock
--*/
{
    TREE_HASH_NODE<_Record> *pNewNode;
    TREE_HASH_NODE<_Record> *pNextNode;
    TREE_HASH_NODE<_Record> **ppNextPointer;
    HRESULT hr;

    //
    // Ownership of pRecord is not transferred to pNewNode yet, so remember
    // to either set it to null before deleting pNewNode or add an extra
    // reference later - this is to make sure we do not do an extra ref/deref
    // which users may view as getting flushed out of the hash-table
    //
    hr = AllocateNode(pszPath,
                      dwHash,
                      pRecord,
                      pParentNode,
                      &pNewNode);
    if (FAILED(hr))
    {
        return hr;
    }

    do
    {
        //
        // Find the right place to add this node
        //

        if (FindNodeInternal(pszPath, dwHash, &pNextNode, &ppNextPointer))
        {
            //
            // If node already there, record may still need updating
            //
            if (pRecord != NULL &&
                InterlockedCompareExchangePointer((PVOID *)&pNextNode->_pRecord,
                                                  pRecord,
                                                  NULL) == NULL)
            {
                ReferenceRecord(pRecord);
                hr = S_OK;
            }
            else
            {
                hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
            }

            // ownership of pRecord has either passed to existing record or
            // not to anyone at all
            pNewNode->_pRecord = NULL;
            DeleteNode(pNewNode);
            *ppNewNode = pNextNode;
            return hr;
        }

        //
        // If another node got inserted in betwen, we will have to retry
        //
        pNewNode->_pNext = pNextNode;
    } while (InterlockedCompareExchangePointer((PVOID *)ppNextPointer,
                                               pNewNode,
                                               pNextNode) != pNextNode);
    // pass ownership of pRecord now
    if (pRecord != NULL)
    {
        ReferenceRecord(pRecord);
        pRecord = NULL;
    }
    InterlockedIncrement((LONG *)&_nItems);

    //
    // update the parent
    //
    if (pParentNode != NULL)
    {
        ppNextPointer = &pParentNode->_pFirstChild;
        do
        {
            pNextNode = *ppNextPointer;
            pNewNode->_pNextSibling = pNextNode;
        } while (InterlockedCompareExchangePointer((PVOID *)ppNextPointer,
                                                   pNewNode,
                                                   pNextNode) != pNextNode);
    }

    *ppNewNode = pNewNode;
    return S_OK;
}

template <class _Record>
HRESULT
TREE_HASH_TABLE<_Record>::InsertRecord(
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
    PCWSTR pszKey = GetKey(pRecord);
    STACK_STRU( strPartialPath, 256);
    PWSTR pszPartialPath;
    DWORD dwHash;
    DWORD cchEnd;
    HRESULT hr;
    TREE_HASH_NODE<_Record> *pParentNode = NULL;

    hr = strPartialPath.Copy(pszKey);
    if (FAILED(hr))
    {
        goto Finished;
    }
    pszPartialPath = strPartialPath.QueryStr();

    _tableLock.SharedAcquire();

    //
    // First find the lowest parent node present
    //
    for (cchEnd = strPartialPath.QueryCCH() - 1; cchEnd > 0; cchEnd--)
    {
        if (pszPartialPath[cchEnd] == L'/' || pszPartialPath[cchEnd] == L'\\')
        {
            pszPartialPath[cchEnd] = L'\0';

            dwHash = CalcHash(pszPartialPath);
            if (FindNodeInternal(pszPartialPath, dwHash, &pParentNode))
            {
                pszPartialPath[cchEnd] = pszKey[cchEnd];
                break;
            }
            pParentNode = NULL;
        }
    }

    //
    // Now go ahead and add the rest of the tree (including our record)
    //
    for (; cchEnd <= strPartialPath.QueryCCH(); cchEnd++)
    {
        if (pszPartialPath[cchEnd] == L'\0')
        {
            dwHash = CalcHash(pszPartialPath);
            hr = AddNodeInternal(
                    pszPartialPath,
                    dwHash,
                    (cchEnd == strPartialPath.QueryCCH()) ? pRecord : NULL,
                    pParentNode,
                    &pParentNode);
            if (FAILED(hr) &&
                hr != HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS))
            {
                goto Finished;
            }

            pszPartialPath[cchEnd] = pszKey[cchEnd];
        }
    }

Finished:
    _tableLock.SharedRelease();

    if (SUCCEEDED(hr))
    {
        RehashTableIfNeeded();
    }

    return hr;
}

template <class _Record>
VOID
TREE_HASH_TABLE<_Record>::DeleteNodeInternal(
    TREE_HASH_NODE<_Record> **  ppNextPointer,
    TREE_HASH_NODE<_Record> *   pNode
)
/*++
  pNode is the node to be deleted
  ppNextPointer is the pointer to the previous node's next pointer pointing
  to this node

  This function should be called under write-lock
--*/
{
    //
    // First remove this node from hash table
    //
    *ppNextPointer = pNode->_pNext;

    //
    // Now fixup parent
    //
    if (pNode->_pParentNode != NULL)
    {
        ppNextPointer = &pNode->_pParentNode->_pFirstChild;
        while (*ppNextPointer != pNode)
        {
            ppNextPointer = &(*ppNextPointer)->_pNextSibling;
        }
        *ppNextPointer = pNode->_pNextSibling;
    }

    //
    // Now remove all children recursively
    //
    TREE_HASH_NODE<_Record> *pChild = pNode->_pFirstChild;
    TREE_HASH_NODE<_Record> *pNextChild;
    while (pChild != NULL)
    {
        pNextChild = pChild->_pNextSibling;

        ppNextPointer = _ppBuckets + (pChild->_dwHash % _nBuckets);
        while (*ppNextPointer != pChild)
        {
            ppNextPointer = &(*ppNextPointer)->_pNext;
        }
        pChild->_pParentNode = NULL;
        DeleteNodeInternal(ppNextPointer, pChild);

        pChild = pNextChild;
    }

    DeleteNode(pNode);
    _nItems--;
}

template <class _Record>
VOID
TREE_HASH_TABLE<_Record>::DeleteKey(
    PCWSTR      pszKey
)
{
    TREE_HASH_NODE<_Record> *pNode;
    TREE_HASH_NODE<_Record> **ppPreviousNodeNextPointer;

    DWORD dwHash = CalcHash(pszKey);

    _tableLock.ExclusiveAcquire();

    if (FindNodeInternal(pszKey, dwHash, &pNode, &ppPreviousNodeNextPointer))
    {
        DeleteNodeInternal(ppPreviousNodeNextPointer, pNode);
    }

    _tableLock.ExclusiveRelease();
}

template <class _Record>
VOID
TREE_HASH_TABLE<_Record>::DeleteIf(
    PFN_DELETE_IF               pfnDeleteIf,
    PVOID                       pvContext
)
{
    TREE_HASH_NODE<_Record> *pNode;
    TREE_HASH_NODE<_Record> **ppPreviousNodeNextPointer;
    BOOL fDelete;

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
            fDelete = FALSE;
            if (pNode->_pRecord != NULL)
            {
                if (pfnDeleteIf(pNode->_pRecord, pvContext))
                {
                    fDelete = TRUE;
                }
            }
            else if (pNode->_pFirstChild == NULL)
            {
                fDelete =  TRUE;
            }

            if (fDelete)
            {
                if (pNode->_pFirstChild == NULL)
                {
                    DeleteNodeInternal(ppPreviousNodeNextPointer, pNode);
                }
                else
                {
                    DereferenceRecord(pNode->_pRecord);
                    pNode->_pRecord = NULL;
                }
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

template <class _Record>
VOID
TREE_HASH_TABLE<_Record>::Apply(
    PFN_APPLY                   pfnApply,
    PVOID                       pvContext
)
{
    TREE_HASH_NODE<_Record> *pNode;

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

template <class _Record>
VOID
TREE_HASH_TABLE<_Record>::RehashTableIfNeeded(
    VOID
)
{
    TREE_HASH_NODE<_Record> **ppBuckets;
    DWORD nBuckets;
    TREE_HASH_NODE<_Record> *pNode;
    TREE_HASH_NODE<_Record> *pNextNode;
    TREE_HASH_NODE<_Record> **ppNextPointer;
    TREE_HASH_NODE<_Record> *pNewNextNode;
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
    if (nBuckets >= 0xffffffff/sizeof(TREE_HASH_NODE<_Record> *))
    {
        goto Finished;
    }
    ppBuckets = (TREE_HASH_NODE<_Record> **)HeapAlloc(
                        GetProcessHeap(),
                        HEAP_ZERO_MEMORY,
                        nBuckets*sizeof(TREE_HASH_NODE<_Record> *));
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

