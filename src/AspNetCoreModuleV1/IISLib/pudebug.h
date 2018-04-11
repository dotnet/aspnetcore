// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

# ifndef _PUDEBUG_H_
# define _PUDEBUG_H_

#ifndef _NO_TRACING_
# define _NO_TRACING_
#endif // _NO_TRACING_

/************************************************************
 *     Include Headers
 ************************************************************/

# ifdef __cplusplus
extern "C" {
# endif // __cplusplus

# include <windows.h>

# ifndef dllexp
# define dllexp   __declspec( dllexport)
# endif // dllexp

#include <specstrings.h>

#ifndef IN_OUT
#define IN_OUT __inout
#endif

/***********************************************************
 *    Macros
 ************************************************************/

enum  PRINT_REASONS {
    PrintNone     = 0x0,   // Nothing to be printed
    PrintError    = 0x1,   // An error message
    PrintWarning  = 0x2,   // A  warning message
    PrintLog      = 0x3,   // Just logging. Indicates a trace of where ...
    PrintMsg      = 0x4,   // Echo input message
    PrintCritical = 0x5,   // Print and Exit
    PrintAssertion= 0x6    // Printing for an assertion failure
  };


enum  DEBUG_OUTPUT_FLAGS {
    DbgOutputNone     = 0x0,            // None
    DbgOutputKdb      = 0x1,            // Output to Kernel Debugger
    DbgOutputLogFile  = 0x2,            // Output to LogFile
    DbgOutputTruncate = 0x4,            // Truncate Log File if necessary
    DbgOutputStderr   = 0x8,            // Send output to std error
    DbgOutputBackup   = 0x10,           // Make backup of debug file ?
    DbgOutputMemory   = 0x20,           // Dump to memory buffer
    DbgOutputAll      = 0xFFFFFFFF      // All the bits set.
  };


# define MAX_LABEL_LENGTH                 ( 100)


// The following flags are used internally to track what level of tracing we
// are currently using. Bitmapped for extensibility.
#define DEBUG_FLAG_ODS          0x00000001
//#define DEBUG_FLAG_INFO         0x00000002
//#define DEBUG_FLAG_WARN         0x00000004
//#define DEBUG_FLAG_ERROR        0x00000008
// The following are used internally to determine whether to log or not based
// on what the current state is
//#define DEBUG_FLAGS_INFO        (DEBUG_FLAG_ODS | DEBUG_FLAG_INFO)
//#define DEBUG_FLAGS_WARN        (DEBUG_FLAG_ODS | DEBUG_FLAG_INFO | DEBUG_FLAG_WARN)
//#define DEBUG_FLAGS_ERROR       (DEBUG_FLAG_ODS | DEBUG_FLAG_INFO | DEBUG_FLAG_WARN | DEBUG_FLAG_ERROR)

#define DEBUG_FLAGS_ANY         (DEBUG_FLAG_INFO | DEBUG_FLAG_WARN | DEBUG_FLAG_ERROR)

//
// user of DEBUG infrastructure may choose unique variable name for DEBUG_FLAGS
// that's specially useful for cases where DEBUG infrastructure is used within
// static library (static library may prefer to maintain it's own DebugFlags independent
// on the main program it links to
//
#ifndef DEBUG_FLAGS_VAR
#define DEBUG_FLAGS_VAR g_dwDebugFlags
#endif

extern
#ifdef __cplusplus
"C"
# endif // _cplusplus
 DWORD  DEBUG_FLAGS_VAR ;           // Debugging Flags

# define DECLARE_DEBUG_VARIABLE()

# define SET_DEBUG_FLAGS( dwFlags)         DEBUG_FLAGS_VAR = dwFlags
# define GET_DEBUG_FLAGS()                 ( DEBUG_FLAGS_VAR )

# define LOAD_DEBUG_FLAGS_FROM_REG(hkey, dwDefault)  \
             DEBUG_FLAGS_VAR = PuLoadDebugFlagsFromReg((hkey), (dwDefault))

# define LOAD_DEBUG_FLAGS_FROM_REG_STR(pszRegKey, dwDefault)  \
             DEBUG_FLAGS_VAR = PuLoadDebugFlagsFromRegStr((pszRegKey), (dwDefault))

# define SAVE_DEBUG_FLAGS_IN_REG(hkey, dwDbg)  \
               PuSaveDebugFlagsInReg((hkey), (dwDbg))

# define DEBUG_IF( arg, s)     if ( DEBUG_ ## arg & GET_DEBUG_FLAGS()) { \
                                       s \
                                } else {}

