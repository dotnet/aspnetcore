// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <crtdbg.h>
#include <CodeAnalysis/Warnings.h>

//
// BUFFER_T class shouldn't be used directly. Use BUFFER specialization class instead.
// The only BUFFER_T partners are STRU and STRA classes.
// BUFFER_T cannot hold other but primitive types since it doesn't call
// constructor and destructor.
//
// Note: Size is in bytes.
//
template<typename T, DWORD LENGTH>
class BUFFER_T
{
public:

    BUFFER_T()
      : m_cbBuffer( sizeof(m_rgBuffer) ),
        m_fHeapAllocated( false ),
        m_pBuffer(m_rgBuffer)
    /*++
        Description:

            Default constructor where the inline buffer is used.

        Arguments:

            None.

        Returns:
            
            None.

    --*/    
    {
    }

    BUFFER_T(
        __inout_bcount(cbInit) T* pbInit, 
        __in DWORD cbInit
    ) : m_pBuffer( pbInit ),
        m_cbBuffer( cbInit ),
        m_fHeapAllocated( false )
    /*++
        Description:

            Instantiate BUFFER, initially using pbInit as buffer
            This is useful for stack-buffers and inline-buffer class members 
            (see STACK_BUFFER and INLINE_BUFFER_INIT below)

            BUFFER does not free pbInit.

        Arguments:

            pbInit - Initial buffer to use.
            cbInit - Size of pbInit in bytes (not in elements).

        Returns:
            
            None.

    --*/
    {
        _ASSERTE(nullptr != pbInit );
        _ASSERTE( cbInit > 0 );
    }

    ~BUFFER_T()
    {
        if( IsHeapAllocated() )
        {
            _ASSERTE( nullptr != m_pBuffer );
            HeapFree( GetProcessHeap(), 0, m_pBuffer );
            m_pBuffer = nullptr;
            m_cbBuffer = 0;
            m_fHeapAllocated = false;
        }
    }

    T*
    QueryPtr(
        VOID
    ) const
    {
        //
        // Return pointer to data buffer.
        //
        return m_pBuffer;
    }

    DWORD 
    QuerySize(
        VOID
    ) const  
    { 
        //
        // Return number of bytes.
        //
        return m_cbBuffer; 
    }

    __success(return == true)
    bool 
    Resize(
        const SIZE_T   cbNewSize,
        const bool     fZeroMemoryBeyondOldSize = false
    )
    /*++
        Description:

            Resizes the buffer.

        Arguments:

            cbNewSize   - Size in bytes to grow to.
            fZeroMemoryBeyondOldSize 
                        - Whether to zero the region of memory of the
                          new buffer beyond the original size.

        Returns:
            
            TRUE on success, FALSE on failure.

    --*/
    {
        PVOID pNewMem = nullptr;

        if ( cbNewSize <= m_cbBuffer )
        {
            return true;
        }

        if ( cbNewSize > MAXDWORD )
        {
            SetLastError( ERROR_INVALID_PARAMETER );
            return false;
        }

        DWORD dwHeapAllocFlags = fZeroMemoryBeyondOldSize ? HEAP_ZERO_MEMORY : 0;

        if( IsHeapAllocated() )
        {
            pNewMem = HeapReAlloc( GetProcessHeap(), dwHeapAllocFlags, m_pBuffer, cbNewSize );
        }
        else
        {
            pNewMem = HeapAlloc( GetProcessHeap(), dwHeapAllocFlags, cbNewSize );
        }

        if( pNewMem == nullptr )
        {
            SetLastError( ERROR_NOT_ENOUGH_MEMORY );
            return false;
        }

        if( !IsHeapAllocated() ) 
        {
            //
            // First time this block is allocated. Copy over old contents.
            //
            memcpy_s( pNewMem, static_cast<DWORD>(cbNewSize), m_pBuffer, m_cbBuffer );
            m_fHeapAllocated = true;
        }

        m_pBuffer = reinterpret_cast<T*>(pNewMem);
        m_cbBuffer = static_cast<DWORD>(cbNewSize);

        _ASSERTE( m_pBuffer != NULL );

        return true;
    }

private:

    bool 
    IsHeapAllocated(
        VOID
    ) const 
    {   
        return m_fHeapAllocated; 
    }

    //
    // The default inline buffer.
    // This member should be at the beginning for alignment purposes.
    //
    T       m_rgBuffer[LENGTH];
    
    //
    // Is m_pBuffer dynamically allocated?
    //
    bool    m_fHeapAllocated;

    //
    // Size of the buffer as requested by client in bytes.
    //
    DWORD   m_cbBuffer;

    //
    // Pointer to buffer.
    //
    __field_bcount_full(m_cbBuffer)
    T*      m_pBuffer;
};

//
// Resizes the buffer by 2 if the ideal size is bigger
// than the buffer length. That give us lg(n) allocations.
//
// Use template inferring like:
//
//   BUFFER buff;
//   hr = ResizeBufferByTwo(buff, 100);
//
template<typename T, DWORD LENGTH>
HRESULT
ResizeBufferByTwo(
    BUFFER_T<T,LENGTH>& Buffer,
    SIZE_T              cbIdealSize,
    bool                fZeroMemoryBeyondOldSize = false
)
{
    if (cbIdealSize > Buffer.QuerySize())
    {
        if (!Buffer.Resize(max(cbIdealSize, static_cast<SIZE_T>(Buffer.QuerySize() * 2)),
                           fZeroMemoryBeyondOldSize))
        {
            return E_OUTOFMEMORY;
        }
    }
    return S_OK;
}
    

//
//
// Lots of code uses BUFFER class to store a bunch of different
// structures, so m_rgBuffer needs to be 8 byte aligned when it is used
// as an opaque buffer.
//
#define INLINED_BUFFER_LEN 32
typedef BUFFER_T<BYTE, INLINED_BUFFER_LEN> BUFFER;

//
// Assumption of macros below for pointer alignment purposes
//
C_ASSERT( sizeof(VOID*) <= sizeof(ULONGLONG) );

//
//  Declare a BUFFER that will use stack memory of <size>
//  bytes. If the buffer overflows then a heap buffer will be allocated.
//
#define STACK_BUFFER( _name, _size )    \
    ULONGLONG   __aqw##_name[ ( ( (_size) + sizeof(ULONGLONG) - 1 ) / sizeof(ULONGLONG) ) ]{}; \
    BUFFER      _name( (BYTE*)__aqw##_name, sizeof(__aqw##_name) )

//
// Macros for declaring and initializing a BUFFER that will use inline memory
// of <size> bytes as a member of an object.
//
#define INLINE_BUFFER( _name, _size )   \
    ULONGLONG   __aqw##_name[ ( ( (_size) + sizeof(ULONGLONG) - 1 ) / sizeof(ULONGLONG) ) ]; \
    BUFFER      _name;

#define INLINE_BUFFER_INIT( _name )     \
    _name( (BYTE*)__aqw##_name, sizeof( __aqw##_name ) )
