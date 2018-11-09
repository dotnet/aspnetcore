// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

//-------------------------------------------------------------------------------------------------
// <summary>
//    Windows Installer XML CustomAction utility library wrappers for MSI API
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


/********************************************************************
 WcaProcessMessage() - sends a message from the CustomAction

********************************************************************/
extern "C" UINT WcaProcessMessage(
	__in INSTALLMESSAGE eMessageType,
	__in MSIHANDLE hRecord
	)
{
	UINT er = ::MsiProcessMessage(WcaGetInstallHandle(), eMessageType, hRecord);
	if (ERROR_INSTALL_USEREXIT == er || IDCANCEL == er)
		WcaSetReturnValue(ERROR_INSTALL_USEREXIT);

	return er;
}


/********************************************************************
 WcaErrorMessage() - sends an error message from the CustomAction using 
                     the Error table

 NOTE: Any and all var_args (...) must be WCHAR*
********************************************************************/
extern "C" UINT WcaErrorMessage(
	__in int iError, 
	__in HRESULT hrError, 
	__in UINT uiType, 
	__in DWORD cArgs, 
	...
	)
{
	UINT er;
	MSIHANDLE hRec = NULL;
	va_list args;

	uiType |= INSTALLMESSAGE_ERROR;  // ensure error type is set
	hRec = ::MsiCreateRecord(cArgs + 2);
	if (!hRec)
	{
		er = ERROR_OUTOFMEMORY;
		ExitOnFailure(HRESULT_FROM_WIN32(er), "failed to create record when sending error message");
	}

	er = ::MsiRecordSetInteger(hRec, 1, iError);
	ExitOnFailure(HRESULT_FROM_WIN32(er), "failed to set error code into error message");

	er = ::MsiRecordSetInteger(hRec, 2, hrError);
	ExitOnFailure(HRESULT_FROM_WIN32(er), "failed to set hresult code into error message");

	va_start(args, cArgs);
	for (DWORD i = 0; i < cArgs; i++)
	{
		er = ::MsiRecordSetStringW(hRec, i + 3, va_arg(args, WCHAR*));
		ExitOnFailure(HRESULT_FROM_WIN32(er), "failed to set string string into error message");
	}
	va_end(args);

	er = WcaProcessMessage(static_cast<INSTALLMESSAGE>(uiType), hRec);
LExit:
	if (hRec)
		::MsiCloseHandle(hRec);

	return er;
}


/********************************************************************
 WcaProgressMessage() - extends the progress bar or sends a progress 
                        update from the CustomAction

********************************************************************/
extern "C" HRESULT WcaProgressMessage(
	__in UINT uiCost,
	__in BOOL fExtendProgressBar
	)
{
	static BOOL fExplicitProgressMessages = FALSE;

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	MSIHANDLE hRec = ::MsiCreateRecord(3);

	// if aren't extending the progress bar and we haven't switched into explicit message mode
	if (!fExtendProgressBar && !fExplicitProgressMessages)
	{
		AssertSz(::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_SCHEDULED) ||
		         ::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_COMMIT) ||
		         ::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_ROLLBACK), "can only send progress bar messages in a deferred CustomAction");

		// tell Darwin to use explicit progress messages
		::MsiRecordSetInteger(hRec, 1, 1);
		::MsiRecordSetInteger(hRec, 2, 1);
		::MsiRecordSetInteger(hRec, 3, 0);

		er = WcaProcessMessage(INSTALLMESSAGE_PROGRESS, hRec);
		if (0 == er || IDOK == er || IDYES == er)
			hr = S_OK;
		else if (IDABORT == er || IDCANCEL == er)
		{
			WcaSetReturnValue(ERROR_INSTALL_USEREXIT); // note that the user said exit
			ExitFunction1(hr = S_FALSE);
		}
		else
			hr = E_UNEXPECTED;
		ExitOnFailure(hr, "failed to tell Darwin to use explicit progress messages");

		fExplicitProgressMessages = TRUE;
	}
#if DEBUG
	else if (fExtendProgressBar)   // if we are extending the progress bar, make sure we're not deferred
	{
		AssertSz(!::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_SCHEDULED), "cannot add ticks to progress bar length from deferred CustomAction");
	}
#endif

	// send the progress message
	::MsiRecordSetInteger(hRec, 1, (fExtendProgressBar) ? 3 : 2);
	::MsiRecordSetInteger(hRec, 2, uiCost);
	::MsiRecordSetInteger(hRec, 3, 0);

	er = WcaProcessMessage(INSTALLMESSAGE_PROGRESS, hRec);
	if (0 == er || IDOK == er || IDYES == er)
	{
		hr = S_OK;
	}
	else if (IDABORT == er || IDCANCEL == er)
	{
		WcaSetReturnValue(ERROR_INSTALL_USEREXIT); // note that the user said exit
		hr = S_FALSE;
	}
	else
		hr = E_UNEXPECTED;

LExit:
	if (hRec)
		::MsiCloseHandle(hRec);

	return hr;
}


/********************************************************************
 WcaIsInstalling() - determines if a pair of installstates means install

********************************************************************/
extern "C" BOOL WcaIsInstalling(
	__in INSTALLSTATE isInstalled,
	__in INSTALLSTATE isAction
	)
{
	return (INSTALLSTATE_LOCAL == isAction ||
	        INSTALLSTATE_SOURCE == isAction ||
	        (INSTALLSTATE_DEFAULT == isAction &&
	         (INSTALLSTATE_LOCAL == isInstalled ||
	          INSTALLSTATE_SOURCE == isInstalled)));
}

