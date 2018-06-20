// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <system_error>

#include "debugutil.h"

inline VOID ReportUntypedException()
{
    DebugPrint(ASPNETCORE_DEBUG_FLAG_ERROR, "Unhandled non-standard exception");
}

inline VOID ReportException(std::exception& exception)
{
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR, "Unhandled exception: %s", exception.what());
}

inline __declspec(noinline) HRESULT CaughtExceptionHResult()
{
    try
    {
        throw;
    }
    catch (const std::bad_alloc&)
    {
        return E_OUTOFMEMORY;
    }
    catch (std::system_error& exception)
    {
        ReportException(exception);
        return exception.code().value();
    }
    catch (std::exception& exception)
    {
        ReportException(exception);
        return HRESULT_FROM_WIN32(ERROR_UNHANDLED_EXCEPTION);
    }
    catch (...)
    {
        ReportUntypedException();
        return HRESULT_FROM_WIN32(ERROR_UNHANDLED_EXCEPTION);
    }
}

template <typename PointerT> auto Throw_IfNullAlloc(PointerT pointer)
{
    if (pointer == nullptr)
    {
        throw std::bad_alloc();
    }
    return pointer;
}


#define RETURN_CAUGHT_EXCEPTION()                               return CaughtExceptionHResult();
#define CATCH_RETURN()                                          catch (...) { RETURN_CAUGHT_EXCEPTION(); }
#define THROW_IF_NULL_ALLOC(ptr)                                Throw_IfNullAlloc(ptr)
