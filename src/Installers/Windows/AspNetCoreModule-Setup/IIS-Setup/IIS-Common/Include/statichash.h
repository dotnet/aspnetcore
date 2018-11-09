// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef __STATIC_HASH__H_
#define __STATIC_HASH__H_

#define STATIC_STRING_HASH_BUCKETS         131

//
// SERVER_VARIABLE_HASH maps server variable string to routines to eval them
//

struct STATIC_STRING_HASH_RECORD
{
    CHAR *                          _pszName;
    STATIC_STRING_HASH_RECORD *     _pNext;
    USHORT                          _cchName;
};

struct STATIC_STRING_HASH_ITER
{
    STATIC_STRING_HASH_RECORD *_pCursor;
    DWORD _dwBucket;
    BOOL _fRemove;
};

class STATIC_STRING_HASH
{
 public:

    STATIC_STRING_HASH(
        BOOL fCaseSensitive = FALSE
    ) : _fCaseSensitive( fCaseSensitive )
    {
        Reset();
    }

    VOID
    Reset()
    {
        ZeroMemory( &_rgBuckets, sizeof( _rgBuckets ) );
    }

    static
    PCSTR
    ExtractKey(
        __in const STATIC_STRING_HASH_RECORD * pRecord
    )
    /*++

    Routine Description:

        Get the key out of the record

    Arguments:

        record to fetch the key from

    Return Value:

        key

    --*/
    {
        DBG_ASSERT( pRecord != NULL );
        return pRecord->_pszName;
    }

    VOID
    InsertRecord(
        __in STATIC_STRING_HASH_RECORD *        pRecord
    )
    /*++

    Routine Description:

        Insert record to hash table

        Note: remember this is static hash table
        There is no synchronization on the table
        Exclusive acess must be assured by caller

    Arguments:

        record to fetch the key from

    Return Value:

        VOID

    --*/
    {
        DWORD                   dwIndex;
        STATIC_STRING_HASH_RECORD* pCursor;

        DBG_ASSERT( pRecord != NULL );
        DBG_ASSERT( pRecord->_pszName != NULL );

        if(NULL == pRecord->_pszName)
        {
            return;
        }

        if (_fCaseSensitive)
        {
            dwIndex = HashString( pRecord->_pszName ) % STATIC_STRING_HASH_BUCKETS;
        }
        else
        {
            dwIndex = HashStringNoCase( pRecord->_pszName ) % STATIC_STRING_HASH_BUCKETS;
        }

        pCursor = _rgBuckets[ dwIndex ];

        pRecord->_pNext = pCursor;
        _rgBuckets[ dwIndex ] = pRecord;
    }

    STATIC_STRING_HASH_RECORD *
    FindKey(
        __in PCSTR  pszName,
        BOOL   fRemove = FALSE
    )
    /*++

    Routine Description:

        Find key in the table (and remove it optionally)

    Arguments:

        key

    Return Value:

        record containing the key

    --*/
    {
        DWORD                   dwIndex;
        STATIC_STRING_HASH_RECORD* pRecord;
        STATIC_STRING_HASH_RECORD* pLastRecord = NULL;

        DBG_ASSERT( pszName != NULL );

        if (_fCaseSensitive)
        {
            dwIndex = HashString( pszName ) % STATIC_STRING_HASH_BUCKETS;
        }
        else
        {
            dwIndex = HashStringNoCase( pszName ) % STATIC_STRING_HASH_BUCKETS;
        }

        pRecord = _rgBuckets[ dwIndex ];
        while ( pRecord != NULL )
        {
            if (_fCaseSensitive)
            {
                if ( strcmp( pszName, pRecord->_pszName ) == 0 )
                {
                    break;
                }
            }
            else if ( _stricmp( pszName, pRecord->_pszName ) == 0 )
            {
                break;
            }

            pLastRecord = pRecord;
            pRecord = pRecord->_pNext;
        }

        if (fRemove &&
            pRecord != NULL)
        {
            if (pLastRecord != NULL)
            {
                pLastRecord->_pNext = pRecord->_pNext;
            }
            else
            {
                _rgBuckets[dwIndex] = pRecord->_pNext;
            }
        }

        return pRecord;
    }

