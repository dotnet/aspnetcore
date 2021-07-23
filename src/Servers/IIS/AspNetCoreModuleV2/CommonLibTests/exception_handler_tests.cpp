// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

TEST(CaughtExceptionHResult, ReturnsOutOfMemoryForBadAlloc)
{
    HRESULT hr;
    try
    {
        throw std::bad_alloc();
    }
    catch(...)
    {
        hr = CaughtExceptionHResult();
    }

    EXPECT_EQ(E_OUTOFMEMORY, hr);
}

TEST(CaughtExceptionHResult, ReturnsValueForSystemError)
{
    HRESULT hr;
    try
    {
        throw std::system_error(E_INVALIDARG, std::system_category());
    }
    catch(...)
    {
        hr = CaughtExceptionHResult();
    }

    EXPECT_EQ(E_INVALIDARG, hr);
}

TEST(CaughtExceptionHResult, ReturnsUhandledExceptionForOtherExceptions)
{
    HRESULT hr;
    try
    {
        throw E_INVALIDARG;
    }
    catch(...)
    {
        hr = CaughtExceptionHResult();
    }

    EXPECT_EQ(HRESULT_FROM_WIN32(ERROR_UNHANDLED_EXCEPTION), hr);
}