/********************************************************************
 WcaIsReInstalling() - determines if a pair of installstates means install

********************************************************************/
extern "C" BOOL WcaIsReInstalling(
	__in INSTALLSTATE isInstalled,
	__in INSTALLSTATE isAction
	)
{
	return ((INSTALLSTATE_LOCAL == isAction ||
			INSTALLSTATE_SOURCE == isAction ||
			INSTALLSTATE_DEFAULT == isAction) &&
			(INSTALLSTATE_LOCAL == isInstalled ||
			INSTALLSTATE_SOURCE == isInstalled));
}


/********************************************************************
 WcaIsUninstalling() - determines if a pair of installstates means uninstall

********************************************************************/
extern "C" BOOL WcaIsUninstalling(
	__in INSTALLSTATE isInstalled,
	__in INSTALLSTATE isAction
	)
{
	return ((INSTALLSTATE_ABSENT == isAction ||
	         INSTALLSTATE_REMOVED == isAction) &&
	        (INSTALLSTATE_LOCAL == isInstalled ||
	         INSTALLSTATE_SOURCE == isInstalled));
}


/********************************************************************
 WcaSetComponentState() - sets the install state of a Component

********************************************************************/
extern "C" HRESULT WcaSetComponentState(
	__in LPCWSTR wzComponent,
	__in INSTALLSTATE isState
	)
{
	UINT er = ::MsiSetComponentStateW(WcaGetInstallHandle(), wzComponent, isState);
	if (ERROR_INSTALL_USEREXIT == er)
		WcaSetReturnValue(er);

	return HRESULT_FROM_WIN32(er);
}


/********************************************************************
 WcaTableExists() - determines if installing database contains a table

********************************************************************/
extern "C" HRESULT WcaTableExists(
	__in LPCWSTR wzTable
	)
{
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

// NOTE:  The following line of commented out code should work in a 
//        CustomAction but does not in Windows Installer v1.1
	// er = ::MsiDatabaseIsTablePersistentW(hDatabase, wzTable);

	// a "most elegant" workaround a Darwin v1.1 bug
	PMSIHANDLE hRec;
	er = ::MsiDatabaseGetPrimaryKeysW(WcaGetDatabaseHandle(), wzTable, &hRec);

	if (ERROR_SUCCESS == er)
		hr = S_OK;
	else if (ERROR_INVALID_TABLE == er)
		hr = S_FALSE;
	else
		hr = E_FAIL;
	Assert(SUCCEEDED(hr));

	return hr;
}


/********************************************************************
 WcaOpenView() - opens a view on the installing database

********************************************************************/
extern "C" HRESULT WcaOpenView(
	__in LPCWSTR wzSql,
	__out MSIHANDLE* phView
	)
{
	if (!wzSql || !*wzSql|| !phView)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er = ::MsiDatabaseOpenViewW(WcaGetDatabaseHandle(), wzSql, phView);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure1(hr, "failed to open view on database with SQL: %S", wzSql);

LExit:
	return hr;
}


/********************************************************************
 WcaExecuteView() - executes a parameterized open view on the installing database

********************************************************************/
extern "C" HRESULT WcaExecuteView(
	__in MSIHANDLE hView,
	__in MSIHANDLE hRec
	)
{
	if (!hView)
		return E_INVALIDARG;
	AssertSz(hRec, "Use WcaOpenExecuteView() if you don't need to pass in a record");

	HRESULT hr = S_OK;
	UINT er = ::MsiViewExecute(hView, hRec);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "failed to execute view");

LExit:
	return hr;
}


/********************************************************************
 WcaOpenExecuteView() - opens and executes a view on the installing database

********************************************************************/
extern "C" HRESULT WcaOpenExecuteView(
	__in LPCWSTR wzSql,
	__out MSIHANDLE* phView
	)
{
	if (!wzSql || !*wzSql|| !phView)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er = ::MsiDatabaseOpenViewW(WcaGetDatabaseHandle(), wzSql, phView);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "failed to open view on database");

	er = ::MsiViewExecute(*phView, NULL);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "failed to execute view");

LExit:
	return hr;
}


/********************************************************************
 WcaFetchRecord() - gets the next record from a view on the installing database

********************************************************************/
extern "C" HRESULT WcaFetchRecord(
	__in MSIHANDLE hView,
	__out MSIHANDLE* phRec
	)
{
	if (!hView|| !phRec)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er = ::MsiViewFetch(hView, phRec);
	hr = HRESULT_FROM_WIN32(er);
	if (FAILED(hr) && E_NOMOREITEMS != hr)
		ExitOnFailure(hr, "failed to fetch record from view");

LExit:
	return hr;
}


/********************************************************************
 WcaFetchSingleRecord() - gets a single record from a view on the installing database

********************************************************************/
extern "C" HRESULT WcaFetchSingleRecord(
	__in MSIHANDLE hView,
	__out MSIHANDLE* phRec
	)
{
	if (!hView|| !phRec)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er = ::MsiViewFetch(hView, phRec);
	if (ERROR_NO_MORE_ITEMS == er)
		hr = S_FALSE;
	else
		hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "failed to fetch single record from view");

