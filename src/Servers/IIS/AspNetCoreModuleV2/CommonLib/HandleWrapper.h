// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <Windows.h>
#include "ntassert.h"

struct InvalidHandleTraits
{
    using HandleType = HANDLE;
    static constexpr HANDLE DefaultHandle = nullptr;
    static void Close(HANDLE handle) noexcept { CloseHandle(handle); }
};

struct NullHandleTraits
{
    using HandleType = HANDLE;
    static constexpr HANDLE DefaultHandle = nullptr;
    static void Close(HANDLE handle) noexcept { CloseHandle(handle); }
};

struct ModuleHandleTraits
{
    using HandleType = HMODULE;
    static constexpr HMODULE DefaultHandle = nullptr;
    static void Close(HMODULE handle) noexcept { FreeModule(handle); }
};

// Code analysis doesn't like nullptr usages via traits
#pragma warning(push)
#pragma warning(disable : 26477) // disable  Use 'nullptr' rather than 0 or NULL (es.47).

template <typename traits>
class HandleWrapper
{
public:
    using HandleType = typename traits::HandleType;

    HandleWrapper(HandleType handle = traits::DefaultHandle) noexcept : m_handle(handle) {}
    ~HandleWrapper()
    {
        if (m_handle != traits::DefaultHandle)
        {
            traits::Close(m_handle);
        }
    }

    operator HandleType() noexcept { return m_handle; }
    HandleWrapper &operator=(HandleType value) noexcept
    {
        DBG_ASSERT(m_handle == traits::DefaultHandle);
        m_handle = value;
        return *this;
    }

    HandleType *operator&() noexcept { return &m_handle; }

    HandleType release() noexcept
    {
        auto value = m_handle;
        m_handle = traits::DefaultHandle;
        return value;
    }

private:
    HandleType m_handle;
};

#pragma warning(pop)
