// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <system_error>

#include "debugutil.h"

#define LOCATION_INFO_ENABLED TRUE

#if LOCATION_INFO_ENABLED
#define LOCATION_FORMAT                                         "%s:%d "
#define LOCATION_ARGUMENTS_ONLY                                 _In_opt_ PCSTR fileName, unsigned int lineNumber
#define LOCATION_ARGUMENTS                                      LOCATION_ARGUMENTS_ONLY,
#define LOCATION_CALL_ONLY                                      fileName, lineNumber
#define LOCATION_CALL                                           LOCATION_CALL_ONLY,
#define LOCATION_INFO                                           __FILE__, __LINE__
#else
#define LOCATION_FORMAT
#define LOCATION_ARGUMENTS_ONLY
#define LOCATION_ARGUMENTS
#define LOCATION_CALL_ONLY
#define LOCATION_CALL
#define LOCATION_INFO
#endif

#define OBSERVE_CAUGHT_EXCEPTION()                              CaughtExceptionHResult(LOCATION_INFO);
#define RETURN_CAUGHT_EXCEPTION()                               return CaughtExceptionHResult(LOCATION_INFO);
#define CATCH_RETURN()                                          catch (...) { RETURN_CAUGHT_EXCEPTION(); }
#define THROW_IF_NULL_ALLOC(ptr)                                Throw_IfNullAlloc(ptr)
#define RETURN_IF_FAILED(hr)                                    do { HRESULT __hrRet = hr; if (FAILED(__hrRet)) { LogHResultFailed(LOCATION_INFO, __hrRet); return __hrRet; }} while (0, 0)
#define FINISHED_IF_FAILED(hrr)                                 do { HRESULT __hrRet = hrr; if (FAILED(__hrRet)) { LogHResultFailed(LOCATION_INFO, __hrRet); hr = __hrRet; goto Finished; }} while (0, 0)
#define LOG_IF_FAILED(hr)                                       LogHResultFailed(LOCATION_INFO, hr)
#define SUCCEEDED_LOG(hr)                                       SUCCEEDED(LOG_IF_FAILED(hr))
#define FAILED_LOG(hr)                                          FAILED(LOG_IF_FAILED(hr))

 __declspec(noinline) inline VOID ReportUntypedException(LOCATION_ARGUMENTS_ONLY)
{
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR, LOCATION_FORMAT "Unhandled non-standard exception", LOCATION_CALL_ONLY);
}

 __declspec(noinline) inline VOID ReportException(LOCATION_ARGUMENTS std::exception& exception)
{
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR, LOCATION_FORMAT "Unhandled exception: %s", LOCATION_CALL exception.what());
}

 __declspec(noinline) inline HRESULT LogHResultFailed(LOCATION_ARGUMENTS HRESULT hr)
{
    if (FAILED(hr))
    {
        DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR,  "Failed HRESULT returned: 0x%x at " LOCATION_FORMAT, hr, LOCATION_CALL_ONLY);
    }
    return hr;
}

__declspec(noinline) inline HRESULT CaughtExceptionHResult(LOCATION_ARGUMENTS_ONLY)
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
        ReportException(LOCATION_CALL exception);
        return exception.code().value();
    }
    catch (std::exception& exception)
    {
        ReportException(LOCATION_CALL exception);
        return HRESULT_FROM_WIN32(ERROR_UNHANDLED_EXCEPTION);
    }
    catch (...)
    {
        ReportUntypedException(LOCATION_CALL_ONLY);
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