#ifdef DEBUG // only do this in debug to verify that a single record was returned
	MSIHANDLE hRecTest;
	er = ::MsiViewFetch(hView, &hRecTest);
	AssertSz(ERROR_NO_MORE_ITEMS == er && NULL == hRecTest, "WcaSingleFetch() did not fetch a single record");
	::MsiCloseHandle(hRecTest);
#endif

LExit:
	return hr;
}


/********************************************************************
 WcaGetProperty - gets a string property value from the active install

********************************************************************/
extern "C" HRESULT WcaGetProperty(
	__in LPCWSTR wzProperty,
	__inout LPWSTR* ppwzData
	)
{
	if (!wzProperty || !*wzProperty || !ppwzData)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	DWORD_PTR cch = 0;

	if (!*ppwzData)
	{
		er = ::MsiGetPropertyW(WcaGetInstallHandle(), wzProperty, L"", (DWORD *)&cch);
		if (ERROR_MORE_DATA == er || ERROR_SUCCESS == er)
		{
			hr = StrAlloc(ppwzData, ++cch);
		}
		else
			hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure1(hr, "Failed to allocate string for Property '%S'", wzProperty);
	}
	else
	{
		hr = StrMaxLength(*ppwzData, &cch);
		ExitOnFailure(hr, "Failed to get previous size of property data string.");
	}

	er = ::MsiGetPropertyW(WcaGetInstallHandle(), wzProperty, *ppwzData, (DWORD *)&cch);
	if (ERROR_MORE_DATA == er)
	{
		Assert(*ppwzData);
		hr = StrAlloc(ppwzData, ++cch);
		ExitOnFailure1(hr, "Failed to allocate string for Property '%S'", wzProperty);

		er = ::MsiGetPropertyW(WcaGetInstallHandle(), wzProperty, *ppwzData, (DWORD *)&cch);
	}
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure1(hr, "Failed to get data for property '%S'", wzProperty);

LExit:
	return hr;
}


/********************************************************************
 WcaGetFormattedProperty - gets a formatted string property value from 
                           the active install

********************************************************************/
extern "C" HRESULT WcaGetFormattedProperty(
	__in LPCWSTR wzProperty,
	__out LPWSTR* ppwzData
	)
{
	if (!wzProperty || !*wzProperty || !ppwzData)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	LPWSTR pwzPropertyValue = NULL;
	

	hr = WcaGetProperty(wzProperty, &pwzPropertyValue);
	ExitOnFailure1(hr, "failed to get %S", wzProperty);

	hr = WcaGetFormattedString(pwzPropertyValue, ppwzData);
	ExitOnFailure2(hr, "failed to get formatted value for property: '%S' with value: '%S'", wzProperty, pwzPropertyValue);

LExit:
	ReleaseStr(pwzPropertyValue);
	return hr;
}


/********************************************************************
 WcaGetFormattedString - gets a formatted string value from 
                           the active install

********************************************************************/
extern "C" HRESULT WcaGetFormattedString(
	__in LPCWSTR wzString,
	__out LPWSTR* ppwzData
	)
{
	if (!wzString || !*wzString || !ppwzData)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;
	PMSIHANDLE hRecord = ::MsiCreateRecord(1);
	DWORD_PTR cch = 0;

	er = ::MsiRecordSetStringW(hRecord, 0, wzString);
	ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "Failed to set record field 0 with '%S'", wzString);

	if (!*ppwzData)
	{
		er = ::MsiFormatRecordW(WcaGetInstallHandle(), hRecord, L"", (DWORD *)&cch);
		if (ERROR_MORE_DATA == er || ERROR_SUCCESS == er)
		{
			hr = StrAlloc(ppwzData, ++cch);
		}
		else
			hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure1(hr, "Failed to allocate string for formatted string: '%S'", wzString);
	}
	else
	{
		hr = StrMaxLength(*ppwzData, &cch);
		ExitOnFailure(hr, "Failed to get previous size of property data string");
	}

	er = ::MsiFormatRecordW(WcaGetInstallHandle(), hRecord, *ppwzData, (DWORD *)&cch);
	if (ERROR_MORE_DATA == er)
	{
		hr = StrAlloc(ppwzData, ++cch);
		ExitOnFailure1(hr, "Failed to allocate string for formatted string: '%S'", wzString);

		er = ::MsiFormatRecordW(WcaGetInstallHandle(), hRecord, *ppwzData, (DWORD *)&cch);
	}
	ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "Failed to get formatted string: '%S'", wzString);

LExit:
	return hr;
}


/********************************************************************
 WcaGetIntProperty - gets an integer property value from the active install

********************************************************************/
extern "C" HRESULT WcaGetIntProperty(
	__in LPCWSTR wzProperty,
	__inout int* piData
	)
{
	if (!piData)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er;

	WCHAR wzValue[32];
	DWORD cch = countof(wzValue) - 1;

	er = ::MsiGetPropertyW(WcaGetInstallHandle(), wzProperty, wzValue, &cch);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure1(hr, "Failed to get data for property '%S'", wzProperty);

	*piData = wcstol(wzValue, NULL, 10);

LExit:
	return hr;
}