# define IF_DEBUG( arg)        if ( DEBUG_## arg & GET_DEBUG_FLAGS())


/*++
  class DEBUG_PRINTS

  This class is responsible for printing messages to log file / kernel debugger

  Currently the class supports only member functions for <ANSI> char.
   ( not unicode-strings).

--*/


typedef struct _DEBUG_PRINTS {

    CHAR         m_rgchLabel[MAX_LABEL_LENGTH];
    CHAR         m_rgchLogFilePath[MAX_PATH];
    CHAR         m_rgchLogFileName[MAX_PATH];
    HANDLE       m_LogFileHandle;
    HANDLE       m_StdErrHandle;
    BOOL         m_fInitialized;
    BOOL         m_fBreakOnAssert;
    DWORD        m_dwOutputFlags;
    VOID        *m_pMemoryLog;
} DEBUG_PRINTS, FAR * LPDEBUG_PRINTS;


LPDEBUG_PRINTS
PuCreateDebugPrintsObject(
   IN const char * pszPrintLabel,
   IN DWORD  dwOutputFlags);

//
// frees the debug prints object and closes any file if necessary.
//  Returns NULL on success or returns pDebugPrints on failure.
//
LPDEBUG_PRINTS
PuDeleteDebugPrintsObject(
   IN_OUT LPDEBUG_PRINTS  pDebugPrints);


VOID
PuDbgPrint(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFilePath,
   IN int                  nLineNum,
   IN const char *         pszFunctionName,
   IN const char *         pszFormat,
   ...);
                           // arglist
VOID
PuDbgPrintW(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFilePath,
   IN int                  nLineNum,
   IN const char *         pszFunctionName,
   IN const WCHAR *        pszFormat,
   ...);                               // arglist

// PuDbgPrintError is similar to PuDbgPrint() but allows
// one to print error code in friendly manner
VOID
PuDbgPrintError(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFilePath,
   IN int                  nLineNum,
   IN const char *         pszFunctionName,
   IN DWORD                dwError,
   IN const char *         pszFormat,
   ...);                               // arglist

/*++
  PuDbgDump() does not do any formatting of output.
  It just dumps the given message onto the debug destinations.
--*/
VOID
PuDbgDump(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFilePath,
   IN int                  nLineNum,
   IN const char *         pszFunctionName,
   IN const char *         pszDump
   );

//
// PuDbgAssertFailed() *must* be __cdecl to properly capture the
// thread context at the time of the failure.
//

INT
__cdecl
PuDbgAssertFailed(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFilePath,
   IN int                  nLineNum,
   IN const char *         pszFunctionName,
   IN const char *         pszExpression,
   IN const char *         pszMessage);

INT
WINAPI
PuDbgPrintAssertFailed(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFilePath,
   IN int                  nLineNum,
   IN const char *         pszFunctionName,
   IN const char *         pszExpression,
   IN const char *         pszMessage);

VOID
PuDbgCaptureContext (
    OUT PCONTEXT ContextRecord
    );

VOID
PuDbgPrintCurrentTime(
    IN_OUT LPDEBUG_PRINTS         pDebugPrints,
    IN const char *               pszFilePath,
    IN int                        nLineNum,
    IN const char *               pszFunctionName
    );

VOID
PuSetDbgOutputFlags(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints,
   IN DWORD                dwFlags);

DWORD
PuGetDbgOutputFlags(
   IN const LPDEBUG_PRINTS       pDebugPrints);


//
// Following functions return Win32 error codes.
// NO_ERROR if success
//

