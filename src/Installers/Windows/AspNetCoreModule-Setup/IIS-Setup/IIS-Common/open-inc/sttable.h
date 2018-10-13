// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _STTABLE_H
#define _STTABLE_H

#include <stdio.h>
#include <windows.h>
#include "stbuff.h"
#include "stlock.h"
#include "stlist.h"

#define DEFAULT_BUCKETS 97  // largest prime under 100

class ITEM;
class STTABLE_ITEM;

typedef DWORD (WINAPI * PFN_HASH)(STBUFF*);
typedef BOOL (WINAPI * PFN_COMPARE_KEYS)(STBUFF*,STBUFF*);
typedef VOID (WINAPI * PFN_ITER)(STTABLE_ITEM*, BOOL*);

class STTABLE_ITEM
{
public:

    STTABLE_ITEM()
        : _cRefs( 1 )
    {
        InitializeListHead( &le );
    }

    VOID
    Reference()
    {
        InterlockedIncrement( &_cRefs );
    }

    VOID
    Dereference()
    {
        LONG cRefs = InterlockedDecrement( &_cRefs );

        if ( cRefs == 0 )
        {
            delete this;
        }
    }

    HRESULT
    Initialize(
        STBUFF * pKey
        )
    {
        return _buffKey.SetData( pKey->QueryPtr(),
                                 pKey->QueryDataSize() );
    }

    STBUFF *
    QueryKey()
    {
        return &_buffKey;
    }

    virtual
    ~STTABLE_ITEM()
    {}

    LIST_ENTRY le;

private:

    LONG     _cRefs;
    STBUFF _buffKey;
};

class STTABLE_BUCKET
{
public:

    STTABLE_BUCKET()
        : _pfnCompareKeys( NULL )
    {}

    virtual
    ~STTABLE_BUCKET()
    {
        LIST_ENTRY *      pEntry;
        STTABLE_ITEM * pItem;

        while ( !IsListEmpty( &_Head ) )
        {
            pEntry = RemoveHeadList( &_Head );

            pItem = CONTAINING_RECORD( pEntry,
                                       STTABLE_ITEM,
                                       le );

            pItem->Dereference();
            pItem = NULL;
        }
    }

    HRESULT
    Initialize(
        PFN_COMPARE_KEYS pfnCompareKeys = NULL
        )
    {
        InitializeListHead( &_Head );

        _pfnCompareKeys = pfnCompareKeys;

        return _BucketLock.Initialize();
    }

    HRESULT
    Insert(
        STTABLE_ITEM * pNewItem
        )
    {
        LIST_ENTRY *     pEntry;
        STTABLE_ITEM *   pItem;
        HRESULT          hr = S_OK; 

        _BucketLock.ExclusiveAcquire();

        //
        // Check to see if the item is already in the list
        //

        pEntry = _Head.Flink;

        while ( pEntry != &_Head )
        {
            pItem = CONTAINING_RECORD( pEntry,
                                       STTABLE_ITEM,
                                       le );

            if ( CompareKeys( pNewItem->QueryKey(),
                              pItem->QueryKey() ) )
            {
                hr =  HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
                goto Finished;
            }

            pEntry = pEntry->Flink;
        }

        //
        // It's not in the list.  Add it now
        //

        pNewItem->Reference();
        InsertTailList( &_Head, &pNewItem->le );

Finished:

        _BucketLock.ExclusiveRelease();

        return hr;
    }

    HRESULT
    Remove(
        STTABLE_ITEM * pItemToRemove
        )
    {
        LIST_ENTRY *     pEntry;
        STTABLE_ITEM *   pItem;

        _BucketLock.ExclusiveAcquire();

        //
        // Find the item in the list
        //

        pEntry = _Head.Flink;

        while ( pEntry != &_Head )
        {
            pItem = CONTAINING_RECORD( pEntry,
                                       STTABLE_ITEM,
                                       le );

            if ( CompareKeys( pItemToRemove->QueryKey(),
                              pItem->QueryKey() ) )
            {
                RemoveEntryList( &pItemToRemove->le );
                pItemToRemove->Dereference();
                pItemToRemove = NULL;

                goto Finished;
            }

            pEntry = pEntry->Flink;
        }

        //
        // Item was not found.  Set error code, but
        // don't fail function.
        //

        SetLastError (ERROR_FILE_NOT_FOUND);

Finished:

        _BucketLock.ExclusiveRelease();

        return S_OK;
    }