/********************************************************************
 WcaGetTargetPath - gets the target path for a specified folder

********************************************************************/
extern "C" HRESULT WcaGetTargetPath(
	__in LPCWSTR wzFolder,
	__out LPWSTR* ppwzData
	)
{
	if (!wzFolder || !*wzFolder || !ppwzData)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	
	UINT er = ERROR_SUCCESS;
	DWORD_PTR cch = 0;

	if (!*ppwzData)
	{
		er = ::MsiGetTargetPathW(WcaGetInstallHandle(), wzFolder, L"", (DWORD*)&cch);
		if (ERROR_MORE_DATA == er || ERROR_SUCCESS == er)
		{
			cch++; //Add one for the null terminator
			hr = StrAlloc(ppwzData, cch);
		}
		else
			hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure1(hr, "Failed to allocate string for target path of folder: '%S'", wzFolder);
	}
	else
	{
		hr = StrMaxLength(*ppwzData, &cch);
		ExitOnFailure(hr, "Failed to get previous size of string");
	}

	er = ::MsiGetTargetPathW(WcaGetInstallHandle(), wzFolder, *ppwzData, (DWORD*)&cch);
	if (ERROR_MORE_DATA == er)
	{
		cch++;
		hr = StrAlloc(ppwzData, cch);
		ExitOnFailure1(hr, "Failed to allocate string for target path of folder: '%S'", wzFolder);

		er = ::MsiGetTargetPathW(WcaGetInstallHandle(), wzFolder, *ppwzData, (DWORD*)&cch);
	}
	ExitOnFailure1(hr = HRESULT_FROM_WIN32(er), "Failed to get target path for folder '%S'", wzFolder);

LExit:
	return hr;
}


/********************************************************************
 WcaSetProperty - sets a string property value in the active install

********************************************************************/
extern "C" HRESULT WcaSetProperty(
	__in LPCWSTR wzPropertyName,
	__in LPCWSTR wzPropertyValue
	)
{
	if (!wzPropertyName || !*wzPropertyName || !wzPropertyValue)
		return E_INVALIDARG;

	UINT er = ::MsiSetPropertyW(WcaGetInstallHandle(), wzPropertyName, wzPropertyValue);
	HRESULT hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure1(hr, "failed to set property: %S", wzPropertyName);

LExit:
	return hr;
}


/********************************************************************
 WcaSetIntProperty - sets a integer property value in the active install

********************************************************************/
extern "C" HRESULT WcaSetIntProperty(
	__in LPCWSTR wzPropertyName,
	__in int nPropertyValue
	)
{
	if (!wzPropertyName || !*wzPropertyName)
		return E_INVALIDARG;

	// 12 characters should be enough for a 32-bit int: 10 digits, 1 sign, 1 null
	WCHAR wzPropertyValue[13];
	HRESULT hr = StringCchPrintfW(wzPropertyValue, countof(wzPropertyValue), L"%d", nPropertyValue);
	ExitOnFailure1(hr, "failed to convert into string property value: %d", nPropertyValue);

	UINT er = ::MsiSetPropertyW(WcaGetInstallHandle(), wzPropertyName, wzPropertyValue);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure1(hr, "failed to set property: %S", wzPropertyName);

LExit:
	return hr;
}


/********************************************************************
 WcaIsPropertySet() - returns TRUE if property is set

********************************************************************/
extern "C" BOOL WcaIsPropertySet(
	__in LPCSTR szProperty
	)
{
	DWORD cchProperty = 0;
	UINT er = ::MsiGetPropertyA(WcaGetInstallHandle(), szProperty, "", &cchProperty);
	AssertSz(ERROR_INVALID_PARAMETER != er && ERROR_INVALID_HANDLE != er, "Unexpected return value from ::MsiGetProperty()");

	return 0 < cchProperty; // property is set if the length is greater than zero
}


/********************************************************************
 WcaGetRecordInteger() - gets an integer field out of a record

 NOTE: returns S_FALSE if the field was null
********************************************************************/
extern "C" HRESULT WcaGetRecordInteger(
	__in MSIHANDLE hRec,
	__in UINT uiField,
	__inout int* piData
	)
{
	if (!hRec || !piData)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	*piData = ::MsiRecordGetInteger(hRec, uiField);
	if (MSI_NULL_INTEGER == *piData)
		hr = S_FALSE;

//LExit:
	return hr;
}


/********************************************************************
 WcaGetRecordString() - gets a string field out of a record

********************************************************************/
extern "C" HRESULT WcaGetRecordString(
	__in MSIHANDLE hRec,
	__in UINT uiField,
	__inout LPWSTR* ppwzData
	)
{
	if (!hRec || !ppwzData)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er;
	DWORD_PTR cch = 0;

	if (!*ppwzData)
	{
		er = ::MsiRecordGetStringW(hRec, uiField, L"", (DWORD*)&cch);
		if (ERROR_MORE_DATA == er || ERROR_SUCCESS == er)
		{
			hr = StrAlloc(ppwzData, ++cch);
		}
		else
			hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure(hr, "Failed to allocate memory for record string");
	}
	else
	{
		hr = StrMaxLength(*ppwzData, &cch);
		ExitOnFailure(hr, "Failed to get previous size of string");
	}

	er = ::MsiRecordGetStringW(hRec, uiField, *ppwzData, (DWORD*)&cch);
	if (ERROR_MORE_DATA == er)
	{
		hr = StrAlloc(ppwzData, ++cch);
		ExitOnFailure(hr, "Failed to allocate memory for record string");

		er = ::MsiRecordGetStringW(hRec, uiField, *ppwzData, (DWORD*)&cch);
	}
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "Failed to get string from record");