DWORD
PuOpenDbgPrintFile(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFileName,
   IN const char *         pszPathForFile);

DWORD
PuReOpenDbgPrintFile(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints);

DWORD
PuCloseDbgPrintFile(
   IN_OUT LPDEBUG_PRINTS   pDebugPrints);

DWORD
PuOpenDbgMemoryLog(
    IN_OUT LPDEBUG_PRINTS   pDebugPrints);

DWORD
PuCloseDbgMemoryLog(
    IN_OUT LPDEBUG_PRINTS   pDebugPrints);

DWORD
PuLoadDebugFlagsFromReg(IN HKEY hkey, IN DWORD dwDefault);

DWORD
PuLoadDebugFlagsFromRegStr(IN LPCSTR pszRegKey, IN DWORD dwDefault);

DWORD
PuSaveDebugFlagsInReg(IN HKEY hkey, IN DWORD dwDbg);


# define PuPrintToKdb( pszOutput)    \
                    if ( pszOutput != NULL)   {   \
                        OutputDebugString( pszOutput);  \
                    } else {}



# ifdef __cplusplus
};
# endif // __cplusplus

// begin_user_unmodifiable



/***********************************************************
 *    Macros
 ************************************************************/


extern
#ifdef __cplusplus
"C"
# endif // _cplusplus
DEBUG_PRINTS  *  g_pDebug;        // define a global debug variable

# if DBG

// For the CHK build we want ODS enabled. For an explanation of these flags see
// the comment just after the definition of DBG_CONTEXT
# define DECLARE_DEBUG_PRINTS_OBJECT()                      \
         DEBUG_PRINTS  *  g_pDebug = NULL;                  \
         DWORD  DEBUG_FLAGS_VAR = DEBUG_FLAG_ERROR;

#else // !DBG

# define DECLARE_DEBUG_PRINTS_OBJECT()          \
         DEBUG_PRINTS  *  g_pDebug = NULL;      \
         DWORD  DEBUG_FLAGS_VAR = 0;

#endif // !DBG


//
// Call the following macro as part of your initialization for program
//  planning to use the debugging class.
//
/** DEBUGDEBUG
# define CREATE_DEBUG_PRINT_OBJECT( pszLabel)  \
        g_pDebug = PuCreateDebugPrintsObject( pszLabel, DEFAULT_OUTPUT_FLAGS);\
         if  ( g_pDebug == NULL) {   \
               OutputDebugStringA( "Unable to Create Debug Print Object \n"); \
         }
*/

//
// Call the following macro once as part of the termination of program
//    which uses the debugging class.
//
# define DELETE_DEBUG_PRINT_OBJECT( )  \
        g_pDebug = PuDeleteDebugPrintsObject( g_pDebug);


# define VALID_DEBUG_PRINT_OBJECT()     \
        ( ( g_pDebug != NULL) && g_pDebug->m_fInitialized)


//
//  Use the DBG_CONTEXT without any surrounding braces.
//  This is used to pass the values for global DebugPrintObject
//     and File/Line information
//
//# define DBG_CONTEXT        g_pDebug, __FILE__, __LINE__, __FUNCTION__

// The 3 main tracing macros, each one corresponds to a different level of
// tracing

// The 3 main tracing macros, each one corresponds to a different level of
// tracing
//# define DBGINFO(args)      {if (DEBUG_FLAGS_VAR & DEBUG_FLAGS_INFO) { PuDbgPrint args; }}
//# define DBGWARN(args)      {if (DEBUG_FLAGS_VAR & DEBUG_FLAGS_WARN) { PuDbgPrint args; }}
//# define DBGERROR(args)     {if (DEBUG_FLAGS_VAR & DEBUG_FLAGS_ERROR) { PuDbgPrint args; }}

# define DBGINFOW(args)     {if (DEBUG_FLAGS_VAR & DEBUG_FLAGS_INFO) { PuDbgPrintW args; }}
# define DBGWARNW(args)     {if (DEBUG_FLAGS_VAR & DEBUG_FLAGS_WARN) { PuDbgPrintW args; }}
# define DBGERRORW(args)    {if (DEBUG_FLAGS_VAR & DEBUG_FLAGS_ERROR) { PuDbgPrintW args; }}