    BOOL
    CheckDistribution(
        IN DWORD       dwConflictThreshold,
        IN BOOL        fToDebugger
    )
     /*++

    Routine Description:

        Simple verification on conflicts within the table

    Arguments:

        dwConflictThreshold - max number of entries tolerated per bucket
        fToDebbuger - spew the entries exceeding threshold into debugger

    Return Value:

        FALSE it threshold was reached (means hash funcion may not be optimal)

    --*/
    {
        BOOL fThresholdReached = FALSE;
        STATIC_STRING_HASH_RECORD* pRecord;
        for ( DWORD dwIndex = 0; dwIndex < STATIC_STRING_HASH_BUCKETS; dwIndex++)
        {
            pRecord = _rgBuckets[ dwIndex ];
            DWORD countInBucket = 0;
            while ( pRecord != NULL )
            {
                countInBucket++;
                pRecord = pRecord->_pNext;
            }
            //
            // print out the list of multiple entries in bucket
            //
            if ( countInBucket > dwConflictThreshold && fToDebugger )
            {
                fThresholdReached = TRUE;

                pRecord = _rgBuckets[ dwIndex ];
                while ( pRecord != NULL )
                {
                    pRecord = pRecord->_pNext;
                }
            }
        }
        return fThresholdReached;
    }

    STATIC_STRING_HASH_RECORD *
    FindFirst(
        STATIC_STRING_HASH_ITER *pIterator,
        BOOL fRemove = FALSE
        )
    /*++

    Routine Description:

        Begins a new hash item enumeration.

    Arguments:

        pIterator - Supplies the context for the enumeration.

        fRemove - Supplies TRUE if the items should be removed
            from the hash as they are enumerated.

    Return Value:

        The first entry in the hash if successful, NULL otherwise.

    --*/
    {
        pIterator->_dwBucket = 0;
        pIterator->_fRemove = fRemove;
        pIterator->_pCursor = FindNextBucket(&pIterator->_dwBucket);

        if (pIterator->_fRemove && pIterator->_pCursor != NULL)
        {
            _rgBuckets[pIterator->_dwBucket] = pIterator->_pCursor->_pNext;
        }

        return pIterator->_pCursor;
    }

    STATIC_STRING_HASH_RECORD *
    FindNext(
        STATIC_STRING_HASH_ITER *pIterator
        )
    /*++

    Routine Description:

        Continues a hash item enumeration.

    Arguments:

        pIterator - Supplies the context for the enumeration.

    Return Value:

        The next entry in the hash if successful, NULL otherwise.

    --*/
    {
        if (pIterator->_pCursor != NULL)
        {
            if (pIterator->_fRemove)
            {
                pIterator->_pCursor = _rgBuckets[pIterator->_dwBucket];
            }
            else
            {
                pIterator->_pCursor = pIterator->_pCursor->_pNext;
            }

            if (pIterator->_pCursor == NULL)
            {
                pIterator->_dwBucket++;
                pIterator->_pCursor = FindNextBucket(&pIterator->_dwBucket);
            }
        }

        if (pIterator->_fRemove && pIterator->_pCursor != NULL)
        {
            _rgBuckets[pIterator->_dwBucket] = pIterator->_pCursor->_pNext;
        }

        return pIterator->_pCursor;
    }

 protected:

    STATIC_STRING_HASH_RECORD * _rgBuckets[ STATIC_STRING_HASH_BUCKETS ];

 private:

    BOOL                        _fCaseSensitive;

    STATIC_STRING_HASH_RECORD *
    FindNextBucket(
        DWORD *pdwStartingBucket
        )
    /*++

    Routine Description:

        Scan for the next non-empty bucket.

    Arguments:

        pdwStartingBucket - Supplies a pointer to the starting
            bucket index. This value is updated with the index
            of the next non-empty bucket if successful.

    Return Value:

        The first entry in the next non-empty bucket if successful,
        NULL otherwise.

    --*/
    {
        DWORD i;
        STATIC_STRING_HASH_RECORD *pScan = NULL;

        for (i = *pdwStartingBucket ; i < STATIC_STRING_HASH_BUCKETS ; i++)
        {
            pScan = _rgBuckets[i];

            if (pScan != NULL)
            {
                break;
            }
        }

        *pdwStartingBucket = i;
        return pScan;
    }
};