LExit:
	return hr;
}


/********************************************************************
 HideNulls() - internal helper function to escape [~] in formatted strings

********************************************************************/
static void HideNulls(
	__in LPWSTR wzData
	)
{
	LPWSTR pwz = wzData;

	while(*pwz)
	{
		if (pwz[0] == L'[' && pwz[1] == L'~' && pwz[2] == L']') // found a null [~]
		{
			pwz[0] = L'!'; // turn it into !$!
			pwz[1] = L'$';
			pwz[2] = L'!';
			pwz += 3;
		}
		else
			pwz++;
	}
}


/********************************************************************
 RevealNulls() - internal helper function to unescape !$! in formatted strings

********************************************************************/
static void RevealNulls(
	__in LPWSTR wzData
	)
{
	LPWSTR pwz = wzData;

	while(*pwz)
	{
		if (pwz[0] == L'!' && pwz[1] == L'$' && pwz[2] == L'!') // found the fake null !$!
		{
			pwz[0] = L'['; // turn it back into [~]
			pwz[1] = L'~';
			pwz[2] = L']';
			pwz += 3;
		}
		else
			pwz++;
	}
}


/********************************************************************
 WcaGetRecordFormattedString() - gets formatted string filed from record

********************************************************************/
extern "C" HRESULT WcaGetRecordFormattedString(
	__in MSIHANDLE hRec,
	__in UINT uiField,
	__inout LPWSTR* ppwzData
	)
{
	if (!hRec || !ppwzData)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er;
	DWORD_PTR cch = 0;
	PMSIHANDLE hRecFormat;

	// get the format string
	hr = WcaGetRecordString(hRec, uiField, ppwzData);
	ExitOnFailure(hr, "failed to get string from record");

	if (!**ppwzData)
		ExitFunction();

	// hide the nulls '[~]' so we can get them back after formatting
	HideNulls(*ppwzData);

	// set up the format record
	hRecFormat = ::MsiCreateRecord(1);
	ExitOnNull(hRecFormat, hr, E_UNEXPECTED, "Failed to create record to format string");
	hr = WcaSetRecordString(hRecFormat, 0, *ppwzData);
	ExitOnFailure(hr, "failed to set string to format record");

	// format the string
	hr = StrMaxLength(*ppwzData, &cch);
	ExitOnFailure(hr, "failed to get max length of string");

	er = ::MsiFormatRecordW(WcaGetInstallHandle(), hRecFormat, *ppwzData, (DWORD*)&cch);
	if (ERROR_MORE_DATA == er)
	{
		hr = StrAlloc(ppwzData, ++cch);
		ExitOnFailure(hr, "Failed to allocate memory for record string");

		er = ::MsiFormatRecordW(WcaGetInstallHandle(), hRecFormat, *ppwzData, (DWORD*)&cch);
	}
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "Failed to format string");

	// put the nulls back
	RevealNulls(*ppwzData);

LExit:
	return hr;
}


/********************************************************************
 WcaAllocStream() - creates a byte stream of the specified size

 NOTE: Use WcaFreeStream() to release the byte stream
********************************************************************/
extern "C" HRESULT WcaAllocStream(
	__inout BYTE** ppbData,
	__in DWORD cbData
	)
{
	Assert(ppbData);
	HRESULT hr;
	BYTE* pbNewData;

	if (*ppbData)
		pbNewData = (BYTE*)MemReAlloc(*ppbData, cbData, TRUE);
	else
		pbNewData = (BYTE*)MemAlloc(cbData, TRUE);

	if (!pbNewData)
		ExitOnLastError(hr, "Failed to allocate string");
	*ppbData = pbNewData;
	pbNewData = NULL;

	hr = S_OK;
LExit:
	if (pbNewData)
		MemFree(pbNewData);

	return hr;
}


/********************************************************************
 WcaFreeStream() - frees a byte stream

********************************************************************/
extern "C" HRESULT WcaFreeStream(
	__in BYTE* pbData
	)
{
	if (!pbData)
		return E_INVALIDARG;

	HRESULT hr = MemFree(pbData);
	return hr;
}


/********************************************************************
 WcaReadRecordStream() - gets a byte stream field from record

********************************************************************/
extern "C" HRESULT WcaGetRecordStream(
	__in MSIHANDLE hRecBinary,
	__in UINT uiField, 
	__inout BYTE** ppbData,
	__inout DWORD* pcbData
	)
{
	HRESULT hr = S_OK;
	UINT er = ERROR_SUCCESS;

	if (!hRecBinary || !ppbData || !pcbData)
		return E_INVALIDARG;

	*pcbData = 0;
	er = ::MsiRecordReadStream(hRecBinary, uiField, NULL, pcbData);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "failed to get size of stream");

	hr = WcaAllocStream(ppbData, *pcbData);
	ExitOnFailure(hr, "failed to allocate data for stream");

	er = ::MsiRecordReadStream(hRecBinary, uiField, (char*)*ppbData, pcbData);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "failed to read from stream");

LExit:
	return hr;
}


