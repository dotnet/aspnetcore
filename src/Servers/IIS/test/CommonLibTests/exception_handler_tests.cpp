// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