struct STATIC_WSTRING_HASH_RECORD
{
    WCHAR *                         _pszName;
    STATIC_WSTRING_HASH_RECORD *    _pNext;
    USHORT                          _cchName;
};


struct STATIC_WSTRING_HASH_ITER
{
    STATIC_WSTRING_HASH_RECORD *_pCursor;
    DWORD _dwBucket;
    BOOL _fRemove;
};


class STATIC_WSTRING_HASH
{
 public:
    STATIC_WSTRING_HASH(
        BOOL        fCaseSensitive = FALSE
    ) : _fCaseSensitive( fCaseSensitive )
    {
        Reset();
    }

    VOID
    Reset()
    {
        ZeroMemory( &_rgBuckets, sizeof( _rgBuckets ) );
    }

    static
    PCWSTR
    ExtractKey(
        __in const STATIC_WSTRING_HASH_RECORD * pRecord
    )
    /*++

    Routine Description:

        Get the key out of the record

    Arguments:

        record to fetch the key from

    Return Value:

        key

    --*/
    {
        DBG_ASSERT( pRecord != NULL );
        return pRecord->_pszName;
    }

    VOID
    InsertRecord(
        __in STATIC_WSTRING_HASH_RECORD *        pRecord
    )
    /*++

    Routine Description:

        Insert record to hash table

        Note: remember this is static hash table
        There is no synchronization on the table
        Exclusive acess must be assured by caller

    Arguments:

        record to fetch the key from

    Return Value:

        VOID

    --*/
    {
        DWORD                   dwIndex;
        STATIC_WSTRING_HASH_RECORD* pCursor;

        DBG_ASSERT( pRecord != NULL );
        DBG_ASSERT( pRecord->_pszName != NULL );

        if (_fCaseSensitive)
        {
            dwIndex = HashString( pRecord->_pszName ) % STATIC_STRING_HASH_BUCKETS;
        }
        else
        {
            dwIndex = HashStringNoCase( pRecord->_pszName ) % STATIC_STRING_HASH_BUCKETS;
        }

        pCursor = _rgBuckets[ dwIndex ];

        pRecord->_pNext = pCursor;
        _rgBuckets[ dwIndex ] = pRecord;
    }

    STATIC_WSTRING_HASH_RECORD *
    FindKey(
        __in PCWSTR  pszName,
        BOOL    fRemove = FALSE
    )
    /*++

    Routine Description:

        Find key in the table (and remove it optionally)

    Arguments:

        key

    Return Value:

        record containing the key

    --*/
    {
        DWORD                   dwIndex;
        STATIC_WSTRING_HASH_RECORD* pRecord;
        STATIC_WSTRING_HASH_RECORD* pLastRecord = NULL;

        DBG_ASSERT( pszName != NULL );

        if (_fCaseSensitive)
        {
            dwIndex = HashString( pszName ) % STATIC_STRING_HASH_BUCKETS;
        }
        else
        {
            dwIndex = HashStringNoCase( pszName ) % STATIC_STRING_HASH_BUCKETS;
        }

        pRecord = _rgBuckets[ dwIndex ];
        while ( pRecord != NULL )
        {
            if ( _fCaseSensitive )
            {
                if ( wcscmp( pszName, pRecord->_pszName ) == 0 )
                {
                    break;
                }
            }
            else if ( _wcsicmp( pszName, pRecord->_pszName ) == 0 )
            {
                break;
            }

            pLastRecord = pRecord;
            pRecord = pRecord->_pNext;
        }

        if (fRemove &&
            pRecord != NULL)
        {
            if (pLastRecord != NULL)
            {
                pLastRecord->_pNext = pRecord->_pNext;
            }
            else
            {
                _rgBuckets[dwIndex] = pRecord->_pNext;
            }
        }

        return pRecord;
    }