/********************************************************************
 WcaSetRecordString() - set a string field in record

********************************************************************/
extern "C" HRESULT WcaSetRecordString(
	__in MSIHANDLE hRec,
	__in UINT uiField,
	__in LPCWSTR wzData
	)
{
	if (!hRec || !wzData)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er = ::MsiRecordSetStringW(hRec, uiField, wzData);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "failed to set string in record");

LExit:
	return hr;
}


/********************************************************************
 WcaSetRecordInteger() - set a integer field in record

********************************************************************/
extern "C" HRESULT WcaSetRecordInteger(
	__in MSIHANDLE hRec,
	__in UINT uiField,
	__in int iValue
	)
{
	if (!hRec)
		return E_INVALIDARG;

	HRESULT hr = S_OK;
	UINT er = ::MsiRecordSetInteger(hRec, uiField, iValue);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "failed to set integer in record");

LExit:
	return hr;
}


/********************************************************************

 WcaDoDeferredAction() - schedules an action at this point in the script

********************************************************************/
extern "C" HRESULT WcaDoDeferredAction(
	__in LPCWSTR wzAction,
	__in LPCWSTR wzCustomActionData,
	__in UINT uiCost
	)
{
	HRESULT hr = S_OK;
	UINT er;

	if (wzCustomActionData && *wzCustomActionData)
	{
		er = ::MsiSetPropertyW(WcaGetInstallHandle(), wzAction, wzCustomActionData);
		hr = HRESULT_FROM_WIN32(er);
		ExitOnFailure(hr, "Failed to set CustomActionData for deferred action");
	}

	if (0 < uiCost)
	{
		hr = WcaProgressMessage(uiCost, TRUE);  // add ticks to the progress bar
		// TODO: handle the return codes correctly
	}

	er = ::MsiDoActionW(WcaGetInstallHandle(), wzAction);
	hr = HRESULT_FROM_WIN32(er);
	ExitOnFailure(hr, "Failed MsiDoAction on deferred action");

LExit:
	return hr;
}


/********************************************************************
 WcaCountOfCustomActionDataRecords() - counts the number of records 
                                       passed to a deferred CustomAction

********************************************************************/
extern "C" DWORD WcaCountOfCustomActionDataRecords(
	__in LPCWSTR wzData
	)
{
	if (!wzData)
		return E_INVALIDARG;

	WCHAR delim[] = {MAGIC_MULTISZ_DELIM, 0}; // magic char followed by NULL terminator
	DWORD dwCount = 0;

	// Loop through until there are no delimiters, we are at the end of the string, or the delimiter is the last character in the string
	for (LPCWSTR pwzCurrent = wzData; pwzCurrent && *pwzCurrent && *(pwzCurrent + 1); pwzCurrent = wcsstr(pwzCurrent, delim))
	{
		dwCount++;
		pwzCurrent++;
	}

	return dwCount;
}


/********************************************************************
 BreakDownCustomActionData() - internal helper to chop up CustomActionData

 NOTE: this modifies the passed in data
********************************************************************/
static LPWSTR BreakDownCustomActionData(
	__inout LPWSTR* ppwzData
	)
{
	if (!ppwzData)
		return NULL;
	if (0 == *ppwzData)
		return NULL;

	WCHAR delim[] = {MAGIC_MULTISZ_DELIM, 0}; // magic char followed by Null terminator

	LPWSTR pwzReturn = *ppwzData;
	LPWSTR pwz = wcsstr(pwzReturn, delim);
	if (pwz)
	{
		*pwz = 0;
		*ppwzData = pwz + 1;
	}
	else
		*ppwzData = 0;

	return pwzReturn;
}


/********************************************************************
 WcaReadStringFromCaData() - reads a string out of the CustomActionData

 NOTE: this modifies the passed in ppwzCustomActionData variable
********************************************************************/
extern "C" HRESULT WcaReadStringFromCaData(
	__inout LPWSTR* ppwzCustomActionData,
	__inout LPWSTR* ppwzString
	)
{
	HRESULT hr = S_OK;

	LPCWSTR pwz = BreakDownCustomActionData(ppwzCustomActionData);
	if (!pwz)
		return E_NOMOREITEMS;

	hr = StrAllocString(ppwzString, pwz, 0);
	ExitOnFailure(hr, "failed to allocate memory for string");

	hr  = S_OK;
LExit:
	return hr;
}


/********************************************************************
 WcaReadIntegerFromCaData() - reads an integer out of the CustomActionData

 NOTE: this modifies the passed in ppwzCustomActionData variable
********************************************************************/
extern "C" HRESULT WcaReadIntegerFromCaData(
	__inout LPWSTR* ppwzCustomActionData,
	__inout int* piResult
	)
{
	LPCWSTR pwz = BreakDownCustomActionData(ppwzCustomActionData);
	if (!pwz)
		return E_NOMOREITEMS;

	*piResult = wcstol(pwz, NULL, 10);
	return S_OK;
}


/********************************************************************
 WcaReadStreamFromCaData() - reads a stream out of the CustomActionData

 NOTE: this modifies the passed in ppwzCustomActionData variable
 NOTE: returned stream should be freed with WcaFreeStream()
********************************************************************/
extern "C" HRESULT WcaReadStreamFromCaData(
	__inout LPWSTR* ppwzCustomActionData,
	__out BYTE** ppbData,
	__out DWORD_PTR* pcbData
	)
{
	HRESULT hr;

	LPCWSTR pwz = BreakDownCustomActionData(ppwzCustomActionData);
	if (!pwz)
		return E_NOMOREITEMS;

	hr = StrAllocBase85Decode(pwz, ppbData, pcbData);
	ExitOnFailure(hr, "failed to decode string into stream");

LExit:
	return hr;
}


