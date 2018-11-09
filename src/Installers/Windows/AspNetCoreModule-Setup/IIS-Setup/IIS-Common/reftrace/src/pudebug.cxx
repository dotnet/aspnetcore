// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

/************************************************************
 * Include Headers
 ************************************************************/
# include "precomp.hxx"


# include <stdio.h>
# include <stdlib.h>
# include <stdarg.h>
# include <string.h>
# include <suppress.h>


# include "pudebug.h"
# include "memorylog.hxx"


/*************************************************************
 * Global Variables and Default Values
 *************************************************************/

//
// TRUE if we're in a test process. 
// There are a few noisy assertions that fire frequently cause of test code issues. These noisy asserts are masking
// real ones, drastically reducing the value of CHK bits. 
//
BOOL g_fTestProcess = FALSE;

//
// HACK HACK
// suppress shutdown asserts under some hosts
//
BOOL g_fAvoidShutdownAsserts = FALSE;

# define MAX_PRINTF_OUTPUT  ( 10240)

# define DEFAULT_DEBUG_FLAGS_VALUE  ( 0)
# define DEBUG_FLAGS_REGISTRY_LOCATION_A   "DebugFlags"
# define DEBUG_BREAK_ENABLED_REGKEYNAME_A  "BreakOnAssert"

/*************************************************************
 *   Functions
 *************************************************************/

/********************************************************************++

Routine Description:
   This function creates a new DEBUG_PRINTS object for the required
     program.

Arguments:
      pszPrintLabel     pointer to null-terminated string containing
                         the label for program's debugging output
      dwOutputFlags     DWORD containing the output flags to be used.

Returns:
       pointer to a new DEBUG_PRINTS object on success.
       Returns NULL on failure.
--*********************************************************************/
LPDEBUG_PRINTS
PuCreateDebugPrintsObject(
    IN const char *         pszPrintLabel,
    IN DWORD                dwOutputFlags)
{

   LPDEBUG_PRINTS   pDebugPrints;

   pDebugPrints = (LPDEBUG_PRINTS ) GlobalAlloc( GPTR, sizeof( DEBUG_PRINTS));

   if ( pDebugPrints != NULL) {

        if ( strlen( pszPrintLabel) < MAX_LABEL_LENGTH) {

            strcpy_s( pDebugPrints->m_rgchLabel, 
                      sizeof( pDebugPrints->m_rgchLabel )  / sizeof( pDebugPrints->m_rgchLabel[0]),
                      pszPrintLabel);
        } else {
            strncpy_s( pDebugPrints->m_rgchLabel,
                       sizeof( pDebugPrints->m_rgchLabel )  / sizeof( pDebugPrints->m_rgchLabel[0]),
                       pszPrintLabel, 
                       MAX_LABEL_LENGTH - 1);
        }

        memset( pDebugPrints->m_rgchLogFilePath, 0, MAX_PATH);
        memset( pDebugPrints->m_rgchLogFileName, 0, MAX_PATH);

        pDebugPrints->m_LogFileHandle = INVALID_HANDLE_VALUE;

        pDebugPrints->m_dwOutputFlags = dwOutputFlags;
        pDebugPrints->m_StdErrHandle  = GetStdHandle( STD_ERROR_HANDLE);

        if ( pDebugPrints->m_StdErrHandle == NULL )
        {
            pDebugPrints->m_StdErrHandle = INVALID_HANDLE_VALUE;
        }

        pDebugPrints->m_fInitialized = TRUE;
        pDebugPrints->m_fBreakOnAssert= TRUE;
        pDebugPrints->m_pMemoryLog = NULL;
    }


   return ( pDebugPrints);
} // PuCreateDebugPrintsObject()




/********************************************************************++

Routine Description:
    This function cleans up the pDebugPrints object and
      frees the allocated memory.

    Arguments:
       pDebugPrints     poitner to the DEBUG_PRINTS object.

    Returns:
        NULL  on  success.
        pDebugPrints() if the deallocation failed.

--*********************************************************************/
LPDEBUG_PRINTS
PuDeleteDebugPrintsObject(
    IN OUT LPDEBUG_PRINTS pDebugPrints)
{
    if ( pDebugPrints != NULL) {

        PuCloseDbgMemoryLog(pDebugPrints);
        DWORD dwError = PuCloseDbgPrintFile( pDebugPrints);

        if ( dwError != NO_ERROR) {

            SetLastError( dwError);
        } else {

            // returns NULL on success
            pDebugPrints =
                (LPDEBUG_PRINTS ) GlobalFree( pDebugPrints);
        }
    }

    return ( pDebugPrints);

} // PuDeleteDebugPrintsObject()




