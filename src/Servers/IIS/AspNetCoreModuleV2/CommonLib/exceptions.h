// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <exception>
#include <system_error>

#include "debugutil.h"
#include "StringHelpers.h"
#include "InvalidOperationException.h"
#include "ntassert.h"
#include "NonCopyable.h"
#include "EventTracing.h"

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

#define _CHECK_FAILED(expr)                                     __pragma(warning(push)) \
    __pragma(warning(disable:4127)) /*disable condition is const warning*/ \
    FAILED(expr) \
    __pragma(warning(pop))

#define _HR_RET(hr)                                             __pragma(warning(push)) \
    __pragma(warning(disable:26498)) /*disable constexpr warning */ \
    const HRESULT __hrRet = hr; \
    __pragma(warning(pop))

#define RETURN_HR(hr)                                           do { _HR_RET(hr); if (_CHECK_FAILED(__hrRet)) { LogHResultFailed(LOCATION_INFO, __hrRet); } return __hrRet; } while (0, 0)
#define RETURN_LAST_ERROR()                                     do { return LogLastError(LOCATION_INFO); } while (0, 0)
#define RETURN_IF_FAILED(hr)                                    do { _HR_RET(hr); if (FAILED(__hrRet)) { LogHResultFailed(LOCATION_INFO, __hrRet); return __hrRet; }} while (0, 0)
#define RETURN_LAST_ERROR_IF(condition)                         do { if (condition) { return LogLastError(LOCATION_INFO); }} while (0, 0)
#define RETURN_LAST_ERROR_IF_NULL(ptr)                          do { if ((ptr) == nullptr) { return LogLastError(LOCATION_INFO); }} while (0, 0)

#define _GOTO_FINISHED()                                        __pragma(warning(push)) \
    __pragma(warning(disable:26438)) /*disable avoid goto warning*/ \
    goto Finished \
    __pragma(warning(pop))


#define _GOTO_FAILURE()                                        __pragma(warning(push)) \
    __pragma(warning(disable:26438)) /*disable avoid goto warning*/ \
    goto Failure \
    __pragma(warning(pop))


#define FINISHED(hrr)                                           do { _HR_RET(hrr); if (_CHECK_FAILED(__hrRet)) { LogHResultFailed(LOCATION_INFO, __hrRet); } hr = __hrRet; _GOTO_FINISHED(); } while (0, 0)
#define FINISHED_IF_FAILED(hrr)                                 do { _HR_RET(hrr); if (FAILED(__hrRet)) { LogHResultFailed(LOCATION_INFO, __hrRet); hr = __hrRet; _GOTO_FINISHED(); }} while (0, 0)
#define FINISHED_IF_NULL_ALLOC(ptr)                             do { if ((ptr) == nullptr) { hr = LogHResultFailed(LOCATION_INFO, E_OUTOFMEMORY); _GOTO_FINISHED(); }} while (0, 0)
#define FINISHED_LAST_ERROR_IF(condition)                       do { if (condition) { hr = LogLastError(LOCATION_INFO); _GOTO_FINISHED(); }} while (0, 0)
#define FINISHED_LAST_ERROR_IF_NULL(ptr)                        do { if ((ptr) == nullptr) { hr = LogLastError(LOCATION_INFO); _GOTO_FINISHED(); }} while (0, 0)

#define FAILURE(hrr)                                            do { _HR_RET(hrr); if (_CHECK_FAILED(__hrRet)) { LogHResultFailed(LOCATION_INFO, __hrRet); } hr = __hrRet; _GOTO_FAILURE(); } while (0, 0)
#define FAILURE_IF_FAILED(hrr)                                  do { _HR_RET(hrr); if (FAILED(__hrRet)) { LogHResultFailed(LOCATION_INFO, __hrRet); hr = __hrRet; _GOTO_FAILURE(); }} while (0, 0)
#define FAILURE_IF_NULL_ALLOC(ptr)                              do { if ((ptr) == nullptr) { hr = LogHResultFailed(LOCATION_INFO, E_OUTOFMEMORY); _GOTO_FAILURE(); }} while (0, 0)
#define FAILURE_LAST_ERROR_IF(condition)                        do { if (condition) { hr = LogLastError(LOCATION_INFO); _GOTO_FAILURE(); }} while (0, 0)
#define FAILURE_LAST_ERROR_IF_NULL(ptr)                         do { if ((ptr) == nullptr) { hr = LogLastError(LOCATION_INFO); _GOTO_FAILURE(); }} while (0, 0)