/********************************************************************
 WcaWriteStringToCaData() - adds a string to the CustomActionData to 
                            feed a deferred CustomAction

********************************************************************/
extern "C" HRESULT WcaWriteStringToCaData(
	__in LPCWSTR wzString,
	__inout LPWSTR* ppwzCustomActionData
	)
{
	HRESULT hr = S_OK;
	WCHAR delim[] = {MAGIC_MULTISZ_DELIM, 0}; // magic char followed by NULL terminator

	if (!ppwzCustomActionData)
		return E_INVALIDARG;

	DWORD cchString = lstrlenW(wzString) + 1; // assume we'll be adding the delim
	DWORD_PTR cchCustomActionData = 0;

	if (*ppwzCustomActionData)
	{
		hr = StrMaxLength(*ppwzCustomActionData, &cchCustomActionData);
		ExitOnFailure(hr, "failed to get length of custom action data");
	}

	if ((cchCustomActionData - lstrlenW(*ppwzCustomActionData)) < cchString + 1)
	{
		cchCustomActionData += cchString + 1 + 255;  // add 255 for good measure
		hr = StrAlloc(ppwzCustomActionData, cchCustomActionData);
		ExitOnFailure(hr, "Failed to allocate memory for CustomActionData string");
		ExitOnNull(*ppwzCustomActionData, hr, E_OUTOFMEMORY, "Failed to allocate memory for CustomActionData string");
	}

	if (**ppwzCustomActionData) // if data exists toss the delimiter on before adding more to the end
		StringCchCatW(*ppwzCustomActionData, cchCustomActionData, delim);
	StringCchCatW(*ppwzCustomActionData, cchCustomActionData, wzString);

LExit:
	return hr;
}


/********************************************************************
 WcaWriteStringToCaData() - adds an integer to the CustomActionData to 
                            feed a deferred CustomAction

********************************************************************/
extern "C" HRESULT WcaWriteIntegerToCaData(
	__in int i, 
	__inout LPWSTR* ppwzCustomActionData
	)
{
	WCHAR wzBuffer[13];
	StringCchPrintfW(wzBuffer, countof(wzBuffer), L"%d", i);

	return WcaWriteStringToCaData(wzBuffer, ppwzCustomActionData);
}


/********************************************************************
 WcaWriteStringToCaData() - adds a byte stream to the CustomActionData to 
                            feed a deferred CustomAction

********************************************************************/
extern "C" HRESULT WcaWriteStreamToCaData(
	__in_bcount(cbData) const BYTE* pbData,
	__in DWORD cbData,
	__inout LPWSTR* ppwzCustomActionData
	)
{
	HRESULT hr;
	LPWSTR pwzData = NULL;

	hr = StrAllocBase85Encode(pbData, cbData, &pwzData);
	ExitOnFailure(hr, "failed to encode data into string");

	hr = WcaWriteStringToCaData(pwzData, ppwzCustomActionData);

LExit:
	ReleaseStr(pwzData);
	return hr;
}


