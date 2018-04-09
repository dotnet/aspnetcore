// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#ifndef _MACROS_H
#define _MACROS_H

//
// The DIFF macro should be used around an expression involving pointer
// subtraction. The expression passed to DIFF is cast to a size_t type,
// allowing the result to be easily assigned to any 32-bit variable or
// passed to a function expecting a 32-bit argument.
//

#define DIFF(x)     ((size_t)(x))

// Change a hexadecimal digit to its numerical equivalent
#define TOHEX( ch )                                     \
    ((ch) > L'9' ?                                      \
        (ch) >= L'a' ?                                  \
            (ch) - L'a' + 10 :                          \
            (ch) - L'A' + 10                            \
        : (ch) - L'0')


// Change a number to its Hexadecimal equivalent

#define TODIGIT( nDigit )                               \
     (CHAR)((nDigit) > 9 ?                              \
          (nDigit) - 10 + 'A'                           \
        : (nDigit) + '0')


inline int
SAFEIsSpace(UCHAR c)
{
    return isspace( c );
}

inline int
SAFEIsAlNum(UCHAR c)
{
    return isalnum( c );
}

inline int
SAFEIsAlpha(UCHAR c)
{
    return isalpha( c );
}

inline int
SAFEIsXDigit(UCHAR c)
{
    return isxdigit( c );
}

inline int
SAFEIsDigit(UCHAR c)
{
    return isdigit( c );
}

#define __RETURN_GLE_FAIL(str)                      return HRESULT_FROM_WIN32(GetLastError());
#define __RETURN_HR_FAIL(hr, str)                   do { HRESULT __hr = (hr); return __hr; } while (0, 0)

#define RETURN_IF_FAILED(hr)                        do { HRESULT __hrRet = hr; if (FAILED(__hrRet)) { __RETURN_HR_FAIL(__hrRet, #hr); }} while (0, 0)
#define RETURN_LAST_ERROR_IF_NULL(ptr)              do { if ((ptr) == nullptr) { __RETURN_GLE_FAIL(#ptr); }} while (0, 0)
#define RETURN_IF_HANDLE_INVALID(handle)            do { HANDLE __hRet = (handle); if (__hRet == INVALID_HANDLE_VALUE) { __RETURN_GLE_FAIL(#handle); }} while (0, 0)

#endif // _MACROS_H