#define THROW_HR(hr)                                            do { _HR_RET(hr); ThrowResultException(LOCATION_INFO, LogHResultFailed(LOCATION_INFO, __hrRet)); } while (0, 0)
#define THROW_LAST_ERROR()                                      do { ThrowResultException(LOCATION_INFO, LogLastError(LOCATION_INFO)); } while (0, 0)
#define THROW_IF_FAILED(hr)                                     do { _HR_RET(hr); if (FAILED(__hrRet)) { ThrowResultException(LOCATION_INFO, __hrRet); }} while (0, 0)
#define THROW_LAST_ERROR_IF(condition)                          do { if (condition) { ThrowResultException(LOCATION_INFO, LogLastError(LOCATION_INFO)); }} while (0, 0)
#define THROW_LAST_ERROR_IF_NULL(ptr)                           do { if ((ptr) == nullptr) { ThrowResultException(LOCATION_INFO, LogLastError(LOCATION_INFO)); }} while (0, 0)

#define THROW_IF_NULL_ALLOC(ptr)                                Throw_IfNullAlloc(ptr)

#define CATCH_RETURN()                                          catch (...) { RETURN_CAUGHT_EXCEPTION(); }
#define LOG_IF_FAILED(hr)                                       LogHResultFailed(LOCATION_INFO, hr)
#define LOG_LAST_ERROR()                                        LogLastErrorIf(LOCATION_INFO, true)
#define LOG_LAST_ERROR_IF(condition)                            LogLastErrorIf(LOCATION_INFO, condition)
#define SUCCEEDED_LOG(hr)                                       SUCCEEDED(LOG_IF_FAILED(hr))
#define FAILED_LOG(hr)                                          FAILED(LOG_IF_FAILED(hr))

#define RETURN_INT_IF_NOT_ZERO(val)                                 do { if ((val) != 0) { return val; }} while (0, 0)
#define RETURN_IF_NOT_ZERO(val)                                 do { if ((val) != 0) { return; }} while (0, 0)

inline thread_local IHttpTraceContext* g_traceContext;

 __declspec(noinline) inline VOID TraceHRESULT(LOCATION_ARGUMENTS HRESULT hr)
{
    ::RaiseEvent<ANCMEvents::ANCM_HRESULT_FAILED>(g_traceContext, nullptr, fileName, lineNumber, hr);
}

 __declspec(noinline) inline VOID TraceException(LOCATION_ARGUMENTS const std::exception& exception)
{
     ::RaiseEvent<ANCMEvents::ANCM_EXCEPTION_CAUGHT>(g_traceContext, nullptr, fileName, lineNumber, exception.what());
}

class ResultException: public std::runtime_error
{
public:
    ResultException(HRESULT hr, LOCATION_ARGUMENTS_ONLY) :
        runtime_error(format("HRESULT 0x%x returned at " LOCATION_FORMAT, hr, LOCATION_CALL_ONLY)),
        m_hr(hr)
    {
    }

