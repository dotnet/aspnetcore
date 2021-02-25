// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

template<typename T>
class PER_CPU
{
public:

    template<typename FunctionInitializer>
    inline
    static
    HRESULT
    Create(
        FunctionInitializer         Initializer,
        __deref_out PER_CPU<T> **   ppInstance
    );

    inline
    T *
    GetLocal(
        VOID
    );

    template<typename FunctionForEach>
    inline
    VOID
    ForEach(
        FunctionForEach Function
    );

    inline
    VOID
    Dispose(
        VOID
    );

private:

    PER_CPU(
        VOID
    )
    {
        //
        // Don't perform any operation during constructor.
        // Constructor will never be called.
        //
    }

    ~PER_CPU(
        VOID
    )
    {
        //
        // Don't perform any operation during destructor.
        // Constructor will never be called.
        //
    }

    template<typename FunctionInitializer>
    HRESULT
    Initialize(
        FunctionInitializer Initializer,
        DWORD               NumberOfVariables,
        DWORD               Alignment
    );

    T *
    GetObject(
        DWORD Index
    );

    static
    HRESULT
    GetProcessorInformation(
        __out DWORD * pCacheLineSize,
        __out DWORD * pNumberOfProcessors
    );

    //
    // Pointer to the beginning of the inlined array.
    //
    PVOID   m_pVariables;
    SIZE_T  m_Alignment;
    SIZE_T  m_VariablesCount;
};

template<typename T>
template<typename FunctionInitializer>
inline
// static
HRESULT
PER_CPU<T>::Create(
    FunctionInitializer        Initializer,
    __deref_out PER_CPU<T> **  ppInstance
)
{
    HRESULT         hr = S_OK;
    DWORD           CacheLineSize = 0;
    DWORD           ObjectCacheLineSize = 0;
    DWORD           NumberOfProcessors = 0;
    PER_CPU<T> *    pInstance = NULL;

    hr = GetProcessorInformation(&CacheLineSize,
                                 &NumberOfProcessors);
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (sizeof(T) > CacheLineSize)
    {
        //
        // Round to the next multiple of the cache line size.
        //
        ObjectCacheLineSize = (sizeof(T) + CacheLineSize-1) & (CacheLineSize-1);
    }
    else
    {
        ObjectCacheLineSize = CacheLineSize;
    }

    //
    // Calculate the size of the PER_CPU<T> object, including the array.
    // The first cache line is for the member variables and the array
    // starts in the next cache line.
    //
    SIZE_T Size = CacheLineSize + NumberOfProcessors * ObjectCacheLineSize;

    pInstance = (PER_CPU<T>*) _aligned_malloc(Size, CacheLineSize);
    if (pInstance == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }
    ZeroMemory(pInstance, Size);

    //
    // The array start in the 2nd cache line.
    //
    pInstance->m_pVariables = reinterpret_cast<PBYTE>(pInstance) + CacheLineSize;

    //
    // Pass a disposer for disposing initialized items in case of failure.
    //
    hr = pInstance->Initialize(Initializer,
                               NumberOfProcessors,
                               ObjectCacheLineSize);
    if (FAILED(hr))
    {
        goto Finished;
    }

    *ppInstance = pInstance;
    pInstance = NULL;

Finished:

    if (pInstance != NULL)
    {
        //
        // Free the instance without disposing it.
        //
        pInstance->Dispose();
        pInstance = NULL;
    }

    return hr;
}

template<typename T>
inline
T *
PER_CPU<T>::GetLocal(
    VOID
)
{
    // Use GetCurrentProcessorNumber (up to 64 logical processors) instead of
    // GetCurrentProcessorNumberEx (more than 64 logical processors) because
    // the number of processors are not densely packed per group.
    // The idea of distributing variables per CPU is to have
    // a scalability multiplier (could be NUMA node instead).
    //
    // Make sure the index don't go beyond the array size, if that happens,
    // there won't be even distribution, but still better
    // than one single variable.
    //
    return GetObject(GetCurrentProcessorNumber());
}

template<typename T>
inline
T *
PER_CPU<T>::GetObject(
    DWORD Index
)
{
    return reinterpret_cast<T*>(static_cast<PBYTE>(m_pVariables) + Index * m_Alignment);
}

template<typename T>
template<typename FunctionForEach>
inline
VOID
PER_CPU<T>::ForEach(
    FunctionForEach Function
)
{
    for(DWORD Index = 0; Index < m_VariablesCount; ++Index)
    {
        T * pObject = GetObject(Index);
        Function(pObject);
    }
}

template<typename T>
VOID
PER_CPU<T>::Dispose(
    VOID
)
{
     _aligned_free(this);
}

template<typename T>
template<typename FunctionInitializer>
inline
HRESULT
PER_CPU<T>::Initialize(
    FunctionInitializer Initializer,
    DWORD               NumberOfVariables,
    DWORD               Alignment
)
/*++

Routine Description:

    Initialize each object using the initializer function.
    If initialization for any object fails, it dispose the
    objects that were successfully initialized.

Arguments:

    Initializer - Function for initialize one object.
                  Signature: HRESULT Func(T*)
    Dispose - Function for disposing initialized objects in case of failure.
              Signature: void Func(T*)
    NumberOfVariables - The length of the array of variables.
    Alignment - Alignment to use for avoiding false sharing.

Return:

    HRESULT - E_OUTOFMEMORY

--*/
{
    HRESULT hr = S_OK;
    DWORD Index = 0;

    m_VariablesCount = NumberOfVariables;
    m_Alignment = Alignment;

    for (; Index < m_VariablesCount; ++Index)
    {
        T * pObject = GetObject(Index);
        Initializer(pObject);
    }

    return hr;
}

template<typename T>
// static
HRESULT
PER_CPU<T>::GetProcessorInformation(
    __out DWORD * pCacheLineSize,
    __out DWORD * pNumberOfProcessors
)
/*++

Routine Description:

    Gets the CPU cache-line size for the current system.
    This information is used for avoiding CPU false sharing.

Arguments:

    pCacheLineSize - The processor cache-line size.
    pNumberOfProcessors - Maximum number of processors per group.

Return:

    HRESULT - E_OUTOFMEMORY

--*/
{
    SYSTEM_INFO     SystemInfo = { };

    GetSystemInfo(&SystemInfo);
    *pNumberOfProcessors = SystemInfo.dwNumberOfProcessors;
    *pCacheLineSize = SYSTEM_CACHE_ALIGNMENT_SIZE;

    return S_OK;
}