VOID
PuSetDbgOutputFlags(
    IN OUT LPDEBUG_PRINTS   pDebugPrints,
    IN DWORD                dwFlags)
{

    if ( pDebugPrints == NULL) {

        SetLastError( ERROR_INVALID_PARAMETER);
    } else {

        pDebugPrints->m_dwOutputFlags = dwFlags;
    }

    return;
} // PuSetDbgOutputFlags()



DWORD
PuGetDbgOutputFlags(
    IN const LPDEBUG_PRINTS      pDebugPrints)
{
    return ( pDebugPrints != NULL) ? pDebugPrints->m_dwOutputFlags : 0;

} // PuGetDbgOutputFlags()


static DWORD
PuOpenDbgFileLocal(
   IN OUT LPDEBUG_PRINTS pDebugPrints)
{
    if ( pDebugPrints == NULL)
        return ERROR_INVALID_PARAMETER;

    if ( pDebugPrints->m_LogFileHandle != INVALID_HANDLE_VALUE) {

        //
        // Silently return as a file handle exists.
        //
        return ( NO_ERROR);
    }

    pDebugPrints->m_LogFileHandle =
                      CreateFileA( pDebugPrints->m_rgchLogFileName,
                                  GENERIC_WRITE,
                                  FILE_SHARE_READ | FILE_SHARE_WRITE,
                                  NULL,
                                  OPEN_ALWAYS,
                                  FILE_ATTRIBUTE_NORMAL,
                                  NULL);

    if ( pDebugPrints->m_LogFileHandle == INVALID_HANDLE_VALUE) {

        CHAR  pchBuffer[1024];
        DWORD dwError = GetLastError();

        sprintf_s( pchBuffer,
                   sizeof( pchBuffer ) / sizeof( pchBuffer[0] ),
                   " Critical Error: Unable to Open File %s. Error = %d\n",
                   pDebugPrints->m_rgchLogFileName, dwError);
        OutputDebugStringA( pchBuffer);

        return ( dwError);
    }

    return ( NO_ERROR);
} // PuOpenDbgFileLocal()





DWORD
PuOpenDbgPrintFile(
   IN OUT LPDEBUG_PRINTS      pDebugPrints,
   IN const char *            pszFileName,
   IN const char *            pszPathForFile)
/********************************************************************++

  Opens a Debugging log file. This function can be called to set path
  and name of the debugging file.

  Arguments:
     pszFileName           pointer to null-terminated string containing
                            the name of the file.

     pszPathForFile        pointer to null-terminated string containing the
                            path for the given file.
                           If NULL, then the old place where dbg files were
                           stored is used or if none,
                           default windows directory will be used.

   Returns:
       Win32 error codes. NO_ERROR on success.

--*********************************************************************/