    BOOL
    CheckDistribution(
        IN DWORD       dwConflictThreshold,
        IN BOOL        fToDebugger
    )
     /*++

    Routine Description:

        Simple verification on conflicts within the table

    Arguments:

        dwConflictThreshold - max number of entries tolerated per bucket
        fToDebbuger - spew the entries exceeding threshold into debugger

    Return Value:

        FALSE it threshold was reached (means hash funcion may not be optimal)

    --*/
    {
        BOOL fThresholdReached = FALSE;
        STATIC_WSTRING_HASH_RECORD* pRecord;
        for ( DWORD dwIndex = 0; dwIndex < STATIC_STRING_HASH_BUCKETS; dwIndex++)
        {
            pRecord = _rgBuckets[ dwIndex ];
            DWORD countInBucket = 0;
            while ( pRecord != NULL )
            {
                countInBucket++;
                pRecord = pRecord->_pNext;
            }
            //
            // print out the list of multiple entries in bucket
            //
            if ( countInBucket > dwConflictThreshold && fToDebugger )
            {
                fThresholdReached = TRUE;

                pRecord = _rgBuckets[ dwIndex ];
                while ( pRecord != NULL )
                {
                    pRecord = pRecord->_pNext;
                }
            }
        }
        return fThresholdReached;
    }

    STATIC_WSTRING_HASH_RECORD *
    FindFirst(
        STATIC_WSTRING_HASH_ITER *pIterator,
        BOOL fRemove = FALSE
        )
    /*++

    Routine Description:

        Begins a new hash item enumeration.

    Arguments:

        pIterator - Supplies the context for the enumeration.

        fRemove - Supplies TRUE if the items should be removed
            from the hash as they are enumerated.

    Return Value:

        The first entry in the hash if successful, NULL otherwise.

    --*/
    {
        pIterator->_dwBucket = 0;
        pIterator->_fRemove = fRemove;
        pIterator->_pCursor = FindNextBucket(&pIterator->_dwBucket);

        if (pIterator->_fRemove && pIterator->_pCursor != NULL)
        {
            _rgBuckets[pIterator->_dwBucket] = pIterator->_pCursor->_pNext;
        }

        return pIterator->_pCursor;
    }

    STATIC_WSTRING_HASH_RECORD *
    FindNext(
        STATIC_WSTRING_HASH_ITER *pIterator
        )
    /*++

    Routine Description:

        Continues a hash item enumeration.

    Arguments:

        pIterator - Supplies the context for the enumeration.

    Return Value:

        The next entry in the hash if successful, NULL otherwise.

    --*/
    {
        if (pIterator->_pCursor != NULL)
        {
            if (pIterator->_fRemove)
            {
                pIterator->_pCursor = _rgBuckets[pIterator->_dwBucket];
            }
            else
            {
                pIterator->_pCursor = pIterator->_pCursor->_pNext;
            }

            if (pIterator->_pCursor == NULL)
            {
                pIterator->_dwBucket++;
                pIterator->_pCursor = FindNextBucket(&pIterator->_dwBucket);
            }
        }

        if (pIterator->_fRemove && pIterator->_pCursor != NULL)
        {
            _rgBuckets[pIterator->_dwBucket] = pIterator->_pCursor->_pNext;
        }

        return pIterator->_pCursor;
    }

 protected:

    STATIC_WSTRING_HASH_RECORD *  _rgBuckets[ STATIC_STRING_HASH_BUCKETS ];

 private:

    BOOL                          _fCaseSensitive;

    STATIC_WSTRING_HASH_RECORD *
    FindNextBucket(
        DWORD *pdwStartingBucket
        )
    /*++

    Routine Description:

        Scan for the next non-empty bucket.

    Arguments:

        pdwStartingBucket - Supplies a pointer to the starting
            bucket index. This value is updated with the index
            of the next non-empty bucket if successful.

    Return Value:

        The first entry in the next non-empty bucket if successful,
        NULL otherwise.

    --*/
    {
        DWORD i;
        STATIC_WSTRING_HASH_RECORD *pScan = NULL;

        for (i = *pdwStartingBucket ; i < STATIC_STRING_HASH_BUCKETS ; i++)
        {
            pScan = _rgBuckets[i];

            if (pScan != NULL)
            {
                break;
            }
        }

        *pdwStartingBucket = i;
        return pScan;
    }
};


#endif //__STATIC_HASH__H_

