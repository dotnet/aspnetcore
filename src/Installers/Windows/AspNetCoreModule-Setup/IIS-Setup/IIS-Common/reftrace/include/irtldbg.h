// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef __IRTLDBG_H__
#define __IRTLDBG_H__

#ifndef __IRTLMISC_H__
# include <irtlmisc.h>
#endif

/* Ensure that MessageBoxes can popup */
# define RUNNING_AS_SERVICE 1


#ifdef _AFX
  /* Assure compatiblity with MFC */

# define IRTLASSERT(x)     ASSERT(x)
# define IRTLVERIFY(x)     VERIFY(x)

#else /* !_AFX */

# if DBG || defined(_DEBUG)
#  define IRTLDEBUG
# endif

# if defined(IRTLDEBUG)
#  ifndef USE_DEBUG_CRTS
    /* IIS (and NT) do not ship msvcrtD.dll, per the VC license,
     * so we can't use the assertion code from <crtdbg.h>.  Use similar
     * macros from <pudebug.h> instead. */
#   include <pudebug.h>

    /* workaround for /W4 warnings about 'constant expressions' */
#   define IRTLASSERT(f)                                    \
    ((void) ((f) || (PuDbgAssertFailed(DBG_CONTEXT, #f, ""), 0) ))

#  elif defined(_MSC_VER)  &&  (_MSC_VER >= 1000)
    /* Use the new debugging tools in Visual C++ 4.x */
#   include <crtdbg.h>
    /* _ASSERTE will give a more meaningful message, but the string takes
     * space.  Use _ASSERT if this is an issue. */
#   define IRTLASSERT(f) _ASSERTE(f)
#  else
#   include <assert.h>
#   define IRTLASSERT(f) assert(f)
#  endif

#  ifdef _PREFAST_
#    undef  IRTLASSERT
#    define IRTLASSERT(f)          ((void)0)
#  endif

#  define IRTLVERIFY(f)           IRTLASSERT(f)
#  define DEBUG_ONLY(f)           (f)
#  define TRACE                   IrtlTrace
#  define TRACE0(psz)             IrtlTrace(_T("%s"), _T(psz))
#  define TRACE1(psz, p1)         IrtlTrace(_T(psz), p1)
#  define TRACE2(psz, p1, p2)     IrtlTrace(_T(psz), p1, p2)
#  define TRACE3(psz, p1, p2, p3) IrtlTrace(_T(psz), p1, p2, p3)
#  define ASSERT_VALID(pObj)  \
     do {IRTLASSERT((pObj) != NULL); (pObj)->AssertValid();} while (0)
#  define DUMP(pObj)  \
     do {IRTLASSERT((pObj) != NULL); (pObj)->Dump();} while (0)

# else /* !_DEBUG */

  /* These macros should all compile away to nothing */
#  define IRTLASSERT(f)           ((void)0)
#  define IRTLVERIFY(f)           ((void)(f))
#  define DEBUG_ONLY(f)           ((void)0)
#  define TRACE                   1 ? (void)0 : IrtlTrace
#  define TRACE0(psz)
#  define TRACE1(psz, p1)
#  define TRACE2(psz, p1, p2)
#  define TRACE3(psz, p1, p2, p3)
#  define ASSERT_VALID(pObj)      ((void)0)
#  define DUMP(pObj)              ((void)0)

# endif /* !_DEBUG */


# define ASSERT_POINTER(p, type) \
    IRTLASSERT((p) != NULL)

#define ASSERT_STRING(s) \
    IRTLASSERT(((s) != NULL))

/* Declarations for non-Windows apps */

# ifndef _WINDEF_
typedef void*           LPVOID;
typedef const void*     LPCVOID;
typedef unsigned int    UINT;
typedef int             BOOL;
typedef const char*     LPCTSTR;
# endif /* _WINDEF_ */

# ifndef TRUE
#  define FALSE  0
#  define TRUE   1
# endif

#endif /* !_AFX */


#ifdef __cplusplus

// Compile-time (not run-time) assertion. Code will not compile if
// expr is false. Note: there is no non-debug version of this; we
// want this for all builds. The compiler optimizes the code away.
template <bool> struct static_checker;
template <> struct static_checker<true> {};  // specialize only for `true'
#define STATIC_ASSERT(expr) static_checker< (expr) >()

#endif /* !__cplusplus */

/* Writes trace messages to debug stream */
extern
#ifdef __cplusplus
"C"
#endif /* !__cplusplus */
IRTL_DLLEXP
void __cdecl
IrtlTrace(
    LPCTSTR pszFormat,
    ...);


#ifdef _DEBUG
# define IRTL_DEBUG_INIT()            IrtlDebugInit()
# define IRTL_DEBUG_TERM()            IrtlDebugTerm()
#else /* !_DEBUG */
# define IRTL_DEBUG_INIT()            ((void)0)
# define IRTL_DEBUG_TERM()            ((void)0)
#endif /* !_DEBUG */


#ifdef __cplusplus
extern "C" {
#endif /* __cplusplus */

/* should be called from main(), WinMain(), or DllMain() */
IRTL_DLLEXP void
IrtlDebugInit();

IRTL_DLLEXP void
IrtlDebugTerm();

#ifdef __cplusplus
}
#endif /* __cplusplus */

#endif /* __IRTLDBG_H__ */