{
    if ( pszFileName == NULL || pDebugPrints == NULL) {

        return ( ERROR_INVALID_PARAMETER);
    }

    //
    //  Setup the Path information. if necessary.
    //

    if ( pszPathForFile != NULL) {

        // Path is being changed.

        if ( strlen( pszPathForFile) < MAX_PATH) {

            strcpy_s( pDebugPrints->m_rgchLogFilePath, 
                      sizeof( pDebugPrints->m_rgchLogFilePath ) / sizeof( pDebugPrints->m_rgchLogFilePath[0] ),
                      pszPathForFile);
        } else {

            return ( ERROR_INVALID_PARAMETER);
        }
    } else {

        if ( pDebugPrints->m_rgchLogFilePath[0] == '\0' &&  // no old path
            !GetWindowsDirectoryA( pDebugPrints->m_rgchLogFilePath, MAX_PATH)) {

            //
            //  Unable to get the windows default directory. Use current dir
            //

            strcpy_s( pDebugPrints->m_rgchLogFilePath, 
                      sizeof( pDebugPrints->m_rgchLogFilePath ) / sizeof( pDebugPrints->m_rgchLogFilePath[0] ),
                      ".");
        }
    }

    //
    // Should need be, we need to create this directory for storing file
    //


    //
    // Form the complete Log File name and open the file.
    //
    if ( (strlen( pszFileName) + strlen( pDebugPrints->m_rgchLogFilePath))
         >= MAX_PATH) {

        return ( ERROR_NOT_ENOUGH_MEMORY);
    }

    //  form the complete path
    strcpy_s( pDebugPrints->m_rgchLogFileName, 
              sizeof( pDebugPrints->m_rgchLogFileName ) / sizeof( pDebugPrints->m_rgchLogFileName[0] ),
              pDebugPrints->m_rgchLogFilePath);

    if ( pDebugPrints->m_rgchLogFileName[ strlen(pDebugPrints->m_rgchLogFileName) - 1]
        != '\\') {
        // Append a \ if necessary
        strcat_s( pDebugPrints->m_rgchLogFileName, 
                  sizeof( pDebugPrints->m_rgchLogFileName ) / sizeof( pDebugPrints->m_rgchLogFileName[0] ),
                  "\\");
    };
    strcat_s( pDebugPrints->m_rgchLogFileName, 
              sizeof( pDebugPrints->m_rgchLogFileName ) / sizeof( pDebugPrints->m_rgchLogFileName[0] ),
              pszFileName);

    return  PuOpenDbgFileLocal( pDebugPrints);

} // PuOpenDbgPrintFile()




DWORD
PuReOpenDbgPrintFile(
    IN OUT LPDEBUG_PRINTS    pDebugPrints)
/********************************************************************++

  This function closes any open log file and reopens a new copy.
  If necessary. It makes a backup copy of the file.

--*********************************************************************/

{
    if ( pDebugPrints == NULL) {
        return ( ERROR_INVALID_PARAMETER);
    }

    PuCloseDbgPrintFile( pDebugPrints);      // close any existing file.

    if ( pDebugPrints->m_dwOutputFlags & DbgOutputBackup) {

        // MakeBkupCopy();

        OutputDebugStringA( " Error: MakeBkupCopy() Not Yet Implemented\n");
    }

    return PuOpenDbgFileLocal( pDebugPrints);

} // PuReOpenDbgPrintFile()




DWORD
PuCloseDbgPrintFile(
    IN OUT LPDEBUG_PRINTS    pDebugPrints)
{
    DWORD dwError = NO_ERROR;

    if ( pDebugPrints == NULL ) {
        dwError = ERROR_INVALID_PARAMETER;
    } else {

        if ( pDebugPrints->m_LogFileHandle != INVALID_HANDLE_VALUE) {

            FlushFileBuffers( pDebugPrints->m_LogFileHandle);

            if ( !CloseHandle( pDebugPrints->m_LogFileHandle)) {

                CHAR pchBuffer[1024];

                dwError = GetLastError();

                sprintf_s( pchBuffer,
                           sizeof( pchBuffer ) / sizeof( pchBuffer[0] ),
                           "CloseDbgPrintFile() : CloseHandle( %p) failed."
                           " Error = %d\n",
                           pDebugPrints->m_LogFileHandle,
                           dwError);
                OutputDebugStringA( pchBuffer);
            }

            pDebugPrints->m_LogFileHandle = INVALID_HANDLE_VALUE;
        }
    }

    return ( dwError);
} // DEBUG_PRINTS::CloseDbgPrintFile()