/********************************************************************
WcaAddTempRecord - adds a temporary record to the active database

NOTE: you cannot use PMSIHANDLEs for the __in/__out parameters
NOTE: uiUniquifyColumn can be 0 if no column needs to be made unique
********************************************************************/
extern "C" HRESULT WcaAddTempRecord(
    __inout MSIHANDLE* phTableView,
    __inout MSIHANDLE* phColumns,
    __in LPCWSTR wzTable,
    __in UINT uiUniquifyColumn,
    __in UINT cColumns,
    ...
    )
{
    Assert(phTableView && phColumns);

    static DWORD dwUniquifyValue = ::GetTickCount();

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzQuery = NULL;
    PMSIHANDLE hTempRec;
    DWORD i;
    va_list args;

    LPWSTR pwzData = NULL;
    LPWSTR pwzUniquify = NULL;

    //
    // if we don't have a table and it's columns already
    //
    if (NULL == *phTableView)
    {
        // set the query
        hr = StrAllocFormatted(&pwzQuery, L"SELECT * FROM `%s`",wzTable);
        ExitOnFailure(hr, "failed to allocate string for query");

        // Open and Execute the temp View
        hr = WcaOpenExecuteView(pwzQuery, phTableView);
        ExitOnFailure1(hr, "failed to openexecute temp view with query %S", pwzQuery);
    }

    if (NULL == *phColumns)
    {
        // use GetColumnInfo to populate the datatype record
        er = ::MsiViewGetColumnInfo(*phTableView, MSICOLINFO_TYPES, phColumns);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure1(hr, "failed to columns for table: %S", wzTable);
    }
    AssertSz(::MsiRecordGetFieldCount(*phColumns) == cColumns, "passed in argument does not match number of columns in table");

    //
    // create the temp record
    //
    hTempRec = ::MsiCreateRecord(cColumns);
    ExitOnNull1(hTempRec, hr, E_UNEXPECTED, "could not create temp record for table: %S", wzTable);

    //
    // loop through all the columns filling in the data
    //
    va_start(args, cColumns);
    for (i = 1; i <= cColumns; i++)
    {
        hr = WcaGetRecordString(*phColumns, i, &pwzData);
        ExitOnFailure1(hr, "failed to get the data type for %d", i);

        // if data type is string write string
        if (L's' == *pwzData || L'S' == *pwzData || L'g' == *pwzData || L'G' == *pwzData || L'l' == *pwzData || L'L' == *pwzData)
        {
            LPCWSTR wz = va_arg(args, WCHAR*);

            // if this is the column that is supposed to be unique add the time stamp on the end
            if (uiUniquifyColumn == i)
            {
                hr = StrAllocFormatted(&pwzUniquify, L"%s%u", wz, ++dwUniquifyValue);   // up the count so we have no collisions on the unique name
                ExitOnFailure1(hr, "failed to allocate string for unique column: %d", uiUniquifyColumn);

                wz = pwzUniquify;
            }

            er = ::MsiRecordSetStringW(hTempRec, i, wz);
            hr = HRESULT_FROM_WIN32(er);
            ExitOnFailure1(hr, "failed to set string value at position %d", i);
        }
        // if data type is integer write integer
        else if (L'i' == *pwzData || L'I' == *pwzData || L'j' == *pwzData || L'J' == *pwzData)
        {
            AssertSz(uiUniquifyColumn != i, "Cannot uniquify an integer column");
            int iData = va_arg(args, int);

            er = ::MsiRecordSetInteger(hTempRec, i, iData);
            hr = HRESULT_FROM_WIN32(er);
            ExitOnFailure1(hr, "failed to set integer value at position %d", i);
        }
        else
        {
            // not supporting binary streams so error out
            hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
            ExitOnFailure2(hr, "unsupported data type '%S' in column: %d", pwzData, i);
        }
    }
    va_end(args);

    //
    // add the temporary record to the MSI
    //
    er = ::MsiViewModify(*phTableView, MSIMODIFY_INSERT_TEMPORARY, hTempRec);
    hr = HRESULT_FROM_WIN32(er);
    if (FAILED(hr))
    {
        MSIDBERROR dbErr;
        WCHAR wzBuf[MAX_PATH];
        DWORD cchBuf = countof(wzBuf);

        dbErr = ::MsiViewGetErrorW(*phTableView, wzBuf, &cchBuf);
        ExitOnFailure2(hr, "failed to add temporary row, dberr: %d, err: %S", dbErr, wzBuf);
    }

LExit:
    ReleaseStr(pwzUniquify);
    ReleaseStr(pwzData);
    ReleaseStr(pwzQuery);

    return hr;
}


/********************************************************************
WcaDumpTable - dumps a table to the log file

********************************************************************/
extern "C" HRESULT WIXAPI WcaDumpTable(
    __in LPCWSTR wzTable
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzQuery = NULL;
    PMSIHANDLE hView;
    PMSIHANDLE hColumns;
    DWORD cColumns = 0;
    PMSIHANDLE hRec;

    LPWSTR pwzData = NULL;
    LPWSTR pwzPrint = NULL;

    hr = StrAllocFormatted(&pwzQuery, L"SELECT * FROM `%s`",wzTable);
    ExitOnFailure(hr, "failed to allocate string for query");

    // Open and Execute the temp View
    hr = WcaOpenExecuteView(pwzQuery, &hView);
    ExitOnFailure1(hr, "failed to openexecute temp view with query %S", pwzQuery);

    // Use GetColumnInfo to populate the names of the columns.
    er = ::MsiViewGetColumnInfo(hView, MSICOLINFO_NAMES, &hColumns);
    hr = HRESULT_FROM_WIN32(er);
    ExitOnFailure1(hr, "failed to columns for table: %S", wzTable);

    cColumns = ::MsiRecordGetFieldCount(hColumns);

    WcaLog(LOGMSG_STANDARD, "--- Begin Table Dump %S ---", wzTable);

    // Loop through all the columns filling in the data.
    for (DWORD i = 1; i <= cColumns; i++)
    {
        hr = WcaGetRecordString(hColumns, i, &pwzData);
        ExitOnFailure1(hr, "failed to get the column name for %d", i);

        hr = StrAllocConcat(&pwzPrint, pwzData, 0);
        ExitOnFailure(hr, "Failed to add column name.");

        hr = StrAllocConcat(&pwzPrint, L"\t", 1);
        ExitOnFailure(hr, "Failed to add column name.");
    }

    WcaLog(LOGMSG_STANDARD, "%S", pwzPrint);

    // Now dump the actual rows.
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        if (pwzPrint && *pwzPrint)
        {
            *pwzPrint = L'\0';
        }

        for (DWORD i = 1; i <= cColumns; i++)
        {
            hr = WcaGetRecordString(hRec, i, &pwzData);
            ExitOnFailure1(hr, "failed to get the column name for %d", i);

            hr = StrAllocConcat(&pwzPrint, pwzData, 0);
            ExitOnFailure(hr, "Failed to add column name.");

            hr = StrAllocConcat(&pwzPrint, L"\t", 1);
            ExitOnFailure(hr, "Failed to add column name.");
        }

        WcaLog(LOGMSG_STANDARD, "%S", pwzPrint);
    }

    WcaLog(LOGMSG_STANDARD, "--- End Table Dump %S ---", wzTable);

LExit:
    ReleaseStr(pwzPrint);
    ReleaseStr(pwzData);
    ReleaseStr(pwzQuery);

    return hr;
}
