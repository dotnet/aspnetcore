// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.hxx"
#include "memorylog.hxx"
#include "pudebug.h"

CMemoryLog::CMemoryLog(DWORD dwMaxByteSize) :
  m_fValid(FALSE),
  m_fCritSecInitialized(FALSE)
{
    BOOL fRet;
    
    fRet = m_buf.Resize(dwMaxByteSize);
    if (fRet)
    {
        m_fValid = TRUE;
    }

    m_pBufferBegin = (CHAR*) m_buf.QueryPtr();
    m_pLastMessageEnd = (CHAR*) m_buf.QueryPtr();
    m_pBufferEnd = ((CHAR*) m_buf.QueryPtr()) + m_buf.QuerySize();

    fRet = InitializeCriticalSectionAndSpinCount(&m_cs, 
                                0x80000000 /* precreate event */ | 
                                IIS_DEFAULT_CS_SPIN_COUNT );
    if (FALSE != fRet)
    {
        m_fCritSecInitialized = TRUE;
    }

}

CMemoryLog::~CMemoryLog()
{
    m_pBufferBegin = NULL;
    m_pLastMessageEnd = NULL;
    m_pBufferEnd = NULL;
    m_fValid = FALSE;

    if (m_fCritSecInitialized)
    {
        DeleteCriticalSection(&m_cs);
        m_fCritSecInitialized = FALSE;
    }
}

//
// Appends to end of the circular memory log.  
// 
DWORD
CMemoryLog::Append(LPCSTR pszOutput,
                   DWORD cchLen
                  )
{
    // make sure internal state can accept this request
    if (FALSE == m_fValid ||
        FALSE == m_fCritSecInitialized )
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }

    // make sure that we won't think we need less 
    // memory than we do.  We are going to add 1 to
    // this value next, so if it is MAX_UINT then
    // we will wrap on the add. Don't allow strings
    // that are that long.
    if ( (ULONGLONG)cchLen + 1 > MAXDWORD )
    {
        return ERROR_ARITHMETIC_OVERFLOW;
    }

    // make sure the string length will fit inside the buffer
    if ( cchLen + 1 > m_buf.QuerySize())
    {
        return ERROR_NOT_ENOUGH_MEMORY;
    }

    CHAR * pWhereToWriteMessage = NULL;

    // need to synchronize access to m_pLastMessageEnd
    EnterCriticalSection(&m_cs);

    // check if the new message will fit into the remaining space in the buffer
    // previous end (+1) + new length + 1 for NULL
    if (m_pLastMessageEnd + cchLen + 1 < m_pBufferEnd)
    {
        // it will fit in remaining space
        pWhereToWriteMessage = m_pLastMessageEnd;
    }
    else
    {
        // start over at the beginning
        pWhereToWriteMessage = (CHAR*)m_buf.QueryPtr();

        // don't leave extra old goo sitting around in the buffer
        ZeroMemory(m_pLastMessageEnd, m_pBufferEnd - m_pLastMessageEnd);
    }
    
    // set end of message to pWhere + length + 1 for NULL
    m_pLastMessageEnd = pWhereToWriteMessage + cchLen + 1;

    LeaveCriticalSection(&m_cs);

    // the following memcpy is outside of the criticalsection - 
    // this introduces a race between leaving the criticalsection and 
    // looping back through the buffer before we finish writing.
    // how likely is this?  Not very.
    //
    // In addition - moving the memcpy inside of the critsec makes the time spent
    // quite a bit larger than some simple load/stores that are currently there.
    // 
    // Plus this is a debugging aid - life isn't fair.

    // actually do the copy
    memcpy(pWhereToWriteMessage, pszOutput, cchLen);

    // write out a NULL to indicate end of message
    *(pWhereToWriteMessage + cchLen) = NULL;

    return NO_ERROR;
}

