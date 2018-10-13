// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

template<typename TYPE, SIZE_T SIZE>
class HYBRID_ARRAY
{
public:

    HYBRID_ARRAY(
        VOID
    ) : m_pArray(m_InlineArray), 
        m_Capacity(ARRAYSIZE(m_InlineArray))
    {
    }

    ~HYBRID_ARRAY()
    {
        if ( !QueryUsesInlineArray() )
        {
            delete [] m_pArray;
            m_pArray = NULL;
        }
    }

    SIZE_T
    QueryCapacity(
        VOID
    ) const
    {
        //
        // Number of elements available in the array.
        //
        return m_Capacity;
    }

    TYPE *
    QueryArray(
        VOID
    ) const
    {
        //
        // Raw pointer to the current array.
        //
        return m_pArray;
    }

    TYPE &
    QueryItem(
        __in const SIZE_T Index
    )
    {
        //
        // Gets the array item giving the index.
        //
        return m_pArray[Index];
    }

    TYPE &
    operator [] (const SIZE_T Index)
    {
        //
        // Operator override for convenience.
        // Please don't add other overloads like '++' '--'
        // in order to keep it simple.
        //
        return m_pArray[Index];
    }

    const TYPE &
    operator [] (const SIZE_T Index) const
    {
        return m_pArray[Index];
    }

    template<SIZE_T SourceSize>
    HRESULT
    Copy( 
        __in TYPE const (&SourceArray)[SourceSize],
        __in bool   fHasTrivialAssign = false
    )
    /*++

    Routine Description:

        Copies a source array like:
        
          int source[] = { 1, 2, 3 };
          hr = hybridArray.Copy( source );

        It will statically determinate the length of the source array.

    Arguments:

        SourceArray - The array to copy.
        SourceSize - The number of array elements.
        fHasTrivialAssign - True if safe to perform buffer copy.

    Return Value:

        HRESULT

    --*/
    {
        return Copy( SourceArray, SourceSize, fHasTrivialAssign );
    }

    HRESULT
    Copy( 
        __in_ecount(SourceSize)
        const TYPE *        pSourceArray,
        __in const SIZE_T   SourceSize,
        __in bool           fHasTrivialAssign = false
    )
    /*++

    Routine Description:

        Copies a source array.

    Arguments:

        pSourceArray - The array to copy.
        SourceSize - The number of array elements.
        fHasTrivialAssign - True if safe to perform buffer copy.

    Return Value:

        HRESULT

    --*/
    {
        HRESULT hr;

        hr = EnsureCapacity( SourceSize,
                             FALSE,     // fCopyPrevious 
                             FALSE );   // fHasTrivialAssign 
        if ( FAILED( hr ) )
        {
            return hr;
        }

        if ( fHasTrivialAssign ) // Future Work: use std::tr1::has_trivial_assign
        {
            CopyMemory(m_pArray, pSourceArray, m_Capacity * sizeof(TYPE));
        }
        else
        {
            for ( SIZE_T Index = 0; Index < SourceSize; ++Index )
            {
                m_pArray[Index] = pSourceArray[Index];
            }
        }

        return S_OK;
    }

    HRESULT
    EnsureCapacity(
        __in const SIZE_T   MinimumCapacity,
        __in bool           fCopyPrevious,
        __in bool           fHasTrivialAssign = false
    )
    /*++

    Routine Description:

        Copies a source array.

    Arguments:

        MinimumCapacity   - The expected length of the array.
        fCopyPrevious     - Must be always explicit parameter.
                            True if copy previous array data.
        fHasTrivialAssign - True if safe to perform buffer copy.

    Return Value:

        HRESULT

    --*/
    {
        //
        // Caller is responsible for calculating a length that won't cause
        // too many reallocations in the future.
        //

        if ( MinimumCapacity <= ARRAYSIZE(m_InlineArray) )
        {
            return S_OK;
        }

        TYPE * pNewArray;

        pNewArray = new TYPE[ MinimumCapacity ];
        if ( pNewArray == NULL )
        {
            return E_OUTOFMEMORY;
        }

        if ( fCopyPrevious )
        {
            if ( fHasTrivialAssign )
            {
                CopyMemory(pNewArray, m_pArray, m_Capacity * sizeof(TYPE));
            }
            else
            {
                for ( SIZE_T Index = 0; Index < m_Capacity; ++Index )
                {
                    pNewArray[Index] = m_pArray[Index];
                }
            }
        }

        if ( QueryUsesInlineArray() )
        {
            m_pArray = pNewArray;
        }
        else
        {
            delete [] m_pArray;
            m_pArray = pNewArray;
        }

        m_Capacity = MinimumCapacity;

        return S_OK;
    }

private:

    bool
    QueryUsesInlineArray(
        VOID
    ) const
    {
        return m_pArray == m_InlineArray;
    }

    TYPE    m_InlineArray[SIZE];
    TYPE *  m_pArray;
    SIZE_T  m_Capacity;
};