    HRESULT
    GetItem(
        STBUFF * pKey,
        STTABLE_ITEM **ppItem
        )
    {
        LIST_ENTRY *     pEntry;
        STTABLE_ITEM *   pItem;
        HRESULT          hr = S_OK;

        _BucketLock.SharedAcquire();

        //
        // Find the item in the list
        //

        pEntry = _Head.Flink;

        while ( pEntry != &_Head )
        {
            pItem = CONTAINING_RECORD( pEntry,
                                       STTABLE_ITEM,
                                       le );

            if ( CompareKeys( pKey,
                              pItem->QueryKey() ) )
            {
                pItem->Reference();
                goto Finished;
            }

            pEntry = pEntry->Flink;
        }

        //
        // Item was not found.
        //

        pItem = NULL;

        hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);

Finished:

        _BucketLock.SharedRelease();

        *ppItem = pItem;

        return hr;
    }

    VOID    
    Iterate(
        PFN_ITER pIterFunction
        )
    {
        LIST_ENTRY *     pEntry;
        STTABLE_ITEM *   pItem;
        BOOL             fRemoveItem;

        ////////////////////////////////////////
        //
        // It is assumed that this function will
        // be called under a write lock
        //
        ////////////////////////////////////////

        //
        // Walk the list and call the provided
        // function on each item
        //

        pEntry = _Head.Flink;

        while ( pEntry != &_Head )
        {
            pItem = CONTAINING_RECORD( pEntry,
                                       STTABLE_ITEM,
                                       le );

            //
            // The iterator function might remove
            // the item from the list, so we need
            // to get the next link first
            //

            pEntry = pEntry->Flink;

            pItem->Reference();
            pIterFunction( pItem, &fRemoveItem );

            if ( fRemoveItem )
            {
                RemoveEntryList( &pItem->le );
                pItem->Dereference();
                pItem = NULL;
            }
        }

    }

private:

    LIST_ENTRY       _Head;
    STLOCK           _BucketLock;
    PFN_COMPARE_KEYS _pfnCompareKeys;

    BOOL
    CompareKeys(
        STBUFF * pKey1,
        STBUFF * pKey2
        )
    {
        if ( _pfnCompareKeys )
        {
            return _pfnCompareKeys( pKey1,
                                    pKey2 );
        }

        return ( strcmp( pKey1->QueryStr(),
                         pKey2->QueryStr() ) == 0 );
    }
};


class STTABLE
{
public:

    STTABLE()
        : _cBuckets( 0 ),
          _pfnHash( NULL )
    {}

    virtual
    ~STTABLE()
    {
        STTABLE_BUCKET ** rgBuckets;
        STTABLE_BUCKET *  pBucket;
        DWORD               n;

        rgBuckets = (STTABLE_BUCKET**)_buffBucketPtrs.QueryPtr();

        for( n = 0; n < _cBuckets; n++ )
        {
            pBucket = rgBuckets[n];

            delete pBucket;
            pBucket = NULL;
        }
    }

    HRESULT
    Initialize(
        DWORD            cBuckets = DEFAULT_BUCKETS,
        PFN_HASH         pfnHash  = NULL,
        PFN_COMPARE_KEYS pfnCompareKeys = NULL
        )
    {
        STTABLE_BUCKET **   rgBuckets;
        DWORD               n;
        HRESULT             hr = S_OK;

        //
        // Create a buffer for the bucket array
        //

        hr = _buffBucketPtrs.Resize( cBuckets * sizeof(STTABLE_BUCKET*) );

        if ( FAILED (hr) )
        {
            goto Finished;
        }

        rgBuckets = (STTABLE_BUCKET**)_buffBucketPtrs.QueryPtr();

        //
        // Create the buckets
        //

        for ( n = 0; n < cBuckets; n++ )
        {
            STTABLE_BUCKET * pNewBucket = new STTABLE_BUCKET;

            if ( !pNewBucket )
            {
                hr = E_OUTOFMEMORY;
                goto Finished;
            }

            hr = pNewBucket->Initialize( pfnCompareKeys );

            if ( FAILED (hr) )
            {
                delete pNewBucket;
                pNewBucket = NULL;

                goto Finished;
            }

            rgBuckets[n] = pNewBucket;
            pNewBucket = NULL;

            _cBuckets++;
        }

        //
        // Initialize the table lock
        //

        _TableLock.Initialize();

        //
        // Set the hash function
        //

        _pfnHash = pfnHash;

Finished:
        return hr;
    }