DWORD
PuOpenDbgMemoryLog(IN OUT LPDEBUG_PRINTS pDebugPrints)
{
    DWORD dwError;
    CMemoryLog * pLog = NULL;

    if (NULL == pDebugPrints)
    {
        dwError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    if (NULL != pDebugPrints->m_pMemoryLog)
    {
        dwError = ERROR_SUCCESS;
        goto done;
    }

    pLog = new CMemoryLog(1024 * 512);  // max size of 512 K
    if (NULL == pLog)
    {
        dwError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }

    // save away the pointer
    pDebugPrints->m_pMemoryLog = pLog;

    // make sure output gets to the log
    pDebugPrints->m_dwOutputFlags |= DbgOutputMemory;

    dwError = NO_ERROR;
done:
    return dwError;
}

DWORD
PuCloseDbgMemoryLog(IN OUT LPDEBUG_PRINTS pDebugPrints)
{
    DWORD dwError;

    if (NULL == pDebugPrints)
    {
        dwError = ERROR_INVALID_PARAMETER;
        goto done;
    }
    if (NULL != pDebugPrints->m_pMemoryLog)
    {
        CMemoryLog * pLog = (CMemoryLog*) (pDebugPrints->m_pMemoryLog);
        delete pLog;
        pDebugPrints->m_pMemoryLog = NULL;
    }

    dwError = NO_ERROR;
done:
    return dwError;
}

VOID
PupOutputMessage(
   IN LPDEBUG_PRINTS  pDebugPrints,
   IN STRA           *straOutput
   )
{
  if ( pDebugPrints != NULL)
  {
      if ( ( pDebugPrints->m_dwOutputFlags & DbgOutputStderr) &&
           ( pDebugPrints->m_StdErrHandle != INVALID_HANDLE_VALUE ) ) {

          DWORD nBytesWritten;

          ( VOID) WriteFile( pDebugPrints->m_StdErrHandle,
                             straOutput->QueryStr(),
                             straOutput->QueryCCH(),
                             &nBytesWritten,
                             NULL);
      }

      if ( pDebugPrints->m_dwOutputFlags & DbgOutputLogFile &&
           pDebugPrints->m_LogFileHandle != INVALID_HANDLE_VALUE) {

          DWORD nBytesWritten;

          //
          // Truncation of log files. Not yet implemented.

          ( VOID) WriteFile( pDebugPrints->m_LogFileHandle,
                             straOutput->QueryStr(),
                             straOutput->QueryCCH(),
                             &nBytesWritten,
                             NULL);

      }

      if ( (pDebugPrints->m_dwOutputFlags & DbgOutputMemory) &&
           (NULL != pDebugPrints->m_pMemoryLog) )
      {
            CMemoryLog* pLog = (CMemoryLog*) (pDebugPrints->m_pMemoryLog);
            pLog->Append(straOutput->QueryStr(), straOutput->QueryCCH());
      }

  }


  if ( pDebugPrints == NULL ||
       pDebugPrints->m_dwOutputFlags & DbgOutputKdb)
  {
      OutputDebugStringA( straOutput->QueryStr() );
  }

  return;
}

void
FormatMsgToBuffer( IN OUT STRA * pSTRAOutput,
                   IN LPDEBUG_PRINTS pDebugPrints,
                   IN LPCSTR    pszFilePath,
                   IN DWORD     nLineNum,
                   IN LPCSTR	   pszFunctionName,
                   IN LPCSTR    pszFormat,
                   IN va_list * pargsList)
{
    LPCSTR pszFileName = strrchr( pszFilePath, '\\');
    int cchPrologue = 0;
    HRESULT hr = S_OK;
    DWORD cchOutput = 0;

    //
    //  Skip the complete path name and retain file name in pszName
    //

    if ( pszFileName== NULL) {

       // if skipping \\ yields nothing use whole path.
       pszFileName = pszFilePath;
    }
    else
    {
       // skip past the '\'
       ++pszFileName;
    }

    // Format the message header as: tid label!function [file @ line number]:message
    cchPrologue = sprintf_s( pSTRAOutput->QueryStr(),
                             pSTRAOutput->QuerySize(),
                             "%lu %hs!%hs [%hs @ %d]:",
                             GetCurrentThreadId(),
                             pDebugPrints ? pDebugPrints->m_rgchLabel : "??",
                             pszFunctionName,
                             pszFileName,
                             nLineNum);

    // we directly touched the buffer - however, wait to SyncWithBuffer
    // until the rest of the operations are done.  Do NOT call QueryCCH() it will be WRONG.


    // Format the incoming message using vsnprintf() so that the overflows are
    //  captured

    cchOutput = _vsnprintf_s( pSTRAOutput->QueryStr() + cchPrologue,
                              pSTRAOutput->QuerySize() - cchPrologue,
                              pSTRAOutput->QuerySize() - cchPrologue - 1,
                              pszFormat, *pargsList);

    if ( cchOutput == -1 )
    {
        // couldn't fit this in the original STRA size.  Try a heap allocation.
        hr = pSTRAOutput->Resize(MAX_PRINTF_OUTPUT);
        if (FAILED(hr))
        {
            // Can't allocate, therefore don't give back half done results
            pSTRAOutput->Reset();
            return;
        }

        cchOutput = _vsnprintf_s( pSTRAOutput->QueryStr() + cchPrologue,
                                  pSTRAOutput->QuerySize() - cchPrologue,
                                  pSTRAOutput->QuerySize() - cchPrologue - 1,
                                  pszFormat, *pargsList);
        if (cchOutput == -1)
        {
            // we need to NULL terminate, as _vsnprintf failed to do that for us.
            pSTRAOutput->QueryStr()[pSTRAOutput->QuerySize() - 1] = '\0';
        }
    }

    // we directly touched the buffer - therefore:
    pSTRAOutput->SyncWithBuffer();

    return;
} // FormatMsgToBuffer()


/********************************************************************++
Routine Description:
   Main function that examines the incoming message and prints out a header
    and the message.

Arguments:
  pDebugPrints - pointer to the debug print object
  pszFilePaht  - pointer to the file from where this function is called
  nLineNum     - Line number within the file
  pszFormat    - formatting string to use.

Returns:
  None
--*********************************************************************/

VOID
PuDbgPrint(
   IN OUT LPDEBUG_PRINTS     	pDebugPrints,
   IN const char *            		pszFilePath,
   IN int              			       nLineNum,
   IN const char *				pszFunctionName,
   IN const char *            		pszFormat,
   ...)
{
   STACK_STRA(straOutput, 256);
   va_list argsList;
   DWORD dwErr;

   // get a local copy of the error code so that it is not lost
  dwErr = GetLastError();

  va_start( argsList, pszFormat);
  FormatMsgToBuffer( &straOutput,
                     pDebugPrints,
                     pszFilePath,
                     nLineNum,
                     pszFunctionName,
                     pszFormat,
                     &argsList);

  va_end( argsList);

  //
  // Send the outputs to respective files.
  //
  PupOutputMessage( pDebugPrints, &straOutput);


  SetLastError( dwErr );

  return;
} // PuDbgPrint()

void
FormatMsgToBufferW( IN OUT STRU * pSTRUOutput,
                   IN LPDEBUG_PRINTS pDebugPrints,
                   IN LPCSTR    pszFilePath,
                   IN DWORD     nLineNum,
                   IN LPCSTR   pszFunctionName,
                   IN LPCWSTR    pszFormat,
                   IN va_list * pargsList)
{
   LPCSTR pszFileName = strrchr( pszFilePath, '\\');
   int cchPrologue = 0;
   HRESULT hr = S_OK;
   DWORD cchOutput = 0;

   //
   //  Skip the complete path name and retain file name in pszName
   //

   if ( pszFileName== NULL) {

      // if skipping \\ yields nothing use whole path.
      pszFileName = pszFilePath;
   }
   else
   {
      // skip past the '\'
      ++pszFileName;
   }

    // Format the message header as: tid label!function [file @ line number]:message
   cchPrologue = swprintf_s( pSTRUOutput->QueryStr(),
                             pSTRUOutput->QuerySizeCCH(),
                             L"%lu %hs!%hs [%hs @ %d]:",
                             GetCurrentThreadId(),
                             pDebugPrints ? pDebugPrints->m_rgchLabel : "??",
                             pszFunctionName,
                             pszFileName,
                             nLineNum);

   // we directly touched the buffer - however, wait to SyncWithBuffer
   // until the rest of the operations are done.  Do NOT call QueryCCH() it will be WRONG.

   // Format the incoming message using vsnprintf() so that the overflows are
   //  captured

   cchOutput = _vsnwprintf_s( pSTRUOutput->QueryStr() + cchPrologue,
                              pSTRUOutput->QuerySizeCCH() - cchPrologue,
							  // DEBUGDEBUG
                              //pSTRUOutput->QueryBuffer()->QuerySize() / sizeof(WCHAR) - cchPrologue - 1, // this is a count of characters
							  pSTRUOutput->QuerySizeCCH() - cchPrologue - 1,
                              pszFormat, *pargsList);

   if ( cchOutput == -1 )
   {
       // couldn't fit this in the original STRA size.  Try a heap allocation.
       hr = pSTRUOutput->Resize(MAX_PRINTF_OUTPUT);
       if (FAILED(hr))
       {
           // Can't allocate, therefore don't give back half done results
           pSTRUOutput->Reset();
           return;
       }

       cchOutput = _vsnwprintf_s( pSTRUOutput->QueryStr() + cchPrologue,
                                  pSTRUOutput->QuerySizeCCH() - cchPrologue,
								  //DEBUGDEBUG
                                  //pSTRUOutput->QueryBuffer()->QuerySize() / sizeof(WCHAR) - cchPrologue - 1, // this is a count of characters
								  pSTRUOutput->QuerySizeCCH() - cchPrologue - 1,
                                  pszFormat, *pargsList);
       if (cchOutput == -1)
       {
           // we need to NULL terminate, as _vsnprintf failed to do that for us.
		   //DEBUGDEBUG
           pSTRUOutput->QueryStr()[pSTRUOutput->QuerySizeCCH() - 1] = L'\0';
		   
       }
   }

   // we directly touched the buffer - therefore:
   pSTRUOutput->SyncWithBuffer();

  return;
} // FormatMsgToBuffer()

extern "C"
VOID
PuDbgPrintW(
   IN OUT LPDEBUG_PRINTS      pDebugPrints,
   IN const char *            pszFilePath,
   IN int                     nLineNum,
   IN const char *		pszFunctionName,
   IN const WCHAR *            pszFormat,
   ...
)
{
   STACK_STRU(struOutput, 256);
   va_list argsList;
   DWORD dwErr;
   HRESULT hr;
   // get a local copy of the error code so that it is not lost
  dwErr = GetLastError();

  va_start( argsList, pszFormat);
  FormatMsgToBufferW( &struOutput,
                     pDebugPrints,
                     pszFilePath,
                     nLineNum,
                     pszFunctionName,
                     pszFormat,
                     &argsList);

  va_end( argsList);

  //
  // Send the outputs to respective files.
  //
  STACK_STRA(straOutput, 256);
  hr = straOutput.CopyWTruncate(struOutput.QueryStr(), struOutput.QueryCCH());
  if (FAILED(hr))
  {
    goto done;
  }

  PupOutputMessage( pDebugPrints, &straOutput);

done:

  SetLastError( dwErr );

  return;
}

/********************************************************************++
Routine Description:
   This function behaves like PuDbgPrint() but also prints out formatted
   Error message indicating what failed.

Arguments:
  pDebugPrints - pointer to the debug print object
  pszFilePaht  - pointer to the file from where this function is called
  nLineNum     - Line number within the file
  dwError      - Error code for which the formatted error message should
                  be printed
  pszFormat    - formatting string to use.

Returns:
  None
--*********************************************************************/
VOID
PuDbgPrintError(
   IN OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFilePath,
   IN int                  nLineNum,
   IN const char *         pszFunctionName,
   IN DWORD                dwError,
   IN const char *         pszFormat,
   ...) // argsList
{
   STACK_STRA(straOutput, 256);
   va_list argsList;
   DWORD dwErr;

   // get a local copy of the error code so that it is not lost
  dwErr = GetLastError();

  va_start( argsList, pszFormat);
  FormatMsgToBuffer( &straOutput,
                     pDebugPrints,
                     pszFilePath,
                     nLineNum,
                     pszFunctionName,
                     pszFormat,
                     &argsList);

  va_end( argsList);


  //
  // obtain the formatted error message for error code
  //

  LPSTR lpErrorBuffer = NULL;
  DWORD nRet;
#pragma prefast(suppress: __WARNING_ANSI_APICALL,"debug spew is ansi")
  nRet =
      FormatMessageA((FORMAT_MESSAGE_ALLOCATE_BUFFER |
                      FORMAT_MESSAGE_FROM_SYSTEM),
                     NULL,     // lpSource
                     dwError,
                     LANG_NEUTRAL,
                     (LPSTR ) &lpErrorBuffer, // pointer to store buffer allocated
                     0,    // size of buffer
                     NULL  // lpArguments
                     );

  if (lpErrorBuffer)
  {
    CHAR pszErrorOut[64];  // 64 from: (/t=)4 + (Error(=)7 + (%x=)18 (0x + 16hex on 64 bit systems) + (): =)3 == 32 + some more slop
    _snprintf_s( pszErrorOut,
                 sizeof( pszErrorOut ) / sizeof( pszErrorOut[0] ),
                 sizeof(pszErrorOut) / sizeof(CHAR) - 1,            // leave space for NULL.
                 "\tError(%x): ",
                 dwError);
    pszErrorOut[63] = '\0';

    // if these appends fail, nothing to be done about it therefore just ignore the return values
    straOutput.Append(pszErrorOut);

    straOutput.Append(lpErrorBuffer);
    straOutput.Append("\n");
  }

  //
  // Send the outputs to respective files.
  //
  PupOutputMessage( pDebugPrints, &straOutput);

  // free the buffer if any was allocated
  if ( lpErrorBuffer != NULL) {
      LocalFree (lpErrorBuffer);
  }

  SetLastError( dwErr );

  return;
} // PuDbgPrintError()



VOID
PuDbgDump(
   IN OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFilePath,
   IN int                  nLineNum,
   IN const char *         pszFunctionName,
   IN const char *         pszDump
   )
{
   UNREFERENCED_PARAMETER( pszFunctionName );
   UNREFERENCED_PARAMETER( nLineNum );

   LPCSTR pszFileName = strrchr( pszFilePath, '\\');
   DWORD dwErr;
   DWORD cbDump;


   //
   //  Skip the complete path name and retain file name in pszName
   //

   if ( pszFileName== NULL) {

      pszFileName = pszFilePath;
   }

   dwErr = GetLastError();

   // No message header for this dump
   cbDump = (DWORD)strlen( pszDump);

   //
   // Send the outputs to respective files.
   //

   if ( pDebugPrints != NULL)
   {
       if ( ( pDebugPrints->m_dwOutputFlags & DbgOutputStderr) &&
            ( pDebugPrints->m_StdErrHandle != INVALID_HANDLE_VALUE ) ) {

           DWORD nBytesWritten;

           ( VOID) WriteFile( pDebugPrints->m_StdErrHandle,
                              pszDump,
                              cbDump,
                              &nBytesWritten,
                              NULL);
       }

       if ( pDebugPrints->m_dwOutputFlags & DbgOutputLogFile &&
            pDebugPrints->m_LogFileHandle != INVALID_HANDLE_VALUE) {

           DWORD nBytesWritten;

           //
           // Truncation of log files. Not yet implemented.

           ( VOID) WriteFile( pDebugPrints->m_LogFileHandle,
                              pszDump,
                              cbDump,
                              &nBytesWritten,
                              NULL);

       }

       if ( (pDebugPrints->m_dwOutputFlags & DbgOutputMemory) &&
            (NULL != pDebugPrints->m_pMemoryLog) )
       {
           CMemoryLog * pLog = (CMemoryLog*)(pDebugPrints->m_pMemoryLog);
           pLog->Append(pszDump, cbDump);
       }
   }

   if ( pDebugPrints == NULL
       ||  pDebugPrints->m_dwOutputFlags & DbgOutputKdb)
   {
       OutputDebugStringA( pszDump);
   }

   SetLastError( dwErr );

  return;
} // PuDbgDump()

//
// N.B. For PuDbgCaptureContext() to work properly, the calling function
// *must* be __cdecl, and must have a "normal" stack frame. So, we decorate
// PuDbgAssertFailed() with the __cdecl modifier and disable the frame pointer
// omission (FPO) optimization.
//

// DEBUGDEBUG
//#pragma optimize( "y", off )    // disable frame pointer omission (FPO)
#pragma optimize( "", off )

INT
__cdecl
PuDbgAssertFailed(
    IN OUT LPDEBUG_PRINTS         pDebugPrints,
    IN const char *               pszFilePath,
    IN int                        nLineNum,
    IN const char *               pszFunctionName,
    IN const char *               pszExpression,
    IN const char *               pszMessage)
/********************************************************************++
    This function calls assertion failure and records assertion failure
     in log file.

--*********************************************************************/

{
    PuDbgPrintAssertFailed( pDebugPrints, pszFilePath, nLineNum, pszFunctionName,
                            pszExpression,
                            pszMessage );
    if ( !g_fAvoidShutdownAsserts )
    {
        DebugBreak();
    }

    return 0;
} // PuDbgAssertFailed()

#pragma optimize( "", on )      // restore frame pointer omission (FPO)

INT
WINAPI
PuDbgPrintAssertFailed(
   IN OUT LPDEBUG_PRINTS   pDebugPrints,
   IN const char *         pszFilePath,
   IN int                  nLineNum,
   IN const char *         pszFunctionName,
   IN const char *         pszExpression,
   IN const char *         pszMessage)
/********************************************************************++
    This function calls assertion failure and records assertion failure
     in log file.

--*********************************************************************/

{
    PuDbgPrint( pDebugPrints, pszFilePath, nLineNum, pszFunctionName,
                " Assertion (%s) Failed: %s\n",
                pszExpression,
                pszMessage );
    return 0;
} // PuDbgPrintAssertFailed()



VOID
PuDbgPrintCurrentTime(
    IN OUT LPDEBUG_PRINTS         pDebugPrints,
    IN const char *               pszFilePath,
    IN int                        nLineNum,
    IN const char *               pszFunctionName
    )
/********************************************************************++
  This function generates the current time and prints it out to debugger
   for tracing out the path traversed, if need be.

  Arguments:
      pszFile    pointer to string containing the name of the file
      lineNum    line number within the file where this function is called.

  Returns:
      NO_ERROR always.
--*********************************************************************/

{
    PuDbgPrint( pDebugPrints, pszFilePath, nLineNum, pszFunctionName,
                " TickCount = %u\n",
                GetTickCount()
                );

    return;
} // PrintOutCurrentTime()




DWORD
PuLoadDebugFlagsFromReg(IN HKEY hkey, IN DWORD dwDefault)
/********************************************************************++
  This function reads the debug flags assumed to be stored in
   the location  "DebugFlags" under given key.
  If there is any error the default value is returned.
--*********************************************************************/

{
    DWORD err;
    DWORD dwDebug = dwDefault;
    DWORD  dwBuffer;
    DWORD  cbBuffer = sizeof(dwBuffer);
    DWORD  dwType;

    if( hkey != NULL )
    {
        err = RegQueryValueExA( hkey,
                               DEBUG_FLAGS_REGISTRY_LOCATION_A,
                               NULL,
                               &dwType,
                               (LPBYTE)&dwBuffer,
                               &cbBuffer );

        if( ( err == NO_ERROR ) && ( dwType == REG_DWORD ) )
        {
            dwDebug = dwBuffer;
        }
    }

    return dwDebug;
} // PuLoadDebugFlagsFromReg()




DWORD
PuLoadDebugFlagsFromRegStr(IN LPCSTR pszRegKey, IN DWORD dwDefault)
/********************************************************************++
Description:
  This function reads the debug flags assumed to be stored in
   the location  "DebugFlags" under given key location in registry.
  If there is any error the default value is returned.

Arguments:
  pszRegKey - pointer to registry key location from where to read the key from
  dwDefault - default values in case the read from registry fails

Returns:
   Newly read value on success
   If there is any error the dwDefault is returned.
--*********************************************************************/

{
    HKEY        hkey = NULL;

    DWORD dwVal = dwDefault;

    DWORD dwError = RegOpenKeyExA(HKEY_LOCAL_MACHINE,
                                  pszRegKey,
                                  0,
                                  KEY_READ,
                                  &hkey);
    if ( dwError == NO_ERROR) {
        dwVal = PuLoadDebugFlagsFromReg( hkey, dwDefault);
        RegCloseKey( hkey);
        hkey = NULL;
    }

    return ( dwVal);
} // PuLoadDebugFlagsFromRegStr()





DWORD
PuSaveDebugFlagsInReg(IN HKEY hkey, IN DWORD dwDbg)
/********************************************************************++
  Saves the debug flags in registry. On failure returns the error code for
   the operation that failed.

--*********************************************************************/
{
    DWORD err;

    if( hkey == NULL ) {

        err = ERROR_INVALID_PARAMETER;
    } else {

        err = RegSetValueExA(hkey,
                             DEBUG_FLAGS_REGISTRY_LOCATION_A,
                             0,
                             REG_DWORD,
                             (LPBYTE)&dwDbg,
                             sizeof(dwDbg) );
    }

    return (err);
} // PuSaveDebugFlagsInReg()


VOID
PuDbgCaptureContext (
    OUT PCONTEXT ContextRecord
    )
{
    //
    // This space intentionally left blank.
    //
	UNREFERENCED_PARAMETER( ContextRecord );
}   // PuDbgCaptureContext


/****************************** End of File ******************************/