//
//  DBGPRINTF() is printing function ( much like printf) but always called
//    with the DBG_CONTEXT as follows
//   DBGPRINTF( ( DBG_CONTEXT, format-string, arguments for format list));
//
# define DBGPRINTF DBGINFO

//
//  DPERROR() is printing function ( much like printf) but always called
//    with the DBG_CONTEXT as follows
//   DPERROR( ( DBG_CONTEXT, error, format-string,
//                      arguments for format list));
//
# define DPERROR( args)       {if (DEBUG_FLAGS_VAR & DEBUG_FLAGS_ERROR) { PuDbgPrintError args; }}

# if DBG

# define DBG_CODE(s)          s          /* echoes code in debugging mode */

// The same 3 main tracing macros however in this case the macros are only compiled
// into the CHK build. This is necessary because some tracing info used functions or
// variables which are not compiled into the FRE build.
# define CHKINFO(args)      { PuDbgPrint args; }
# define CHKWARN(args)      { PuDbgPrint args; }
# define CHKERROR(args)     { PuDbgPrint args; }

# define CHKINFOW(args)     { PuDbgPrintW args; }
# define CHKWARNW(args)     { PuDbgPrintW args; }
# define CHKERRORW(args)    { PuDbgPrintW args; }


#ifndef DBG_ASSERT
# ifdef _PREFAST_
#  define DBG_ASSERT(exp)                         ((void)0) /* Do Nothing */
#  define DBG_ASSERT_MSG(exp, pszMsg)             ((void)0) /* Do Nothing */
#  define DBG_REQUIRE( exp)                       ((void) (exp))
# else  // !_PREFAST_
#  define DBG_ASSERT( exp )                  \
    ( (VOID)( ( exp ) || ( DebugBreak(),    \
                           PuDbgPrintAssertFailed( DBG_CONTEXT, #exp, "" ) ) ) )

#  define DBG_ASSERT_MSG( exp, pszMsg)       \
    ( (VOID)( ( exp ) || ( DebugBreak(),    \
                           PuDbgPrintAssertFailed( DBG_CONTEXT, #exp, pszMsg ) ) ) )

#  define DBG_REQUIRE( exp )                 \
    DBG_ASSERT( exp )
# endif // !_PREFAST_
#endif


# define DBG_LOG()      PuDbgPrint( DBG_CONTEXT, "\n" )

# define DBG_OPEN_LOG_FILE( pszFile, pszPath )  \
                        PuOpenDbgPrintFile( g_pDebug, (pszFile), (pszPath) )

# define DBG_CLOSE_LOG_FILE( )  \
                        PuCloseDbgPrintFile( g_pDebug )

# define DBG_OPEN_MEMORY_LOG( ) \
                        PuOpenDbgMemoryLog( g_pDebug )


# define DBGDUMP( args )            PuDbgDump  args

# define DBGPRINT_CURRENT_TIME()    PuDbgPrintCurrentTime( DBG_CONTEXT )

# else // !DBG

# define DBG_CODE(s)        ((void)0) /* Do Nothing */

# define CHKINFO(args)      ((void)0) /* Do Nothing */
# define CHKWARN(args)      ((void)0) /* Do Nothing */
# define CHKERROR(args)     ((void)0) /* Do Nothing */

# define CHKINFOW(args)     ((void)0) /* Do Nothing */
# define CHKWARNW(args)     ((void)0) /* Do Nothing */
# define CHKERRORW(args)    ((void)0) /* Do Nothing */

#ifndef DBG_ASSERT
# define DBG_ASSERT(exp)                         ((void)0) /* Do Nothing */

# define DBG_ASSERT_MSG(exp, pszMsg)             ((void)0) /* Do Nothing */

# define DBG_REQUIRE( exp)                       ((void) (exp))
#endif // !DBG_ASSERT

# define DBGDUMP( args)                          ((void)0) /* Do nothing */

# define DBG_LOG()                               ((void)0) /* Do Nothing */

# define DBG_OPEN_LOG_FILE( pszFile, pszPath)    ((void)0) /* Do Nothing */

# define DBG_OPEN_MEMORY_LOG()                   ((void)0) /* Do Nothing */

# define DBG_CLOSE_LOG_FILE()                    ((void)0) /* Do Nothing */

# define DBGPRINT_CURRENT_TIME()                 ((void)0) /* Do Nothing */

# endif // !DBG


// end_user_unmodifiable

// begin_user_unmodifiable


#ifdef ASSERT
# undef ASSERT
#endif


# define ASSERT( exp)           DBG_ASSERT( exp)


// end_user_unmodifiable

// begin_user_modifiable

//
//  Debugging constants consist of two pieces.
//  All constants in the range 0x0 to 0x8000 are reserved
//  User extensions may include additional constants (bit flags)
//

# define DEBUG_API_ENTRY                  0x00000001L
# define DEBUG_API_EXIT                   0x00000002L
# define DEBUG_INIT_CLEAN                 0x00000004L
# define DEBUG_ERROR                      0x00000008L

                   // End of Reserved Range
# define DEBUG_RESERVED                   0x00000FFFL

// end_user_modifiable



/***********************************************************
 *    Platform Type related variables and macros
 ************************************************************/

//
// Enum for product types
//

typedef enum _PLATFORM_TYPE {

    PtInvalid = 0,                 // Invalid
    PtNtWorkstation = 1,           // NT Workstation
    PtNtServer = 2,                // NT Server

} PLATFORM_TYPE;

//
// IISGetPlatformType is the function used to the platform type
//

extern
#ifdef __cplusplus
"C"
# endif // _cplusplus
PLATFORM_TYPE
IISGetPlatformType(
        VOID
        );

//
// External Macros
//

#define InetIsNtServer( _pt )           ((_pt) == PtNtServer)
#define InetIsNtWksta( _pt )            ((_pt) == PtNtWorkstation)
#define InetIsValidPT(_pt)              ((_pt) != PtInvalid)

extern
#ifdef __cplusplus
"C"
# endif // _cplusplus
PLATFORM_TYPE    g_PlatformType;


// Use the DECLARE_PLATFORM_TYPE macro to declare the platform type
#define DECLARE_PLATFORM_TYPE()  \
   PLATFORM_TYPE    g_PlatformType = PtInvalid;

// Use the INITIALIZE_PLATFORM_TYPE to init the platform type
// This should typically go inside the DLLInit or equivalent place.
#define INITIALIZE_PLATFORM_TYPE()  \
   g_PlatformType = IISGetPlatformType();

//
// Additional Macros to use the Platform Type
//

#define TsIsNtServer( )         InetIsNtServer(g_PlatformType)
#define TsIsNtWksta( )          InetIsNtWksta(g_PlatformType)
#define IISIsValidPlatform()    InetIsValidPT(g_PlatformType)
#define IISPlatformType()       (g_PlatformType)


/***********************************************************
 *    Some utility functions for Critical Sections
 ************************************************************/

//
// IISSetCriticalSectionSpinCount() provides a thunk for the
// original NT4.0sp3 API SetCriticalSectionSpinCount() for CS with Spin counts
// Users of this function should definitely dynlink with kernel32.dll,
// Otherwise errors will surface to a large extent
//
extern
# ifdef __cplusplus
"C"
# endif // _cplusplus
DWORD
IISSetCriticalSectionSpinCount(
    LPCRITICAL_SECTION lpCriticalSection,
    DWORD dwSpinCount
);


//
// Macro for the calls to SetCriticalSectionSpinCount()
//
# define SET_CRITICAL_SECTION_SPIN_COUNT( lpCS, dwSpins) \
  IISSetCriticalSectionSpinCount( (lpCS), (dwSpins))

//
// IIS_DEFAULT_CS_SPIN_COUNT is the default value of spins used by
//  Critical sections defined within IIS.
// NYI: We should have to switch the individual values based on experiments!
// Current value is an arbitrary choice
//
# define IIS_DEFAULT_CS_SPIN_COUNT   (1000)

//
// Initializes a critical section and sets its spin count
// to IIS_DEFAULT_CS_SPIN_COUNT.  Equivalent to
// InitializeCriticalSectionAndSpinCount(lpCS, IIS_DEFAULT_CS_SPIN_COUNT),
// but provides a safe thunking layer for older systems that don't provide
// this API.
//
extern
# ifdef __cplusplus
"C"
# endif // _cplusplus
BOOL
IISInitializeCriticalSection(
    LPCRITICAL_SECTION lpCriticalSection
);

//
// Macro for the calls to InitializeCriticalSection()
//
# define INITIALIZE_CRITICAL_SECTION(lpCS) IISInitializeCriticalSection(lpCS)

# endif  /* _DEBUG_HXX_ */

//
// The following macros allow the automatic naming of certain Win32 objects.
// See IIS\SVCS\IISRTL\WIN32OBJ.C for details on the naming convention.
//
// Set IIS_NAMED_WIN32_OBJECTS to a non-zero value to enable named events,
// semaphores, and mutexes.
//

#if DBG
#define IIS_NAMED_WIN32_OBJECTS 1
#else
#define IIS_NAMED_WIN32_OBJECTS 0
#endif

#ifdef __cplusplus
extern "C" {
#endif

HANDLE
PuDbgCreateEvent(
    __in LPSTR FileName,
    IN ULONG LineNumber,
    __in LPSTR MemberName,
    IN PVOID Address,
    IN BOOL ManualReset,
    IN BOOL InitialState
    );

HANDLE
PuDbgCreateSemaphore(
    __in LPSTR FileName,
    IN ULONG LineNumber,
    __in LPSTR MemberName,
    IN PVOID Address,
    IN LONG InitialCount,
    IN LONG MaximumCount
    );

HANDLE
PuDbgCreateMutex(
    __in LPSTR FileName,
    IN ULONG LineNumber,
    __in LPSTR MemberName,
    IN PVOID Address,
    IN BOOL InitialOwner
    );

#ifdef __cplusplus
}   // extern "C"
#endif

#if IIS_NAMED_WIN32_OBJECTS

#define IIS_CREATE_EVENT( membername, address, manual, state )              \
    PuDbgCreateEvent(                                                       \
        (LPSTR)__FILE__,                                                    \
        (ULONG)__LINE__,                                                    \
        (membername),                                                       \
        (PVOID)(address),                                                   \
        (manual),                                                           \
        (state)                                                             \
        )

#define IIS_CREATE_SEMAPHORE( membername, address, initial, maximum )       \
    PuDbgCreateSemaphore(                                                   \
        (LPSTR)__FILE__,                                                    \
        (ULONG)__LINE__,                                                    \
        (membername),                                                       \
        (PVOID)(address),                                                   \
        (initial),                                                          \
        (maximum)                                                           \
        )

#define IIS_CREATE_MUTEX( membername, address, initial )                     \
    PuDbgCreateMutex(                                                       \
        (LPSTR)__FILE__,                                                    \
        (ULONG)__LINE__,                                                    \
        (membername),                                                       \
        (PVOID)(address),                                                   \
        (initial)                                                           \
        )

#else   // !IIS_NAMED_WIN32_OBJECTS

#define IIS_CREATE_EVENT( membername, address, manual, state )              \
    CreateEventA(                                                           \
        NULL,                                                               \
        (manual),                                                           \
        (state),                                                            \
        NULL                                                                \
        )

#define IIS_CREATE_SEMAPHORE( membername, address, initial, maximum )       \
    CreateSemaphoreA(                                                       \
        NULL,                                                               \
        (initial),                                                          \
        (maximum),                                                          \
        NULL                                                                \
        )

#define IIS_CREATE_MUTEX( membername, address, initial )                     \
    CreateMutexA(                                                           \
        NULL,                                                               \
        (initial),                                                          \
        NULL                                                                \
        )

#endif  // IIS_NAMED_WIN32_OBJECTS


/************************ End of File ***********************/