    HRESULT GetResult() const noexcept { return m_hr; }

private:

#pragma warning( push )
#pragma warning ( disable : 26495 ) // bug in CA: m_hr is reported as uninitialized
    const HRESULT m_hr = S_OK;
};
#pragma warning( pop )

 __declspec(noinline) inline VOID ReportUntypedException(LOCATION_ARGUMENTS_ONLY)
{
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR, LOCATION_FORMAT "Unhandled non-standard exception", LOCATION_CALL_ONLY);
}

 __declspec(noinline) inline HRESULT LogLastError(LOCATION_ARGUMENTS_ONLY)
{
    const auto lastError = GetLastError();
    const auto hr = HRESULT_FROM_WIN32(lastError);

    TraceHRESULT(LOCATION_CALL hr);
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR, LOCATION_FORMAT "Operation failed with LastError: %d HR: 0x%x", LOCATION_CALL lastError, hr);

    return hr;
}

 __declspec(noinline) inline bool LogLastErrorIf(LOCATION_ARGUMENTS_ONLY, bool condition)
{
    if (condition)
    {
        LogLastError(LOCATION_CALL_ONLY);
    }

    return condition;
}

 __declspec(noinline) inline VOID ReportException(LOCATION_ARGUMENTS const InvalidOperationException& exception)
{
    TraceException(LOCATION_CALL exception);
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR, "InvalidOperationException '%ls' caught at " LOCATION_FORMAT, exception.as_wstring().c_str(), LOCATION_CALL_ONLY);
}

 __declspec(noinline) inline VOID ReportException(LOCATION_ARGUMENTS const std::exception& exception)
{
    TraceException(LOCATION_CALL exception);
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR, "Exception '%s' caught at " LOCATION_FORMAT, exception.what(), LOCATION_CALL_ONLY);
}

 __declspec(noinline) inline HRESULT LogHResultFailed(LOCATION_ARGUMENTS HRESULT hr)
{
    if (FAILED(hr))
    {
        TraceHRESULT(LOCATION_CALL hr);
        DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR,  "Failed HRESULT returned: 0x%x at " LOCATION_FORMAT, hr, LOCATION_CALL_ONLY);
    }
    return hr;
}

 __declspec(noinline) inline HRESULT LogHResultFailed(LOCATION_ARGUMENTS const std::error_code& error_code)
{
    if (error_code)
    {
        TraceHRESULT(LOCATION_CALL error_code.value());
        DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR,  "Failed error_code returned: 0x%x 0xs at " LOCATION_FORMAT, error_code.value(), error_code.message().c_str(), LOCATION_CALL_ONLY);
        return E_FAIL;
    }
    return ERROR_SUCCESS;
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
    catch (const ResultException& exception)
    {
        ReportException(LOCATION_CALL exception);
        return exception.GetResult();
    }
    catch (const InvalidOperationException& exception)
    {
        ReportException(LOCATION_CALL exception);
        return HRESULT_FROM_WIN32(ERROR_UNHANDLED_EXCEPTION);
    }
    catch (const std::exception& exception)
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

__declspec(noinline) inline std::wstring CaughtExceptionToString()
{
    try
    {
        throw;
    }
    catch (const InvalidOperationException& exception)
    {
        return exception.as_wstring();
    }
    catch (const std::system_error& exception)
    {
        return to_wide_string(exception.what(), CP_ACP);
    }
    catch (const std::exception& exception)
    {
        return to_wide_string(exception.what(), CP_ACP);
    }
    catch (...)
    {
        return L"Unknown exception type";
    }
}

[[noreturn]]
 __declspec(noinline) inline void ThrowResultException(LOCATION_ARGUMENTS HRESULT hr)
{
    DebugPrintf(ASPNETCORE_DEBUG_FLAG_ERROR,  "Throwing ResultException for HRESULT 0x%x at " LOCATION_FORMAT, hr, LOCATION_CALL_ONLY);
    throw ResultException(hr, LOCATION_CALL_ONLY);
}

template <typename PointerT> auto Throw_IfNullAlloc(PointerT pointer)
{
    if (pointer == nullptr)
    {
        throw std::bad_alloc();
    }
    return pointer;
}
__declspec(noinline) inline std::wstring GetUnexpectedExceptionMessage(const std::runtime_error& ex)
{
    return format(L"Unexpected exception: %S", ex.what());
}

class TraceContextScope: NonCopyable
{
public:
    TraceContextScope(IHttpTraceContext* pTraceContext) noexcept
    {
        m_pPreviousTraceContext = g_traceContext;
        g_traceContext = pTraceContext;
    }

    ~TraceContextScope()
    {
        g_traceContext = m_pPreviousTraceContext;
    }
 private:
     IHttpTraceContext* m_pPreviousTraceContext;
};

