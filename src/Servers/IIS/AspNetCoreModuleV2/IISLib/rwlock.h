// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#if (_WIN32_WINNT < 0x600)

//
// XP implementation.
//
class CWSDRWLock
{
public:

    CWSDRWLock()
        : m_bInited(FALSE)
    {
    }

    ~CWSDRWLock()
    {
        if (m_bInited)
        {
            DeleteCriticalSection(&m_rwLock.critsec);
            CloseHandle(m_rwLock.ReadersDoneEvent);
        }
    }

    BOOL QueryInited() const
    {
        return m_bInited;
    }

    HRESULT Init()
    {
        HRESULT hr = S_OK;
    
        if (FALSE == m_bInited)
        {
            m_rwLock.fWriterWaiting = FALSE;
            m_rwLock.LockCount = 0;
            if ( !InitializeCriticalSectionAndSpinCount( &m_rwLock.critsec, 0 )) 
            {
                DWORD dwError  = GetLastError();
                hr = HRESULT_FROM_WIN32(dwError);
                return hr;
            }

            m_rwLock.ReadersDoneEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
            if( NULL == m_rwLock.ReadersDoneEvent ) 
            {
                DWORD dwError  = GetLastError();
                hr = HRESULT_FROM_WIN32(dwError);
                DeleteCriticalSection(&m_rwLock.critsec);
                return hr;
            }
            m_bInited = TRUE;
        }

        return hr;
    }
    
    void SharedAcquire()
    {
        EnterCriticalSection(&m_rwLock.critsec);
        InterlockedIncrement(&m_rwLock.LockCount);
        LeaveCriticalSection(&m_rwLock.critsec);
    }
    
    void SharedRelease()
    {
        ReleaseRWLock();
    }
    
    void ExclusiveAcquire()
    {
        EnterCriticalSection( &m_rwLock.critsec );
    
        m_rwLock.fWriterWaiting = TRUE;
    
        // check if there are any readers active
        if ( InterlockedExchangeAdd( &m_rwLock.LockCount, 0 ) > 0 ) 
        {
            //
            // Wait for all the readers to get done..
            //
            WaitForSingleObject( m_rwLock.ReadersDoneEvent, INFINITE );
        }
        m_rwLock.LockCount = -1;
    }
    
    void ExclusiveRelease()
    {
        ReleaseRWLock();
    }

private:

    BOOL m_bInited;

    typedef struct _RW_LOCK 
    {
        BOOL  fWriterWaiting; // Is a writer waiting on the lock?
        LONG LockCount;
        CRITICAL_SECTION critsec;
        HANDLE ReadersDoneEvent;
    } RW_LOCK, *PRW_LOCK;

    RW_LOCK m_rwLock;

private:

    void ReleaseRWLock()
    {
        LONG Count = InterlockedDecrement( &m_rwLock.LockCount );
    
        if ( 0 <= Count )
        {
            // releasing a read lock
            if (( m_rwLock.fWriterWaiting ) && ( 0 == Count ))
            {
                SetEvent( m_rwLock.ReadersDoneEvent );
            }
        }
        else 
        {
            // Releasing a write lock
            m_rwLock.LockCount = 0;
            m_rwLock.fWriterWaiting = FALSE;
            LeaveCriticalSection(&m_rwLock.critsec);
        }
    }
};

#else

//
// Implementation for Windows Vista or greater.
//
class CWSDRWLock
{
public:

    CWSDRWLock()
    {
        InitializeSRWLock(&m_rwLock);
    }

    BOOL QueryInited()
    {
        return TRUE;
    }


    HRESULT Init()
    {
        //
        // Method defined to keep compatibility with CWSDRWLock class for XP.
        //
        return S_OK;
    }

    _Acquires_shared_lock_(this->m_rwLock)
    void SharedAcquire()
    {
        AcquireSRWLockShared(&m_rwLock);
    }

    _Releases_shared_lock_(this->m_rwLock)
    void SharedRelease()
    {
        ReleaseSRWLockShared(&m_rwLock);
    }

    _Acquires_exclusive_lock_(this->m_rwLock)
    void ExclusiveAcquire()
    {
        AcquireSRWLockExclusive(&m_rwLock);
    }

    _Releases_exclusive_lock_(this->m_rwLock)
    void ExclusiveRelease()
    {
        ReleaseSRWLockExclusive(&m_rwLock);
    }

private:

    SRWLOCK m_rwLock;
};

#endif

//
// Rename the lock class to a more clear name.
//
typedef CWSDRWLock READ_WRITE_LOCK;