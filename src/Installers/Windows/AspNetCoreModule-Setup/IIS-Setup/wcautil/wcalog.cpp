// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

//-------------------------------------------------------------------------------------------------
// <summary>
//    Windows Installer XML CustomAction utility library logging functions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


/********************************************************************
 IsVerboseLogging() - internal helper function to detect if doing
                      verbose logging

********************************************************************/
static BOOL IsVerboseLogging()
{
	static int iVerbose = -1;

	if (0 > iVerbose)
	{
		iVerbose = WcaIsPropertySet("LOGVERBOSE");
		if (0 == iVerbose) // if the property wasn't set, check the registry to see if the logging policy was turned on
		{
			HKEY hkey = NULL;
			WCHAR rgwc[16] = { 0 };
			DWORD cb = sizeof(rgwc);
			if (ERROR_SUCCESS == ::RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"Software\\Policies\\Microsoft\\Windows\\Installer", 0, KEY_QUERY_VALUE, &hkey))
			{
				if (ERROR_SUCCESS == ::RegQueryValueExW(hkey, L"Logging", 0, NULL, reinterpret_cast<BYTE*>(rgwc), &cb))
				{
					for (LPCWSTR pwc = rgwc; (cb / sizeof(WCHAR)) > static_cast<DWORD>(pwc - rgwc) && *pwc; pwc++)
					{
						if (L'v' == *pwc || L'V' == *pwc)
						{
							iVerbose = 1;
							break;
						}
					}
				}

				::RegCloseKey(hkey);
			}
		}
	}

	Assert(iVerbose >= 0);
	return (BOOL)iVerbose;
}


/********************************************************************
 WcaLog() - outputs trace and log info

*******************************************************************/
extern "C" void WcaLog(
	__in LOGLEVEL llv,
	__in const char* fmt, ...
	)
{
	static char szFmt[LOG_BUFFER];
	static char szBuf[LOG_BUFFER];
	static bool fInLogPrint = false;

	// prevent re-entrant logprints.  (recursion issues between assert/logging code)
	if (fInLogPrint)
		return;
	fInLogPrint = true;

	if (LOGMSG_STANDARD == llv || 
	    (LOGMSG_VERBOSE == llv && IsVerboseLogging())
#ifdef DEBUG
	    || LOGMSG_TRACEONLY == llv
#endif
	    )
	{
		va_list args;
		va_start(args, fmt);

		LPCSTR szLogName = WcaGetLogName();
		if (szLogName[0] != 0)
			StringCchPrintfA(szFmt, countof(szFmt), "%s:  %s", szLogName, fmt);
		else
			StringCchCopyA(szFmt, countof(szFmt), fmt);

		StringCchVPrintfA(szBuf, countof(szBuf), szFmt, args);
		va_end(args);

#ifdef DEBUG
		// always write to the log in debug
#else
		if (llv == LOGMSG_STANDARD || (llv == LOGMSG_VERBOSE && IsVerboseLogging()))
#endif
		{
			PMSIHANDLE hrec = MsiCreateRecord(1);

			::MsiRecordSetStringA(hrec, 0, szBuf);
			// TODO:  Recursion on failure.  May not be safe to assert from here.
			WcaProcessMessage(INSTALLMESSAGE_INFO, hrec);
		}

#if DEBUG
		StringCchCatA(szBuf, countof(szBuf), "\n");
		OutputDebugStringA(szBuf);
#endif
	}

	fInLogPrint = false;
	return;
}


/********************************************************************
 WcaDisplayAssert() - called before Assert() dialog shows

 NOTE: writes the assert string to the MSI log
********************************************************************/
extern "C" BOOL WcaDisplayAssert(
	__in LPCSTR sz
	)
{
	WcaLog(LOGMSG_STANDARD, "Debug Assert Message: %s", sz);
	return TRUE;
}


/********************************************************************
 WcaLogError() - called before ExitOnXXX() macro exists the function

 NOTE: writes the hresult and error string to the MSI log
********************************************************************/
extern "C" void WcaLogError(
	__in HRESULT hr,
	__in LPCSTR szMessage,
	...
	)
{
	char szBuffer[LOG_BUFFER];
	va_list dots;

	va_start(dots, szMessage);
	StringCchVPrintfA(szBuffer, countof(szBuffer), szMessage, dots);
	va_end(dots);

	// log the message if using Wca common layer
	if (WcaIsInitialized())
		WcaLog(LOGMSG_STANDARD, "Error 0x%x: %s", hr, szBuffer);
}