    HRESULT
    Insert(
        STTABLE_ITEM * pNewItem
        )
    {
        
        DWORD              dwHash;
        STTABLE_BUCKET *   pBucket;
        HRESULT            hr = S_OK;

        dwHash = ComputeHash( pNewItem->QueryKey() );

        pBucket = GetBucket( dwHash );

        _TableLock.SharedAcquire();
        
        hr = pBucket->Insert( pNewItem );

        _TableLock.SharedRelease();

        return hr;
    }
    
    HRESULT
    Remove(
        STTABLE_ITEM * pItemToRemove
        )
    {
        DWORD              dwHash;
        STTABLE_BUCKET *   pBucket;
        HRESULT            hr = S_OK; 

        dwHash = ComputeHash( pItemToRemove->QueryKey() );

        pBucket = GetBucket( dwHash );

        _TableLock.SharedAcquire();

        hr = pBucket->Remove( pItemToRemove );

        _TableLock.SharedRelease();

        return hr;
    }

    HRESULT
    GetItem(
        STBUFF * pKey,
        STTABLE_ITEM **ppItem
        )
    {
        DWORD              dwHash;
        STTABLE_BUCKET *   pBucket;
        STTABLE_ITEM *     pRet;
        HRESULT            hr = S_OK;

        dwHash = ComputeHash( pKey );

        pBucket = GetBucket( dwHash );

        _TableLock.SharedAcquire();

        hr = pBucket->GetItem( pKey, &pRet );
        if(FAILED( hr))
        {
            pRet = NULL;
            goto Finished;
        }
    
    Finished:

        _TableLock.SharedRelease();

        *ppItem = pRet;

        return hr;
    }

    VOID
    Iterate(
        PFN_ITER pIterFunction
        )
    {
        STTABLE_BUCKET ** rgBuckets;
        DWORD               n;

        _TableLock.ExclusiveAcquire();

        rgBuckets = (STTABLE_BUCKET**)_buffBucketPtrs.QueryPtr();

        //
        // Iterate each bucket
        //

        for ( n = 0; n < _cBuckets; n++ )
        {
            STTABLE_BUCKET * pBucket;

            pBucket = rgBuckets[n];

            pBucket->Iterate( pIterFunction );

            pBucket = NULL;
        }

        _TableLock.ExclusiveRelease();
    }

private:

    STBUFF   _buffBucketPtrs;
    DWORD    _cBuckets;
    STLOCK   _TableLock;
    PFN_HASH _pfnHash;

    DWORD
    ComputeHash(
        STBUFF * pKey
        )
    {
        if ( _pfnHash )
        {
            return _pfnHash( pKey );
        }

        return HashString( pKey->QueryStr() );
    }

    STTABLE_BUCKET *
    GetBucket(
        DWORD dwHash
        )
    {
        STTABLE_BUCKET ** rgBuckets;

        rgBuckets = (STTABLE_BUCKET**)_buffBucketPtrs.QueryPtr();

        return rgBuckets[dwHash % _cBuckets];
    }

    DWORD
    WINAPI
    HashString(
        LPCSTR szString
        )
    {
        DWORD dwRet = 0;

        //
        // Create a hash by adding up the ascii values
        // of each character in a case-insensitive manner
        //

        if ( szString )
        {
            while ( *szString )
            {
                dwRet += ( (*szString) | 0x20 );
                szString++;
            }
        }

        return dwRet;
    }
};

#endif // _STTABLE_H
