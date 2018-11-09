// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _STLOCK_H
#define _STLOCK_H

class STLOCK
{
public:

    STLOCK()
        : _fInitialized( FALSE )
    {
    }

    ~STLOCK()
    {
        if ( _fInitialized )
        {
            DeleteCriticalSection( &_cs );
            CloseHandle( _hReadersDone );
        }
    }

    HRESULT
    Initialize(
        VOID
        )
    {
        HRESULT hr = S_OK;
        BOOL    fResult = FALSE;

        if ( !_fInitialized )
        {
            _fWriterWaiting = FALSE;
            _cReaders = 0;

            fResult = InitializeCriticalSectionAndSpinCount( &_cs, 100 );

            if ( !fResult )
            {
                hr = E_FAIL;
                goto Finished;
            }

            _hReadersDone = CreateEvent( NULL,
                                         FALSE,
                                         FALSE,
                                         NULL );

            if ( !_hReadersDone )
            {
                DeleteCriticalSection( &_cs );
                hr = E_FAIL;
                goto Finished;
            }

            _fInitialized = TRUE;
        }

Finished:
        return hr;
    }

    BOOL
    QueryInitialized() const
    {
        return _fInitialized;
    }
    
    void SharedAcquire()
    {
        EnterCriticalSection( &_cs );
        InterlockedIncrement( &_cReaders );
        LeaveCriticalSection( &_cs );
    }
    
    void SharedRelease()
    {
        ReleaseInternal();
    }
    
    void ExclusiveAcquire()
    {
        EnterCriticalSection( &_cs );
    
        _fWriterWaiting = TRUE;
    
        //
        // If there are any readers, wait for them
        // to release
        //

        if ( InterlockedExchangeAdd( &_cReaders, 0 ) > 0 ) 
        {
            WaitForSingleObject( _hReadersDone, INFINITE );
        }

        //
        // Reader count -1 indicates that a writer has the lock
        //

        _cReaders = -1;
    }
    
    void ExclusiveRelease()
    {
        ReleaseInternal();
    }

private:

    BOOL                _fInitialized;
    BOOL                _fWriterWaiting;
    LONG                _cReaders;
    CRITICAL_SECTION    _cs;
    HANDLE              _hReadersDone;

    void ReleaseInternal()
    {
        LONG cReaders = InterlockedDecrement( &_cReaders );
    
        if ( cReaders >= 0 )
        {
            //
            // Released a read lock.  If this was the last
            // reader and writers are waiting, set the
            // readers done event
            //

            if ( ( _fWriterWaiting ) && ( cReaders == 0 ) )
            {
                SetEvent( _hReadersDone );
            }
        }
        else 
        {
            //
            // Released a write lock
            //

            _cReaders = 0;
            _fWriterWaiting = FALSE;
            LeaveCriticalSection( &_cs );
        }
    }
};

#endif // _STLOCK_H