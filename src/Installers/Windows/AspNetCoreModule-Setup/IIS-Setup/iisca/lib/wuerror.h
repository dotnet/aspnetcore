// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

/***************************************************************************
*                                                                          *
*   wuerror.mc --  error code definitions for Windows Update.              *
*                                                                          *
***************************************************************************/
#ifndef _WUERROR_
#define _WUERROR_

#if defined (_MSC_VER) && (_MSC_VER >= 1020) && !defined(__midl)
#pragma once
#endif

#ifdef RC_INVOKED
#define _HRESULT_TYPEDEF_(_sc) _sc
#else // RC_INVOKED
#define _HRESULT_TYPEDEF_(_sc) ((HRESULT)_sc)
#endif // RC_INVOKED


///////////////////////////////////////////////////////////////////////////////
// Windows Update Success Codes
///////////////////////////////////////////////////////////////////////////////
//
//  Values are 32 bit values laid out as follows:
//
//   3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
//   1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
//  +---+-+-+-----------------------+-------------------------------+
//  |Sev|C|R|     Facility          |               Code            |
//  +---+-+-+-----------------------+-------------------------------+
//
//  where
//
//      Sev - is the severity code
//
//          00 - Success
//          01 - Informational
//          10 - Warning
//          11 - Error
//
//      C - is the Customer code flag
//
//      R - is a reserved bit
//
//      Facility - is the facility code
//
//      Code - is the facility's status code
//
//
// Define the facility codes
//


//
// Define the severity codes
//


//
// MessageId: WU_S_SERVICE_STOP
//
// MessageText:
//
// Windows Update Agent was stopped successfully.
//
#define WU_S_SERVICE_STOP                _HRESULT_TYPEDEF_(0x00240001L)

//
// MessageId: WU_S_SELFUPDATE
//
// MessageText:
//
// Windows Update Agent updated itself.
//
#define WU_S_SELFUPDATE                  _HRESULT_TYPEDEF_(0x00240002L)

//
// MessageId: WU_S_UPDATE_ERROR
//
// MessageText:
//
// Operation completed successfully but there were errors applying the updates.
//
#define WU_S_UPDATE_ERROR                _HRESULT_TYPEDEF_(0x00240003L)

//
// MessageId: WU_S_MARKED_FOR_DISCONNECT
//
// MessageText:
//
// A callback was marked to be disconnected later because the request to disconnect the operation came while a callback was executing.
//
#define WU_S_MARKED_FOR_DISCONNECT       _HRESULT_TYPEDEF_(0x00240004L)

//
// MessageId: WU_S_REBOOT_REQUIRED
//
// MessageText:
//
// The system must be restarted to complete installation of the update.
//
#define WU_S_REBOOT_REQUIRED             _HRESULT_TYPEDEF_(0x00240005L)

//
// MessageId: WU_S_ALREADY_INSTALLED
//
// MessageText:
//
// The update to be installed is already installed on the system.
//
#define WU_S_ALREADY_INSTALLED           _HRESULT_TYPEDEF_(0x00240006L)

//
// MessageId: WU_S_ALREADY_UNINSTALLED
//
// MessageText:
//
// The update to be removed is not installed on the system.
//
#define WU_S_ALREADY_UNINSTALLED         _HRESULT_TYPEDEF_(0x00240007L)

//
// MessageId: WU_S_ALREADY_DOWNLOADED
//
// MessageText:
//
// The update to be downloaded has already been downloaded.
//
#define WU_S_ALREADY_DOWNLOADED          _HRESULT_TYPEDEF_(0x00240008L)

///////////////////////////////////////////////////////////////////////////////
// Windows Update Error Codes
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_NO_SERVICE
//
// MessageText:
//
// Windows Update Agent was unable to provide the service.
//
#define WU_E_NO_SERVICE                  _HRESULT_TYPEDEF_(0x80240001L)

//
// MessageId: WU_E_MAX_CAPACITY_REACHED
//
// MessageText:
//
// The maximum capacity of the service was exceeded.
//
#define WU_E_MAX_CAPACITY_REACHED        _HRESULT_TYPEDEF_(0x80240002L)

//
// MessageId: WU_E_UNKNOWN_ID
//
// MessageText:
//
// An ID cannot be found.
//
#define WU_E_UNKNOWN_ID                  _HRESULT_TYPEDEF_(0x80240003L)

//
// MessageId: WU_E_NOT_INITIALIZED
//
// MessageText:
//
// The object could not be initialized.
//
#define WU_E_NOT_INITIALIZED             _HRESULT_TYPEDEF_(0x80240004L)

//
// MessageId: WU_E_RANGEOVERLAP
//
// MessageText:
//
// The update handler requested a byte range overlapping a previously requested range.
//
#define WU_E_RANGEOVERLAP                _HRESULT_TYPEDEF_(0x80240005L)

//
// MessageId: WU_E_TOOMANYRANGES
//
// MessageText:
//
// The requested number of byte ranges exceeds the maximum number (2^31 - 1).
//
#define WU_E_TOOMANYRANGES               _HRESULT_TYPEDEF_(0x80240006L)

//
// MessageId: WU_E_INVALIDINDEX
//
// MessageText:
//
// The index to a collection was invalid.
//
#define WU_E_INVALIDINDEX                _HRESULT_TYPEDEF_(0x80240007L)

//
// MessageId: WU_E_ITEMNOTFOUND
//
// MessageText:
//
// The key for the item queried could not be found.
//
#define WU_E_ITEMNOTFOUND                _HRESULT_TYPEDEF_(0x80240008L)

//
// MessageId: WU_E_OPERATIONINPROGRESS
//
// MessageText:
//
// Another conflicting operation was in progress. Some operations such as installation cannot be performed twice simultaneously.
//
#define WU_E_OPERATIONINPROGRESS         _HRESULT_TYPEDEF_(0x80240009L)

//
// MessageId: WU_E_COULDNOTCANCEL
//
// MessageText:
//
// Cancellation of the operation was not allowed.
//
#define WU_E_COULDNOTCANCEL              _HRESULT_TYPEDEF_(0x8024000AL)

//
// MessageId: WU_E_CALL_CANCELLED
//
// MessageText:
//
// Operation was cancelled.
//
#define WU_E_CALL_CANCELLED              _HRESULT_TYPEDEF_(0x8024000BL)

//
// MessageId: WU_E_NOOP
//
// MessageText:
//
// No operation was required.
//
#define WU_E_NOOP                        _HRESULT_TYPEDEF_(0x8024000CL)

//
// MessageId: WU_E_XML_MISSINGDATA
//
// MessageText:
//
// Windows Update Agent could not find required information in the update's XML data.
//
#define WU_E_XML_MISSINGDATA             _HRESULT_TYPEDEF_(0x8024000DL)

//
// MessageId: WU_E_XML_INVALID
//
// MessageText:
//
// Windows Update Agent found invalid information in the update's XML data.
//
#define WU_E_XML_INVALID                 _HRESULT_TYPEDEF_(0x8024000EL)

//
// MessageId: WU_E_CYCLE_DETECTED
//
// MessageText:
//
// Circular update relationships were detected in the metadata.
//
#define WU_E_CYCLE_DETECTED              _HRESULT_TYPEDEF_(0x8024000FL)

//
// MessageId: WU_E_TOO_DEEP_RELATION
//
// MessageText:
//
// Update relationships too deep to evaluate were evaluated.
//
#define WU_E_TOO_DEEP_RELATION           _HRESULT_TYPEDEF_(0x80240010L)

//
// MessageId: WU_E_INVALID_RELATIONSHIP
//
// MessageText:
//
// An invalid update relationship was detected.
//
#define WU_E_INVALID_RELATIONSHIP        _HRESULT_TYPEDEF_(0x80240011L)

//
// MessageId: WU_E_REG_VALUE_INVALID
//
// MessageText:
//
// An invalid registry value was read.
//
#define WU_E_REG_VALUE_INVALID           _HRESULT_TYPEDEF_(0x80240012L)

//
// MessageId: WU_E_DUPLICATE_ITEM
//
// MessageText:
//
// Operation tried to add a duplicate item to a list.
//
#define WU_E_DUPLICATE_ITEM              _HRESULT_TYPEDEF_(0x80240013L)

//
// MessageId: WU_E_INSTALL_NOT_ALLOWED
//
// MessageText:
//
// Operation tried to install while another installation was in progress or the system was pending a mandatory restart.
//
#define WU_E_INSTALL_NOT_ALLOWED         _HRESULT_TYPEDEF_(0x80240016L)

//
// MessageId: WU_E_NOT_APPLICABLE
//
// MessageText:
//
// Operation was not performed because there are no applicable updates.
//
#define WU_E_NOT_APPLICABLE              _HRESULT_TYPEDEF_(0x80240017L)

//
// MessageId: WU_E_NO_USERTOKEN
//
// MessageText:
//
// Operation failed because a required user token is missing.
//
#define WU_E_NO_USERTOKEN                _HRESULT_TYPEDEF_(0x80240018L)

//
// MessageId: WU_E_EXCLUSIVE_INSTALL_CONFLICT
//
// MessageText:
//
// An exclusive update cannot be installed with other updates at the same time.
//
#define WU_E_EXCLUSIVE_INSTALL_CONFLICT  _HRESULT_TYPEDEF_(0x80240019L)

//
// MessageId: WU_E_POLICY_NOT_SET
//
// MessageText:
//
// A policy value was not set.
//
#define WU_E_POLICY_NOT_SET              _HRESULT_TYPEDEF_(0x8024001AL)

//
// MessageId: WU_E_SELFUPDATE_IN_PROGRESS
//
// MessageText:
//
// The operation could not be performed because the Windows Update Agent is self-updating.
//
#define WU_E_SELFUPDATE_IN_PROGRESS      _HRESULT_TYPEDEF_(0x8024001BL)

//
// MessageId: WU_E_INVALID_UPDATE
//
// MessageText:
//
// An update contains invalid metadata.
//
#define WU_E_INVALID_UPDATE              _HRESULT_TYPEDEF_(0x8024001DL)

//
// MessageId: WU_E_SERVICE_STOP
//
// MessageText:
//
// Operation did not complete because the service or system was being shut down.
//
#define WU_E_SERVICE_STOP                _HRESULT_TYPEDEF_(0x8024001EL)

//
// MessageId: WU_E_NO_CONNECTION
//
// MessageText:
//
// Operation did not complete because the network connection was unavailable.
//
#define WU_E_NO_CONNECTION               _HRESULT_TYPEDEF_(0x8024001FL)

//
// MessageId: WU_E_NO_INTERACTIVE_USER
//
// MessageText:
//
// Operation did not complete because there is no logged-on interactive user.
//
#define WU_E_NO_INTERACTIVE_USER         _HRESULT_TYPEDEF_(0x80240020L)

//
// MessageId: WU_E_TIME_OUT
//
// MessageText:
//
// Operation did not complete because it timed out.
//
#define WU_E_TIME_OUT                    _HRESULT_TYPEDEF_(0x80240021L)

//
// MessageId: WU_E_ALL_UPDATES_FAILED
//
// MessageText:
//
// Operation failed for all the updates.
//
#define WU_E_ALL_UPDATES_FAILED          _HRESULT_TYPEDEF_(0x80240022L)

//
// MessageId: WU_E_EULAS_DECLINED
//
// MessageText:
//
// The license terms for all updates were declined.
//
#define WU_E_EULAS_DECLINED              _HRESULT_TYPEDEF_(0x80240023L)

//
// MessageId: WU_E_NO_UPDATE
//
// MessageText:
//
// There are no updates.
//
#define WU_E_NO_UPDATE                   _HRESULT_TYPEDEF_(0x80240024L)

//
// MessageId: WU_E_USER_ACCESS_DISABLED
//
// MessageText:
//
// Group Policy settings prevented access to Windows Update.
//
#define WU_E_USER_ACCESS_DISABLED        _HRESULT_TYPEDEF_(0x80240025L)

//
// MessageId: WU_E_INVALID_UPDATE_TYPE
//
// MessageText:
//
// The type of update is invalid.
//
#define WU_E_INVALID_UPDATE_TYPE         _HRESULT_TYPEDEF_(0x80240026L)

//
// MessageId: WU_E_URL_TOO_LONG
//
// MessageText:
//
// The URL exceeded the maximum length.
//
#define WU_E_URL_TOO_LONG                _HRESULT_TYPEDEF_(0x80240027L)

//
// MessageId: WU_E_UNINSTALL_NOT_ALLOWED
//
// MessageText:
//
// The update could not be uninstalled because the request did not originate from a WSUS server.
//
#define WU_E_UNINSTALL_NOT_ALLOWED       _HRESULT_TYPEDEF_(0x80240028L)

//
// MessageId: WU_E_INVALID_PRODUCT_LICENSE
//
// MessageText:
//
// Search may have missed some updates before there is an unlicensed application on the system.
//
#define WU_E_INVALID_PRODUCT_LICENSE     _HRESULT_TYPEDEF_(0x80240029L)

//
// MessageId: WU_E_MISSING_HANDLER
//
// MessageText:
//
// A component required to detect applicable updates was missing.
//
#define WU_E_MISSING_HANDLER             _HRESULT_TYPEDEF_(0x8024002AL)

//
// MessageId: WU_E_LEGACYSERVER
//
// MessageText:
//
// An operation did not complete because it requires a newer version of server.
//
#define WU_E_LEGACYSERVER                _HRESULT_TYPEDEF_(0x8024002BL)

//
// MessageId: WU_E_BIN_SOURCE_ABSENT
//
// MessageText:
//
// A delta-compressed update could not be installed because it required the source.
//
#define WU_E_BIN_SOURCE_ABSENT           _HRESULT_TYPEDEF_(0x8024002CL)

//
// MessageId: WU_E_SOURCE_ABSENT
//
// MessageText:
//
// A full-file update could not be installed because it required the source.
//
#define WU_E_SOURCE_ABSENT               _HRESULT_TYPEDEF_(0x8024002DL)

//
// MessageId: WU_E_WU_DISABLED
//
// MessageText:
//
// Access to an unmanaged server is not allowed.
//
#define WU_E_WU_DISABLED                 _HRESULT_TYPEDEF_(0x8024002EL)

//
// MessageId: WU_E_CALL_CANCELLED_BY_POLICY
//
// MessageText:
//
// Operation did not complete because the DisableWindowsUpdateAccess policy was set.
//
#define WU_E_CALL_CANCELLED_BY_POLICY    _HRESULT_TYPEDEF_(0x8024002FL)

//
// MessageId: WU_E_INVALID_PROXY_SERVER
//
// MessageText:
//
// The format of the proxy list was invalid.
//
#define WU_E_INVALID_PROXY_SERVER        _HRESULT_TYPEDEF_(0x80240030L)

//
// MessageId: WU_E_INVALID_FILE
//
// MessageText:
//
// The file is in the wrong format.
//
#define WU_E_INVALID_FILE                _HRESULT_TYPEDEF_(0x80240031L)

//
// MessageId: WU_E_INVALID_CRITERIA
//
// MessageText:
//
// The search criteria string was invalid.
//
#define WU_E_INVALID_CRITERIA            _HRESULT_TYPEDEF_(0x80240032L)

//
// MessageId: WU_E_EULA_UNAVAILABLE
//
// MessageText:
//
// License terms could not be downloaded.
//
#define WU_E_EULA_UNAVAILABLE            _HRESULT_TYPEDEF_(0x80240033L)

//
// MessageId: WU_E_DOWNLOAD_FAILED
//
// MessageText:
//
// Update failed to download.
//
#define WU_E_DOWNLOAD_FAILED             _HRESULT_TYPEDEF_(0x80240034L)

//
// MessageId: WU_E_UPDATE_NOT_PROCESSED
//
// MessageText:
//
// The update was not processed.
//
#define WU_E_UPDATE_NOT_PROCESSED        _HRESULT_TYPEDEF_(0x80240035L)

//
// MessageId: WU_E_INVALID_OPERATION
//
// MessageText:
//
// The object's current state did not allow the operation.
//
#define WU_E_INVALID_OPERATION           _HRESULT_TYPEDEF_(0x80240036L)

//
// MessageId: WU_E_NOT_SUPPORTED
//
// MessageText:
//
// The functionality for the operation is not supported.
//
#define WU_E_NOT_SUPPORTED               _HRESULT_TYPEDEF_(0x80240037L)

//
// MessageId: WU_E_WINHTTP_INVALID_FILE
//
// MessageText:
//
// The downloaded file has an unexpected content type.
//
#define WU_E_WINHTTP_INVALID_FILE        _HRESULT_TYPEDEF_(0x80240038L)

//
// MessageId: WU_E_TOO_MANY_RESYNC
//
// MessageText:
//
// Agent is asked by server to resync too many times.
//
#define WU_E_TOO_MANY_RESYNC             _HRESULT_TYPEDEF_(0x80240039L)

//
// MessageId: WU_E_NO_SERVER_CORE_SUPPORT
//
// MessageText:
//
// WUA API method does not run on Server Core installation.
//
#define WU_E_NO_SERVER_CORE_SUPPORT      _HRESULT_TYPEDEF_(0x80240040L)

//
// MessageId: WU_E_SYSPREP_IN_PROGRESS
//
// MessageText:
//
// Service is not available while sysprep is running.
//
#define WU_E_SYSPREP_IN_PROGRESS         _HRESULT_TYPEDEF_(0x80240041L)

//
// MessageId: WU_E_UNKNOWN_SERVICE
//
// MessageText:
//
// The update service is no longer registered with AU.
//
#define WU_E_UNKNOWN_SERVICE             _HRESULT_TYPEDEF_(0x80240042L)

//
// MessageId: WU_E_NO_UI_SUPPORT
//
// MessageText:
//
// There is no support for WUA UI.
//
#define WU_E_NO_UI_SUPPORT               _HRESULT_TYPEDEF_(0x80240043L)

//
// MessageId: WU_E_UNEXPECTED
//
// MessageText:
//
// An operation failed due to reasons not covered by another error code.
//
#define WU_E_UNEXPECTED                  _HRESULT_TYPEDEF_(0x80240FFFL)

///////////////////////////////////////////////////////////////////////////////
// Windows Installer minor errors
//
// The following errors are used to indicate that part of a search failed for
// MSI problems. Another part of the search may successfully return updates.
// All MSI minor codes should share the same error code range so that the caller
// tell that they are related to Windows Installer.
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_MSI_WRONG_VERSION
//
// MessageText:
//
// Search may have missed some updates because the Windows Installer is less than version 3.1.
//
#define WU_E_MSI_WRONG_VERSION           _HRESULT_TYPEDEF_(0x80241001L)

//
// MessageId: WU_E_MSI_NOT_CONFIGURED
//
// MessageText:
//
// Search may have missed some updates because the Windows Installer is not configured.
//
#define WU_E_MSI_NOT_CONFIGURED          _HRESULT_TYPEDEF_(0x80241002L)

//
// MessageId: WU_E_MSP_DISABLED
//
// MessageText:
//
// Search may have missed some updates because policy has disabled Windows Installer patching.
//
#define WU_E_MSP_DISABLED                _HRESULT_TYPEDEF_(0x80241003L)

//
// MessageId: WU_E_MSI_WRONG_APP_CONTEXT
//
// MessageText:
//
// An update could not be applied because the application is installed per-user.
//
#define WU_E_MSI_WRONG_APP_CONTEXT       _HRESULT_TYPEDEF_(0x80241004L)

//
// MessageId: WU_E_MSP_UNEXPECTED
//
// MessageText:
//
// Search may have missed some updates because there was a failure of the Windows Installer.
//
#define WU_E_MSP_UNEXPECTED              _HRESULT_TYPEDEF_(0x80241FFFL)

///////////////////////////////////////////////////////////////////////////////
// Protocol Talker errors
//
// The following map to SOAPCLIENT_ERRORs from atlsoap.h. These errors
// are obtained from calling GetClientError() on the CClientWebService
// object.
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_PT_SOAPCLIENT_BASE
//
// MessageText:
//
// WU_E_PT_SOAPCLIENT_* error codes map to the SOAPCLIENT_ERROR enum of the ATL Server Library.
//
#define WU_E_PT_SOAPCLIENT_BASE          _HRESULT_TYPEDEF_(0x80244000L)

//
// MessageId: WU_E_PT_SOAPCLIENT_INITIALIZE
//
// MessageText:
//
// Same as SOAPCLIENT_INITIALIZE_ERROR - initialization of the SOAP client failed, possibly because of an MSXML installation failure.
//
#define WU_E_PT_SOAPCLIENT_INITIALIZE    _HRESULT_TYPEDEF_(0x80244001L)

//
// MessageId: WU_E_PT_SOAPCLIENT_OUTOFMEMORY
//
// MessageText:
//
// Same as SOAPCLIENT_OUTOFMEMORY - SOAP client failed because it ran out of memory.
//
#define WU_E_PT_SOAPCLIENT_OUTOFMEMORY   _HRESULT_TYPEDEF_(0x80244002L)

//
// MessageId: WU_E_PT_SOAPCLIENT_GENERATE
//
// MessageText:
//
// Same as SOAPCLIENT_GENERATE_ERROR - SOAP client failed to generate the request.
//
#define WU_E_PT_SOAPCLIENT_GENERATE      _HRESULT_TYPEDEF_(0x80244003L)

//
// MessageId: WU_E_PT_SOAPCLIENT_CONNECT
//
// MessageText:
//
// Same as SOAPCLIENT_CONNECT_ERROR - SOAP client failed to connect to the server.
//
#define WU_E_PT_SOAPCLIENT_CONNECT       _HRESULT_TYPEDEF_(0x80244004L)

//
// MessageId: WU_E_PT_SOAPCLIENT_SEND
//
// MessageText:
//
// Same as SOAPCLIENT_SEND_ERROR - SOAP client failed to send a message for reasons of WU_E_WINHTTP_* error codes.
//
#define WU_E_PT_SOAPCLIENT_SEND          _HRESULT_TYPEDEF_(0x80244005L)

//
// MessageId: WU_E_PT_SOAPCLIENT_SERVER
//
// MessageText:
//
// Same as SOAPCLIENT_SERVER_ERROR - SOAP client failed because there was a server error.
//
#define WU_E_PT_SOAPCLIENT_SERVER        _HRESULT_TYPEDEF_(0x80244006L)

//
// MessageId: WU_E_PT_SOAPCLIENT_SOAPFAULT
//
// MessageText:
//
// Same as SOAPCLIENT_SOAPFAULT - SOAP client failed because there was a SOAP fault for reasons of WU_E_PT_SOAP_* error codes.
//
#define WU_E_PT_SOAPCLIENT_SOAPFAULT     _HRESULT_TYPEDEF_(0x80244007L)

//
// MessageId: WU_E_PT_SOAPCLIENT_PARSEFAULT
//
// MessageText:
//
// Same as SOAPCLIENT_PARSEFAULT_ERROR - SOAP client failed to parse a SOAP fault.
//
#define WU_E_PT_SOAPCLIENT_PARSEFAULT    _HRESULT_TYPEDEF_(0x80244008L)

//
// MessageId: WU_E_PT_SOAPCLIENT_READ
//
// MessageText:
//
// Same as SOAPCLIENT_READ_ERROR - SOAP client failed while reading the response from the server.
//
#define WU_E_PT_SOAPCLIENT_READ          _HRESULT_TYPEDEF_(0x80244009L)

//
// MessageId: WU_E_PT_SOAPCLIENT_PARSE
//
// MessageText:
//
// Same as SOAPCLIENT_PARSE_ERROR - SOAP client failed to parse the response from the server.
//
#define WU_E_PT_SOAPCLIENT_PARSE         _HRESULT_TYPEDEF_(0x8024400AL)

// The following map to SOAP_ERROR_CODEs from atlsoap.h. These errors
// are obtained from the m_fault.m_soapErrCode member on the
// CClientWebService object when GetClientError() returned
// SOAPCLIENT_SOAPFAULT.
//
// MessageId: WU_E_PT_SOAP_VERSION
//
// MessageText:
//
// Same as SOAP_E_VERSION_MISMATCH - SOAP client found an unrecognizable namespace for the SOAP envelope.
//
#define WU_E_PT_SOAP_VERSION             _HRESULT_TYPEDEF_(0x8024400BL)

//
// MessageId: WU_E_PT_SOAP_MUST_UNDERSTAND
//
// MessageText:
//
// Same as SOAP_E_MUST_UNDERSTAND - SOAP client was unable to understand a header.
//
#define WU_E_PT_SOAP_MUST_UNDERSTAND     _HRESULT_TYPEDEF_(0x8024400CL)

//
// MessageId: WU_E_PT_SOAP_CLIENT
//
// MessageText:
//
// Same as SOAP_E_CLIENT - SOAP client found the message was malformed; fix before resending.
//
#define WU_E_PT_SOAP_CLIENT              _HRESULT_TYPEDEF_(0x8024400DL)

//
// MessageId: WU_E_PT_SOAP_SERVER
//
// MessageText:
//
// Same as SOAP_E_SERVER - The SOAP message could not be processed due to a server error; resend later.
//
#define WU_E_PT_SOAP_SERVER              _HRESULT_TYPEDEF_(0x8024400EL)

//
// MessageId: WU_E_PT_WMI_ERROR
//
// MessageText:
//
// There was an unspecified Windows Management Instrumentation (WMI) error.
//
#define WU_E_PT_WMI_ERROR                _HRESULT_TYPEDEF_(0x8024400FL)

//
// MessageId: WU_E_PT_EXCEEDED_MAX_SERVER_TRIPS
//
// MessageText:
//
// The number of round trips to the server exceeded the maximum limit.
//
#define WU_E_PT_EXCEEDED_MAX_SERVER_TRIPS _HRESULT_TYPEDEF_(0x80244010L)

//
// MessageId: WU_E_PT_SUS_SERVER_NOT_SET
//
// MessageText:
//
// WUServer policy value is missing in the registry.
//
#define WU_E_PT_SUS_SERVER_NOT_SET       _HRESULT_TYPEDEF_(0x80244011L)

//
// MessageId: WU_E_PT_DOUBLE_INITIALIZATION
//
// MessageText:
//
// Initialization failed because the object was already initialized.
//
#define WU_E_PT_DOUBLE_INITIALIZATION    _HRESULT_TYPEDEF_(0x80244012L)

//
// MessageId: WU_E_PT_INVALID_COMPUTER_NAME
//
// MessageText:
//
// The computer name could not be determined.
//
#define WU_E_PT_INVALID_COMPUTER_NAME    _HRESULT_TYPEDEF_(0x80244013L)

//
// MessageId: WU_E_PT_REFRESH_CACHE_REQUIRED
//
// MessageText:
//
// The reply from the server indicates that the server was changed or the cookie was invalid; refresh the state of the internal cache and retry.
//
#define WU_E_PT_REFRESH_CACHE_REQUIRED   _HRESULT_TYPEDEF_(0x80244015L)

//
// MessageId: WU_E_PT_HTTP_STATUS_BAD_REQUEST
//
// MessageText:
//
// Same as HTTP status 400 - the server could not process the request due to invalid syntax.
//
#define WU_E_PT_HTTP_STATUS_BAD_REQUEST  _HRESULT_TYPEDEF_(0x80244016L)

//
// MessageId: WU_E_PT_HTTP_STATUS_DENIED
//
// MessageText:
//
// Same as HTTP status 401 - the requested resource requires user authentication.
//
#define WU_E_PT_HTTP_STATUS_DENIED       _HRESULT_TYPEDEF_(0x80244017L)

//
// MessageId: WU_E_PT_HTTP_STATUS_FORBIDDEN
//
// MessageText:
//
// Same as HTTP status 403 - server understood the request, but declined to fulfill it.
//
#define WU_E_PT_HTTP_STATUS_FORBIDDEN    _HRESULT_TYPEDEF_(0x80244018L)

//
// MessageId: WU_E_PT_HTTP_STATUS_NOT_FOUND
//
// MessageText:
//
// Same as HTTP status 404 - the server cannot find the requested URI (Uniform Resource Identifier).
// 
//
#define WU_E_PT_HTTP_STATUS_NOT_FOUND    _HRESULT_TYPEDEF_(0x80244019L)

//
// MessageId: WU_E_PT_HTTP_STATUS_BAD_METHOD
//
// MessageText:
//
// Same as HTTP status 405 - the HTTP method is not allowed.
//
#define WU_E_PT_HTTP_STATUS_BAD_METHOD   _HRESULT_TYPEDEF_(0x8024401AL)

//
// MessageId: WU_E_PT_HTTP_STATUS_PROXY_AUTH_REQ
//
// MessageText:
//
// Same as HTTP status 407 - proxy authentication is required.
//
#define WU_E_PT_HTTP_STATUS_PROXY_AUTH_REQ _HRESULT_TYPEDEF_(0x8024401BL)

//
// MessageId: WU_E_PT_HTTP_STATUS_REQUEST_TIMEOUT
//
// MessageText:
//
// Same as HTTP status 408 - the server timed out waiting for the request.
//
#define WU_E_PT_HTTP_STATUS_REQUEST_TIMEOUT _HRESULT_TYPEDEF_(0x8024401CL)

//
// MessageId: WU_E_PT_HTTP_STATUS_CONFLICT
//
// MessageText:
//
// Same as HTTP status 409 - the request was not completed due to a conflict with the current state of the resource.
//
#define WU_E_PT_HTTP_STATUS_CONFLICT     _HRESULT_TYPEDEF_(0x8024401DL)

//
// MessageId: WU_E_PT_HTTP_STATUS_GONE
//
// MessageText:
//
// Same as HTTP status 410 - requested resource is no longer available at the server.
//
#define WU_E_PT_HTTP_STATUS_GONE         _HRESULT_TYPEDEF_(0x8024401EL)

//
// MessageId: WU_E_PT_HTTP_STATUS_SERVER_ERROR
//
// MessageText:
//
// Same as HTTP status 500 - an error internal to the server prevented fulfilling the request.
//
#define WU_E_PT_HTTP_STATUS_SERVER_ERROR _HRESULT_TYPEDEF_(0x8024401FL)

//
// MessageId: WU_E_PT_HTTP_STATUS_NOT_SUPPORTED
//
// MessageText:
//
// Same as HTTP status 500 - server does not support the functionality required to fulfill the request.
//
#define WU_E_PT_HTTP_STATUS_NOT_SUPPORTED _HRESULT_TYPEDEF_(0x80244020L)

//
// MessageId: WU_E_PT_HTTP_STATUS_BAD_GATEWAY
//
// MessageText:
//
// Same as HTTP status 502 - the server, while acting as a gateway or proxy, received an invalid response from the upstream server it accessed in attempting to fulfill the request.
//
#define WU_E_PT_HTTP_STATUS_BAD_GATEWAY  _HRESULT_TYPEDEF_(0x80244021L)

//
// MessageId: WU_E_PT_HTTP_STATUS_SERVICE_UNAVAIL
//
// MessageText:
//
// Same as HTTP status 503 - the service is temporarily overloaded.
//
#define WU_E_PT_HTTP_STATUS_SERVICE_UNAVAIL _HRESULT_TYPEDEF_(0x80244022L)

//
// MessageId: WU_E_PT_HTTP_STATUS_GATEWAY_TIMEOUT
//
// MessageText:
//
// Same as HTTP status 503 - the request was timed out waiting for a gateway.
//
#define WU_E_PT_HTTP_STATUS_GATEWAY_TIMEOUT _HRESULT_TYPEDEF_(0x80244023L)

//
// MessageId: WU_E_PT_HTTP_STATUS_VERSION_NOT_SUP
//
// MessageText:
//
// Same as HTTP status 505 - the server does not support the HTTP protocol version used for the request.
//
#define WU_E_PT_HTTP_STATUS_VERSION_NOT_SUP _HRESULT_TYPEDEF_(0x80244024L)

//
// MessageId: WU_E_PT_FILE_LOCATIONS_CHANGED
//
// MessageText:
//
// Operation failed due to a changed file location; refresh internal state and resend.
//
#define WU_E_PT_FILE_LOCATIONS_CHANGED   _HRESULT_TYPEDEF_(0x80244025L)

//
// MessageId: WU_E_PT_REGISTRATION_NOT_SUPPORTED
//
// MessageText:
//
// Operation failed because Windows Update Agent does not support registration with a non-WSUS server.
//
#define WU_E_PT_REGISTRATION_NOT_SUPPORTED _HRESULT_TYPEDEF_(0x80244026L)

//
// MessageId: WU_E_PT_NO_AUTH_PLUGINS_REQUESTED
//
// MessageText:
//
// The server returned an empty authentication information list.
//
#define WU_E_PT_NO_AUTH_PLUGINS_REQUESTED _HRESULT_TYPEDEF_(0x80244027L)

//
// MessageId: WU_E_PT_NO_AUTH_COOKIES_CREATED
//
// MessageText:
//
// Windows Update Agent was unable to create any valid authentication cookies.
//
#define WU_E_PT_NO_AUTH_COOKIES_CREATED  _HRESULT_TYPEDEF_(0x80244028L)

//
// MessageId: WU_E_PT_INVALID_CONFIG_PROP
//
// MessageText:
//
// A configuration property value was wrong.
//
#define WU_E_PT_INVALID_CONFIG_PROP      _HRESULT_TYPEDEF_(0x80244029L)

//
// MessageId: WU_E_PT_CONFIG_PROP_MISSING
//
// MessageText:
//
// A configuration property value was missing.
//
#define WU_E_PT_CONFIG_PROP_MISSING      _HRESULT_TYPEDEF_(0x8024402AL)

//
// MessageId: WU_E_PT_HTTP_STATUS_NOT_MAPPED
//
// MessageText:
//
// The HTTP request could not be completed and the reason did not correspond to any of the WU_E_PT_HTTP_* error codes.
//
#define WU_E_PT_HTTP_STATUS_NOT_MAPPED   _HRESULT_TYPEDEF_(0x8024402BL)

//
// MessageId: WU_E_PT_WINHTTP_NAME_NOT_RESOLVED
//
// MessageText:
//
// Same as ERROR_WINHTTP_NAME_NOT_RESOLVED - the proxy server or target server name cannot be resolved.
//
#define WU_E_PT_WINHTTP_NAME_NOT_RESOLVED _HRESULT_TYPEDEF_(0x8024402CL)

//
// MessageId: WU_E_PT_SAME_REDIR_ID
//
// MessageText:
//
// Windows Update Agent failed to download a redirector cabinet file with a new redirectorId value from the server during the recovery.
//
#define WU_E_PT_SAME_REDIR_ID            _HRESULT_TYPEDEF_(0x8024502DL)

//
// MessageId: WU_E_PT_NO_MANAGED_RECOVER
//
// MessageText:
//
// A redirector recovery action did not complete because the server is managed.
//
#define WU_E_PT_NO_MANAGED_RECOVER       _HRESULT_TYPEDEF_(0x8024502EL)

//
// MessageId: WU_E_PT_ECP_SUCCEEDED_WITH_ERRORS
//
// MessageText:
//
// External cab file processing completed with some errors.
//
#define WU_E_PT_ECP_SUCCEEDED_WITH_ERRORS _HRESULT_TYPEDEF_(0x8024402FL)

//
// MessageId: WU_E_PT_ECP_INIT_FAILED
//
// MessageText:
//
// The external cab processor initialization did not complete.
//
#define WU_E_PT_ECP_INIT_FAILED          _HRESULT_TYPEDEF_(0x80244030L)

//
// MessageId: WU_E_PT_ECP_INVALID_FILE_FORMAT
//
// MessageText:
//
// The format of a metadata file was invalid.
//
#define WU_E_PT_ECP_INVALID_FILE_FORMAT  _HRESULT_TYPEDEF_(0x80244031L)

//
// MessageId: WU_E_PT_ECP_INVALID_METADATA
//
// MessageText:
//
// External cab processor found invalid metadata.
//
#define WU_E_PT_ECP_INVALID_METADATA     _HRESULT_TYPEDEF_(0x80244032L)

//
// MessageId: WU_E_PT_ECP_FAILURE_TO_EXTRACT_DIGEST
//
// MessageText:
//
// The file digest could not be extracted from an external cab file.
//
#define WU_E_PT_ECP_FAILURE_TO_EXTRACT_DIGEST _HRESULT_TYPEDEF_(0x80244033L)

//
// MessageId: WU_E_PT_ECP_FAILURE_TO_DECOMPRESS_CAB_FILE
//
// MessageText:
//
// An external cab file could not be decompressed.
//
#define WU_E_PT_ECP_FAILURE_TO_DECOMPRESS_CAB_FILE _HRESULT_TYPEDEF_(0x80244034L)

//
// MessageId: WU_E_PT_ECP_FILE_LOCATION_ERROR
//
// MessageText:
//
// External cab processor was unable to get file locations.
//
#define WU_E_PT_ECP_FILE_LOCATION_ERROR  _HRESULT_TYPEDEF_(0x80244035L)

//
// MessageId: WU_E_PT_UNEXPECTED
//
// MessageText:
//
// A communication error not covered by another WU_E_PT_* error code.
//
#define WU_E_PT_UNEXPECTED               _HRESULT_TYPEDEF_(0x80244FFFL)

///////////////////////////////////////////////////////////////////////////////
// Redirector errors
//
// The following errors are generated by the components that download and
// parse the wuredir.cab
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_REDIRECTOR_LOAD_XML
//
// MessageText:
//
// The redirector XML document could not be loaded into the DOM class.
//
#define WU_E_REDIRECTOR_LOAD_XML         _HRESULT_TYPEDEF_(0x80245001L)

//
// MessageId: WU_E_REDIRECTOR_S_FALSE
//
// MessageText:
//
// The redirector XML document is missing some required information.
//
#define WU_E_REDIRECTOR_S_FALSE          _HRESULT_TYPEDEF_(0x80245002L)

//
// MessageId: WU_E_REDIRECTOR_ID_SMALLER
//
// MessageText:
//
// The redirectorId in the downloaded redirector cab is less than in the cached cab.
//
#define WU_E_REDIRECTOR_ID_SMALLER       _HRESULT_TYPEDEF_(0x80245003L)

//
// MessageId: WU_E_REDIRECTOR_UNEXPECTED
//
// MessageText:
//
// The redirector failed for reasons not covered by another WU_E_REDIRECTOR_* error code.
//
#define WU_E_REDIRECTOR_UNEXPECTED       _HRESULT_TYPEDEF_(0x80245FFFL)

///////////////////////////////////////////////////////////////////////////////
// driver util errors
//
// The device PnP enumerated device was pruned from the SystemSpec because
// one of the hardware or compatible IDs matched an installed printer driver.
// This is not considered a fatal error and the device is simply skipped.
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_DRV_PRUNED
//
// MessageText:
//
// A driver was skipped.
//
#define WU_E_DRV_PRUNED                  _HRESULT_TYPEDEF_(0x8024C001L)

//
// MessageId: WU_E_DRV_NOPROP_OR_LEGACY
//
// MessageText:
//
// A property for the driver could not be found. It may not conform with required specifications.
//
#define WU_E_DRV_NOPROP_OR_LEGACY        _HRESULT_TYPEDEF_(0x8024C002L)

//
// MessageId: WU_E_DRV_REG_MISMATCH
//
// MessageText:
//
// The registry type read for the driver does not match the expected type.
//
#define WU_E_DRV_REG_MISMATCH            _HRESULT_TYPEDEF_(0x8024C003L)

//
// MessageId: WU_E_DRV_NO_METADATA
//
// MessageText:
//
// The driver update is missing metadata.
//
#define WU_E_DRV_NO_METADATA             _HRESULT_TYPEDEF_(0x8024C004L)

//
// MessageId: WU_E_DRV_MISSING_ATTRIBUTE
//
// MessageText:
//
// The driver update is missing a required attribute.
//
#define WU_E_DRV_MISSING_ATTRIBUTE       _HRESULT_TYPEDEF_(0x8024C005L)

//
// MessageId: WU_E_DRV_SYNC_FAILED
//
// MessageText:
//
// Driver synchronization failed.
//
#define WU_E_DRV_SYNC_FAILED             _HRESULT_TYPEDEF_(0x8024C006L)

//
// MessageId: WU_E_DRV_NO_PRINTER_CONTENT
//
// MessageText:
//
// Information required for the synchronization of applicable printers is missing.
//
#define WU_E_DRV_NO_PRINTER_CONTENT      _HRESULT_TYPEDEF_(0x8024C007L)

//
// MessageId: WU_E_DRV_UNEXPECTED
//
// MessageText:
//
// A driver error not covered by another WU_E_DRV_* code.
//
#define WU_E_DRV_UNEXPECTED              _HRESULT_TYPEDEF_(0x8024CFFFL)

//////////////////////////////////////////////////////////////////////////////
// data store errors
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_DS_SHUTDOWN
//
// MessageText:
//
// An operation failed because Windows Update Agent is shutting down.
//
#define WU_E_DS_SHUTDOWN                 _HRESULT_TYPEDEF_(0x80248000L)

//
// MessageId: WU_E_DS_INUSE
//
// MessageText:
//
// An operation failed because the data store was in use.
//
#define WU_E_DS_INUSE                    _HRESULT_TYPEDEF_(0x80248001L)

//
// MessageId: WU_E_DS_INVALID
//
// MessageText:
//
// The current and expected states of the data store do not match.
//
#define WU_E_DS_INVALID                  _HRESULT_TYPEDEF_(0x80248002L)

//
// MessageId: WU_E_DS_TABLEMISSING
//
// MessageText:
//
// The data store is missing a table.
//
#define WU_E_DS_TABLEMISSING             _HRESULT_TYPEDEF_(0x80248003L)

//
// MessageId: WU_E_DS_TABLEINCORRECT
//
// MessageText:
//
// The data store contains a table with unexpected columns.
//
#define WU_E_DS_TABLEINCORRECT           _HRESULT_TYPEDEF_(0x80248004L)

//
// MessageId: WU_E_DS_INVALIDTABLENAME
//
// MessageText:
//
// A table could not be opened because the table is not in the data store.
//
#define WU_E_DS_INVALIDTABLENAME         _HRESULT_TYPEDEF_(0x80248005L)

//
// MessageId: WU_E_DS_BADVERSION
//
// MessageText:
//
// The current and expected versions of the data store do not match.
//
#define WU_E_DS_BADVERSION               _HRESULT_TYPEDEF_(0x80248006L)

//
// MessageId: WU_E_DS_NODATA
//
// MessageText:
//
// The information requested is not in the data store.
//
#define WU_E_DS_NODATA                   _HRESULT_TYPEDEF_(0x80248007L)

//
// MessageId: WU_E_DS_MISSINGDATA
//
// MessageText:
//
// The data store is missing required information or has a NULL in a table column that requires a non-null value.
//
#define WU_E_DS_MISSINGDATA              _HRESULT_TYPEDEF_(0x80248008L)

//
// MessageId: WU_E_DS_MISSINGREF
//
// MessageText:
//
// The data store is missing required information or has a reference to missing license terms, file, localized property or linked row.
//
#define WU_E_DS_MISSINGREF               _HRESULT_TYPEDEF_(0x80248009L)

//
// MessageId: WU_E_DS_UNKNOWNHANDLER
//
// MessageText:
//
// The update was not processed because its update handler could not be recognized.
//
#define WU_E_DS_UNKNOWNHANDLER           _HRESULT_TYPEDEF_(0x8024800AL)

//
// MessageId: WU_E_DS_CANTDELETE
//
// MessageText:
//
// The update was not deleted because it is still referenced by one or more services.
//
#define WU_E_DS_CANTDELETE               _HRESULT_TYPEDEF_(0x8024800BL)

//
// MessageId: WU_E_DS_LOCKTIMEOUTEXPIRED
//
// MessageText:
//
// The data store section could not be locked within the allotted time.
//
#define WU_E_DS_LOCKTIMEOUTEXPIRED       _HRESULT_TYPEDEF_(0x8024800CL)

//
// MessageId: WU_E_DS_NOCATEGORIES
//
// MessageText:
//
// The category was not added because it contains no parent categories and is not a top-level category itself.
//
#define WU_E_DS_NOCATEGORIES             _HRESULT_TYPEDEF_(0x8024800DL)

//
// MessageId: WU_E_DS_ROWEXISTS
//
// MessageText:
//
// The row was not added because an existing row has the same primary key.
//
#define WU_E_DS_ROWEXISTS                _HRESULT_TYPEDEF_(0x8024800EL)

//
// MessageId: WU_E_DS_STOREFILELOCKED
//
// MessageText:
//
// The data store could not be initialized because it was locked by another process.
//
#define WU_E_DS_STOREFILELOCKED          _HRESULT_TYPEDEF_(0x8024800FL)

//
// MessageId: WU_E_DS_CANNOTREGISTER
//
// MessageText:
//
// The data store is not allowed to be registered with COM in the current process.
//
#define WU_E_DS_CANNOTREGISTER           _HRESULT_TYPEDEF_(0x80248010L)

//
// MessageId: WU_E_DS_UNABLETOSTART
//
// MessageText:
//
// Could not create a data store object in another process.
//
#define WU_E_DS_UNABLETOSTART            _HRESULT_TYPEDEF_(0x80248011L)

//
// MessageId: WU_E_DS_DUPLICATEUPDATEID
//
// MessageText:
//
// The server sent the same update to the client with two different revision IDs.
//
#define WU_E_DS_DUPLICATEUPDATEID        _HRESULT_TYPEDEF_(0x80248013L)

//
// MessageId: WU_E_DS_UNKNOWNSERVICE
//
// MessageText:
//
// An operation did not complete because the service is not in the data store.
//
#define WU_E_DS_UNKNOWNSERVICE           _HRESULT_TYPEDEF_(0x80248014L)

//
// MessageId: WU_E_DS_SERVICEEXPIRED
//
// MessageText:
//
// An operation did not complete because the registration of the service has expired.
//
#define WU_E_DS_SERVICEEXPIRED           _HRESULT_TYPEDEF_(0x80248015L)

//
// MessageId: WU_E_DS_DECLINENOTALLOWED
//
// MessageText:
//
// A request to hide an update was declined because it is a mandatory update or because it was deployed with a deadline.
//
#define WU_E_DS_DECLINENOTALLOWED        _HRESULT_TYPEDEF_(0x80248016L)

//
// MessageId: WU_E_DS_TABLESESSIONMISMATCH
//
// MessageText:
//
// A table was not closed because it is not associated with the session.
//
#define WU_E_DS_TABLESESSIONMISMATCH     _HRESULT_TYPEDEF_(0x80248017L)

//
// MessageId: WU_E_DS_SESSIONLOCKMISMATCH
//
// MessageText:
//
// A table was not closed because it is not associated with the session.
//
#define WU_E_DS_SESSIONLOCKMISMATCH      _HRESULT_TYPEDEF_(0x80248018L)

//
// MessageId: WU_E_DS_NEEDWINDOWSSERVICE
//
// MessageText:
//
// A request to remove the Windows Update service or to unregister it with Automatic Updates was declined because it is a built-in service and/or Automatic Updates cannot fall back to another service.
//
#define WU_E_DS_NEEDWINDOWSSERVICE       _HRESULT_TYPEDEF_(0x80248019L)

//
// MessageId: WU_E_DS_INVALIDOPERATION
//
// MessageText:
//
// A request was declined because the operation is not allowed.
//
#define WU_E_DS_INVALIDOPERATION         _HRESULT_TYPEDEF_(0x8024801AL)

//
// MessageId: WU_E_DS_SCHEMAMISMATCH
//
// MessageText:
//
// The schema of the current data store and the schema of a table in a backup XML document do not match.
//
#define WU_E_DS_SCHEMAMISMATCH           _HRESULT_TYPEDEF_(0x8024801BL)

//
// MessageId: WU_E_DS_RESETREQUIRED
//
// MessageText:
//
// The data store requires a session reset; release the session and retry with a new session.
//
#define WU_E_DS_RESETREQUIRED            _HRESULT_TYPEDEF_(0x8024801CL)

//
// MessageId: WU_E_DS_IMPERSONATED
//
// MessageText:
//
// A data store operation did not complete because it was requested with an impersonated identity.
//
#define WU_E_DS_IMPERSONATED             _HRESULT_TYPEDEF_(0x8024801DL)

//
// MessageId: WU_E_DS_UNEXPECTED
//
// MessageText:
//
// A data store error not covered by another WU_E_DS_* code.
//
#define WU_E_DS_UNEXPECTED               _HRESULT_TYPEDEF_(0x80248FFFL)

/////////////////////////////////////////////////////////////////////////////
//Inventory Errors
/////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_INVENTORY_PARSEFAILED
//
// MessageText:
//
// Parsing of the rule file failed.
//
#define WU_E_INVENTORY_PARSEFAILED       _HRESULT_TYPEDEF_(0x80249001L)

//
// MessageId: WU_E_INVENTORY_GET_INVENTORY_TYPE_FAILED
//
// MessageText:
//
// Failed to get the requested inventory type from the server.
//
#define WU_E_INVENTORY_GET_INVENTORY_TYPE_FAILED _HRESULT_TYPEDEF_(0x80249002L)

//
// MessageId: WU_E_INVENTORY_RESULT_UPLOAD_FAILED
//
// MessageText:
//
// Failed to upload inventory result to the server.
//
#define WU_E_INVENTORY_RESULT_UPLOAD_FAILED _HRESULT_TYPEDEF_(0x80249003L)

//
// MessageId: WU_E_INVENTORY_UNEXPECTED
//
// MessageText:
//
// There was an inventory error not covered by another error code.
//
#define WU_E_INVENTORY_UNEXPECTED        _HRESULT_TYPEDEF_(0x80249004L)

//
// MessageId: WU_E_INVENTORY_WMI_ERROR
//
// MessageText:
//
// A WMI error occurred when enumerating the instances for a particular class.
//
#define WU_E_INVENTORY_WMI_ERROR         _HRESULT_TYPEDEF_(0x80249005L)

/////////////////////////////////////////////////////////////////////////////
//AU Errors
/////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_AU_NOSERVICE
//
// MessageText:
//
// Automatic Updates was unable to service incoming requests.
//
#define WU_E_AU_NOSERVICE                _HRESULT_TYPEDEF_(0x8024A000L)

//
// MessageId: WU_E_AU_NONLEGACYSERVER
//
// MessageText:
//
// The old version of the Automatic Updates client has stopped because the WSUS server has been upgraded.
//
#define WU_E_AU_NONLEGACYSERVER          _HRESULT_TYPEDEF_(0x8024A002L)

//
// MessageId: WU_E_AU_LEGACYCLIENTDISABLED
//
// MessageText:
//
// The old version of the Automatic Updates client was disabled.
//
#define WU_E_AU_LEGACYCLIENTDISABLED     _HRESULT_TYPEDEF_(0x8024A003L)

//
// MessageId: WU_E_AU_PAUSED
//
// MessageText:
//
// Automatic Updates was unable to process incoming requests because it was paused.
//
#define WU_E_AU_PAUSED                   _HRESULT_TYPEDEF_(0x8024A004L)

//
// MessageId: WU_E_AU_NO_REGISTERED_SERVICE
//
// MessageText:
//
// No unmanaged service is registered with AU.
//
#define WU_E_AU_NO_REGISTERED_SERVICE    _HRESULT_TYPEDEF_(0x8024A005L)

//
// MessageId: WU_E_AU_UNEXPECTED
//
// MessageText:
//
// An Automatic Updates error not covered by another WU_E_AU * code.
//
#define WU_E_AU_UNEXPECTED               _HRESULT_TYPEDEF_(0x8024AFFFL)

//////////////////////////////////////////////////////////////////////////////
// update handler errors
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_UH_REMOTEUNAVAILABLE
//
// MessageText:
//
// A request for a remote update handler could not be completed because no remote process is available.
//
#define WU_E_UH_REMOTEUNAVAILABLE        _HRESULT_TYPEDEF_(0x80242000L)

//
// MessageId: WU_E_UH_LOCALONLY
//
// MessageText:
//
// A request for a remote update handler could not be completed because the handler is local only.
//
#define WU_E_UH_LOCALONLY                _HRESULT_TYPEDEF_(0x80242001L)

//
// MessageId: WU_E_UH_UNKNOWNHANDLER
//
// MessageText:
//
// A request for an update handler could not be completed because the handler could not be recognized.
//
#define WU_E_UH_UNKNOWNHANDLER           _HRESULT_TYPEDEF_(0x80242002L)

//
// MessageId: WU_E_UH_REMOTEALREADYACTIVE
//
// MessageText:
//
// A remote update handler could not be created because one already exists.
//
#define WU_E_UH_REMOTEALREADYACTIVE      _HRESULT_TYPEDEF_(0x80242003L)

//
// MessageId: WU_E_UH_DOESNOTSUPPORTACTION
//
// MessageText:
//
// A request for the handler to install (uninstall) an update could not be completed because the update does not support install (uninstall).
//
#define WU_E_UH_DOESNOTSUPPORTACTION     _HRESULT_TYPEDEF_(0x80242004L)

//
// MessageId: WU_E_UH_WRONGHANDLER
//
// MessageText:
//
// An operation did not complete because the wrong handler was specified.
//
#define WU_E_UH_WRONGHANDLER             _HRESULT_TYPEDEF_(0x80242005L)

//
// MessageId: WU_E_UH_INVALIDMETADATA
//
// MessageText:
//
// A handler operation could not be completed because the update contains invalid metadata.
//
#define WU_E_UH_INVALIDMETADATA          _HRESULT_TYPEDEF_(0x80242006L)

//
// MessageId: WU_E_UH_INSTALLERHUNG
//
// MessageText:
//
// An operation could not be completed because the installer exceeded the time limit.
//
#define WU_E_UH_INSTALLERHUNG            _HRESULT_TYPEDEF_(0x80242007L)

//
// MessageId: WU_E_UH_OPERATIONCANCELLED
//
// MessageText:
//
// An operation being done by the update handler was cancelled.
//
#define WU_E_UH_OPERATIONCANCELLED       _HRESULT_TYPEDEF_(0x80242008L)

//
// MessageId: WU_E_UH_BADHANDLERXML
//
// MessageText:
//
// An operation could not be completed because the handler-specific metadata is invalid.
//
#define WU_E_UH_BADHANDLERXML            _HRESULT_TYPEDEF_(0x80242009L)

//
// MessageId: WU_E_UH_CANREQUIREINPUT
//
// MessageText:
//
// A request to the handler to install an update could not be completed because the update requires user input.
//
#define WU_E_UH_CANREQUIREINPUT          _HRESULT_TYPEDEF_(0x8024200AL)

//
// MessageId: WU_E_UH_INSTALLERFAILURE
//
// MessageText:
//
// The installer failed to install (uninstall) one or more updates.
//
#define WU_E_UH_INSTALLERFAILURE         _HRESULT_TYPEDEF_(0x8024200BL)

//
// MessageId: WU_E_UH_FALLBACKTOSELFCONTAINED
//
// MessageText:
//
// The update handler should download self-contained content rather than delta-compressed content for the update.
//
#define WU_E_UH_FALLBACKTOSELFCONTAINED  _HRESULT_TYPEDEF_(0x8024200CL)

//
// MessageId: WU_E_UH_NEEDANOTHERDOWNLOAD
//
// MessageText:
//
// The update handler did not install the update because it needs to be downloaded again.
//
#define WU_E_UH_NEEDANOTHERDOWNLOAD      _HRESULT_TYPEDEF_(0x8024200DL)

//
// MessageId: WU_E_UH_NOTIFYFAILURE
//
// MessageText:
//
// The update handler failed to send notification of the status of the install (uninstall) operation.
//
#define WU_E_UH_NOTIFYFAILURE            _HRESULT_TYPEDEF_(0x8024200EL)

//
// MessageId: WU_E_UH_INCONSISTENT_FILE_NAMES
//
// MessageText:
//
// The file names contained in the update metadata and in the update package are inconsistent.
//
#define WU_E_UH_INCONSISTENT_FILE_NAMES  _HRESULT_TYPEDEF_(0x8024200FL)

//
// MessageId: WU_E_UH_FALLBACKERROR
//
// MessageText:
//
// The update handler failed to fall back to the self-contained content.
//
#define WU_E_UH_FALLBACKERROR            _HRESULT_TYPEDEF_(0x80242010L)

//
// MessageId: WU_E_UH_TOOMANYDOWNLOADREQUESTS
//
// MessageText:
//
// The update handler has exceeded the maximum number of download requests.
//
#define WU_E_UH_TOOMANYDOWNLOADREQUESTS  _HRESULT_TYPEDEF_(0x80242011L)

//
// MessageId: WU_E_UH_UNEXPECTEDCBSRESPONSE
//
// MessageText:
//
// The update handler has received an unexpected response from CBS.
//
#define WU_E_UH_UNEXPECTEDCBSRESPONSE    _HRESULT_TYPEDEF_(0x80242012L)

//
// MessageId: WU_E_UH_BADCBSPACKAGEID
//
// MessageText:
//
// The update metadata contains an invalid CBS package identifier.
//
#define WU_E_UH_BADCBSPACKAGEID          _HRESULT_TYPEDEF_(0x80242013L)

//
// MessageId: WU_E_UH_POSTREBOOTSTILLPENDING
//
// MessageText:
//
// The post-reboot operation for the update is still in progress.
//
#define WU_E_UH_POSTREBOOTSTILLPENDING   _HRESULT_TYPEDEF_(0x80242014L)

//
// MessageId: WU_E_UH_POSTREBOOTRESULTUNKNOWN
//
// MessageText:
//
// The result of the post-reboot operation for the update could not be determined.
//
#define WU_E_UH_POSTREBOOTRESULTUNKNOWN  _HRESULT_TYPEDEF_(0x80242015L)

//
// MessageId: WU_E_UH_POSTREBOOTUNEXPECTEDSTATE
//
// MessageText:
//
// The state of the update after its post-reboot operation has completed is unexpected.
//
#define WU_E_UH_POSTREBOOTUNEXPECTEDSTATE _HRESULT_TYPEDEF_(0x80242016L)

//
// MessageId: WU_E_UH_NEW_SERVICING_STACK_REQUIRED
//
// MessageText:
//
// The OS servicing stack must be updated before this update is downloaded or installed.
//
#define WU_E_UH_NEW_SERVICING_STACK_REQUIRED _HRESULT_TYPEDEF_(0x80242017L)

//
// MessageId: WU_E_UH_UNEXPECTED
//
// MessageText:
//
// An update handler error not covered by another WU_E_UH_* code.
//
#define WU_E_UH_UNEXPECTED               _HRESULT_TYPEDEF_(0x80242FFFL)

//////////////////////////////////////////////////////////////////////////////
// download manager errors
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_DM_URLNOTAVAILABLE
//
// MessageText:
//
// A download manager operation could not be completed because the requested file does not have a URL.
//
#define WU_E_DM_URLNOTAVAILABLE          _HRESULT_TYPEDEF_(0x80246001L)

//
// MessageId: WU_E_DM_INCORRECTFILEHASH
//
// MessageText:
//
// A download manager operation could not be completed because the file digest was not recognized.
//
#define WU_E_DM_INCORRECTFILEHASH        _HRESULT_TYPEDEF_(0x80246002L)

//
// MessageId: WU_E_DM_UNKNOWNALGORITHM
//
// MessageText:
//
// A download manager operation could not be completed because the file metadata requested an unrecognized hash algorithm.
//
#define WU_E_DM_UNKNOWNALGORITHM         _HRESULT_TYPEDEF_(0x80246003L)

//
// MessageId: WU_E_DM_NEEDDOWNLOADREQUEST
//
// MessageText:
//
// An operation could not be completed because a download request is required from the download handler.
//
#define WU_E_DM_NEEDDOWNLOADREQUEST      _HRESULT_TYPEDEF_(0x80246004L)

//
// MessageId: WU_E_DM_NONETWORK
//
// MessageText:
//
// A download manager operation could not be completed because the network connection was unavailable.
//
#define WU_E_DM_NONETWORK                _HRESULT_TYPEDEF_(0x80246005L)

//
// MessageId: WU_E_DM_WRONGBITSVERSION
//
// MessageText:
//
// A download manager operation could not be completed because the version of Background Intelligent Transfer Service (BITS) is incompatible.
//
#define WU_E_DM_WRONGBITSVERSION         _HRESULT_TYPEDEF_(0x80246006L)

//
// MessageId: WU_E_DM_NOTDOWNLOADED
//
// MessageText:
//
// The update has not been downloaded.
//
#define WU_E_DM_NOTDOWNLOADED            _HRESULT_TYPEDEF_(0x80246007L)

//
// MessageId: WU_E_DM_FAILTOCONNECTTOBITS
//
// MessageText:
//
// A download manager operation failed because the download manager was unable to connect the Background Intelligent Transfer Service (BITS).
//
#define WU_E_DM_FAILTOCONNECTTOBITS      _HRESULT_TYPEDEF_(0x80246008L)

//
// MessageId: WU_E_DM_BITSTRANSFERERROR
//
// MessageText:
//
// A download manager operation failed because there was an unspecified Background Intelligent Transfer Service (BITS) transfer error.
//
#define WU_E_DM_BITSTRANSFERERROR        _HRESULT_TYPEDEF_(0x80246009L)

//
// MessageId: WU_E_DM_DOWNLOADLOCATIONCHANGED
//
// MessageText:
//
// A download must be restarted because the location of the source of the download has changed.
//
#define WU_E_DM_DOWNLOADLOCATIONCHANGED  _HRESULT_TYPEDEF_(0x8024600AL)

//
// MessageId: WU_E_DM_CONTENTCHANGED
//
// MessageText:
//
// A download must be restarted because the update content changed in a new revision.
//
#define WU_E_DM_CONTENTCHANGED           _HRESULT_TYPEDEF_(0x8024600BL)

//
// MessageId: WU_E_DM_UNEXPECTED
//
// MessageText:
//
// There was a download manager error not covered by another WU_E_DM_* error code.
//
#define WU_E_DM_UNEXPECTED               _HRESULT_TYPEDEF_(0x80246FFFL)

//////////////////////////////////////////////////////////////////////////////
// Setup/SelfUpdate errors
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_SETUP_INVALID_INFDATA
//
// MessageText:
//
// Windows Update Agent could not be updated because an INF file contains invalid information.
//
#define WU_E_SETUP_INVALID_INFDATA       _HRESULT_TYPEDEF_(0x8024D001L)

//
// MessageId: WU_E_SETUP_INVALID_IDENTDATA
//
// MessageText:
//
// Windows Update Agent could not be updated because the wuident.cab file contains invalid information.
//
#define WU_E_SETUP_INVALID_IDENTDATA     _HRESULT_TYPEDEF_(0x8024D002L)

//
// MessageId: WU_E_SETUP_ALREADY_INITIALIZED
//
// MessageText:
//
// Windows Update Agent could not be updated because of an internal error that caused setup initialization to be performed twice.
//
#define WU_E_SETUP_ALREADY_INITIALIZED   _HRESULT_TYPEDEF_(0x8024D003L)

//
// MessageId: WU_E_SETUP_NOT_INITIALIZED
//
// MessageText:
//
// Windows Update Agent could not be updated because setup initialization never completed successfully.
//
#define WU_E_SETUP_NOT_INITIALIZED       _HRESULT_TYPEDEF_(0x8024D004L)

//
// MessageId: WU_E_SETUP_SOURCE_VERSION_MISMATCH
//
// MessageText:
//
// Windows Update Agent could not be updated because the versions specified in the INF do not match the actual source file versions.
//
#define WU_E_SETUP_SOURCE_VERSION_MISMATCH _HRESULT_TYPEDEF_(0x8024D005L)

//
// MessageId: WU_E_SETUP_TARGET_VERSION_GREATER
//
// MessageText:
//
// Windows Update Agent could not be updated because a WUA file on the target system is newer than the corresponding source file.
//
#define WU_E_SETUP_TARGET_VERSION_GREATER _HRESULT_TYPEDEF_(0x8024D006L)

//
// MessageId: WU_E_SETUP_REGISTRATION_FAILED
//
// MessageText:
//
// Windows Update Agent could not be updated because regsvr32.exe returned an error.
//
#define WU_E_SETUP_REGISTRATION_FAILED   _HRESULT_TYPEDEF_(0x8024D007L)

//
// MessageId: WU_E_SELFUPDATE_SKIP_ON_FAILURE
//
// MessageText:
//
// An update to the Windows Update Agent was skipped because previous attempts to update have failed.
//
#define WU_E_SELFUPDATE_SKIP_ON_FAILURE  _HRESULT_TYPEDEF_(0x8024D008L)

//
// MessageId: WU_E_SETUP_SKIP_UPDATE
//
// MessageText:
//
// An update to the Windows Update Agent was skipped due to a directive in the wuident.cab file.
//
#define WU_E_SETUP_SKIP_UPDATE           _HRESULT_TYPEDEF_(0x8024D009L)

//
// MessageId: WU_E_SETUP_UNSUPPORTED_CONFIGURATION
//
// MessageText:
//
// Windows Update Agent could not be updated because the current system configuration is not supported.
//
#define WU_E_SETUP_UNSUPPORTED_CONFIGURATION _HRESULT_TYPEDEF_(0x8024D00AL)

//
// MessageId: WU_E_SETUP_BLOCKED_CONFIGURATION
//
// MessageText:
//
// Windows Update Agent could not be updated because the system is configured to block the update.
//
#define WU_E_SETUP_BLOCKED_CONFIGURATION _HRESULT_TYPEDEF_(0x8024D00BL)

//
// MessageId: WU_E_SETUP_REBOOT_TO_FIX
//
// MessageText:
//
// Windows Update Agent could not be updated because a restart of the system is required.
//
#define WU_E_SETUP_REBOOT_TO_FIX         _HRESULT_TYPEDEF_(0x8024D00CL)

//
// MessageId: WU_E_SETUP_ALREADYRUNNING
//
// MessageText:
//
// Windows Update Agent setup is already running.
//
#define WU_E_SETUP_ALREADYRUNNING        _HRESULT_TYPEDEF_(0x8024D00DL)

//
// MessageId: WU_E_SETUP_REBOOTREQUIRED
//
// MessageText:
//
// Windows Update Agent setup package requires a reboot to complete installation.
//
#define WU_E_SETUP_REBOOTREQUIRED        _HRESULT_TYPEDEF_(0x8024D00EL)

//
// MessageId: WU_E_SETUP_HANDLER_EXEC_FAILURE
//
// MessageText:
//
// Windows Update Agent could not be updated because the setup handler failed during execution.
//
#define WU_E_SETUP_HANDLER_EXEC_FAILURE  _HRESULT_TYPEDEF_(0x8024D00FL)

//
// MessageId: WU_E_SETUP_INVALID_REGISTRY_DATA
//
// MessageText:
//
// Windows Update Agent could not be updated because the registry contains invalid information.
//
#define WU_E_SETUP_INVALID_REGISTRY_DATA _HRESULT_TYPEDEF_(0x8024D010L)

//
// MessageId: WU_E_SELFUPDATE_REQUIRED
//
// MessageText:
//
// Windows Update Agent must be updated before search can continue.
//
#define WU_E_SELFUPDATE_REQUIRED         _HRESULT_TYPEDEF_(0x8024D011L)

//
// MessageId: WU_E_SELFUPDATE_REQUIRED_ADMIN
//
// MessageText:
//
// Windows Update Agent must be updated before search can continue.  An administrator is required to perform the operation.
//
#define WU_E_SELFUPDATE_REQUIRED_ADMIN   _HRESULT_TYPEDEF_(0x8024D012L)

//
// MessageId: WU_E_SETUP_WRONG_SERVER_VERSION
//
// MessageText:
//
// Windows Update Agent could not be updated because the server does not contain update information for this version.
//
#define WU_E_SETUP_WRONG_SERVER_VERSION  _HRESULT_TYPEDEF_(0x8024D013L)

//
// MessageId: WU_E_SETUP_UNEXPECTED
//
// MessageText:
//
// Windows Update Agent could not be updated because of an error not covered by another WU_E_SETUP_* error code.
//
#define WU_E_SETUP_UNEXPECTED            _HRESULT_TYPEDEF_(0x8024DFFFL)

//////////////////////////////////////////////////////////////////////////////
// expression evaluator errors
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_EE_UNKNOWN_EXPRESSION
//
// MessageText:
//
// An expression evaluator operation could not be completed because an expression was unrecognized.
//
#define WU_E_EE_UNKNOWN_EXPRESSION       _HRESULT_TYPEDEF_(0x8024E001L)

//
// MessageId: WU_E_EE_INVALID_EXPRESSION
//
// MessageText:
//
// An expression evaluator operation could not be completed because an expression was invalid.
//
#define WU_E_EE_INVALID_EXPRESSION       _HRESULT_TYPEDEF_(0x8024E002L)

//
// MessageId: WU_E_EE_MISSING_METADATA
//
// MessageText:
//
// An expression evaluator operation could not be completed because an expression contains an incorrect number of metadata nodes.
//
#define WU_E_EE_MISSING_METADATA         _HRESULT_TYPEDEF_(0x8024E003L)

//
// MessageId: WU_E_EE_INVALID_VERSION
//
// MessageText:
//
// An expression evaluator operation could not be completed because the version of the serialized expression data is invalid.
//
#define WU_E_EE_INVALID_VERSION          _HRESULT_TYPEDEF_(0x8024E004L)

//
// MessageId: WU_E_EE_NOT_INITIALIZED
//
// MessageText:
//
// The expression evaluator could not be initialized.
//
#define WU_E_EE_NOT_INITIALIZED          _HRESULT_TYPEDEF_(0x8024E005L)

//
// MessageId: WU_E_EE_INVALID_ATTRIBUTEDATA
//
// MessageText:
//
// An expression evaluator operation could not be completed because there was an invalid attribute.
//
#define WU_E_EE_INVALID_ATTRIBUTEDATA    _HRESULT_TYPEDEF_(0x8024E006L)

//
// MessageId: WU_E_EE_CLUSTER_ERROR
//
// MessageText:
//
// An expression evaluator operation could not be completed because the cluster state of the computer could not be determined.
//
#define WU_E_EE_CLUSTER_ERROR            _HRESULT_TYPEDEF_(0x8024E007L)

//
// MessageId: WU_E_EE_UNEXPECTED
//
// MessageText:
//
// There was an expression evaluator error not covered by another WU_E_EE_* error code.
//
#define WU_E_EE_UNEXPECTED               _HRESULT_TYPEDEF_(0x8024EFFFL)

//////////////////////////////////////////////////////////////////////////////
// UI errors
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_INSTALLATION_RESULTS_UNKNOWN_VERSION
//
// MessageText:
//
// The results of download and installation could not be read from the registry due to an unrecognized data format version.
//
#define WU_E_INSTALLATION_RESULTS_UNKNOWN_VERSION _HRESULT_TYPEDEF_(0x80243001L)

//
// MessageId: WU_E_INSTALLATION_RESULTS_INVALID_DATA
//
// MessageText:
//
// The results of download and installation could not be read from the registry due to an invalid data format.
//
#define WU_E_INSTALLATION_RESULTS_INVALID_DATA _HRESULT_TYPEDEF_(0x80243002L)

//
// MessageId: WU_E_INSTALLATION_RESULTS_NOT_FOUND
//
// MessageText:
//
// The results of download and installation are not available; the operation may have failed to start.
//
#define WU_E_INSTALLATION_RESULTS_NOT_FOUND _HRESULT_TYPEDEF_(0x80243003L)

//
// MessageId: WU_E_TRAYICON_FAILURE
//
// MessageText:
//
// A failure occurred when trying to create an icon in the taskbar notification area.
//
#define WU_E_TRAYICON_FAILURE            _HRESULT_TYPEDEF_(0x80243004L)

//
// MessageId: WU_E_NON_UI_MODE
//
// MessageText:
//
// Unable to show UI when in non-UI mode; WU client UI modules may not be installed.
//
#define WU_E_NON_UI_MODE                 _HRESULT_TYPEDEF_(0x80243FFDL)

//
// MessageId: WU_E_WUCLTUI_UNSUPPORTED_VERSION
//
// MessageText:
//
// Unsupported version of WU client UI exported functions.
//
#define WU_E_WUCLTUI_UNSUPPORTED_VERSION _HRESULT_TYPEDEF_(0x80243FFEL)

//
// MessageId: WU_E_AUCLIENT_UNEXPECTED
//
// MessageText:
//
// There was a user interface error not covered by another WU_E_AUCLIENT_* error code.
//
#define WU_E_AUCLIENT_UNEXPECTED         _HRESULT_TYPEDEF_(0x80243FFFL)

//////////////////////////////////////////////////////////////////////////////
// reporter errors
///////////////////////////////////////////////////////////////////////////////
//
// MessageId: WU_E_REPORTER_EVENTCACHECORRUPT
//
// MessageText:
//
// The event cache file was defective.
//
#define WU_E_REPORTER_EVENTCACHECORRUPT  _HRESULT_TYPEDEF_(0x8024F001L)

//
// MessageId: WU_E_REPORTER_EVENTNAMESPACEPARSEFAILED
//
// MessageText:
//
// The XML in the event namespace descriptor could not be parsed.
//
#define WU_E_REPORTER_EVENTNAMESPACEPARSEFAILED _HRESULT_TYPEDEF_(0x8024F002L)

//
// MessageId: WU_E_INVALID_EVENT
//
// MessageText:
//
// The XML in the event namespace descriptor could not be parsed.
//
#define WU_E_INVALID_EVENT               _HRESULT_TYPEDEF_(0x8024F003L)

//
// MessageId: WU_E_SERVER_BUSY
//
// MessageText:
//
// The server rejected an event because the server was too busy.
//
#define WU_E_SERVER_BUSY                 _HRESULT_TYPEDEF_(0x8024F004L)

//
// MessageId: WU_E_REPORTER_UNEXPECTED
//
// MessageText:
//
// There was a reporter error not covered by another error code.
//
#define WU_E_REPORTER_UNEXPECTED         _HRESULT_TYPEDEF_(0x8024FFFFL)

//
// MessageId: WU_E_OL_INVALID_SCANFILE
//
// MessageText:
//
// An operation could not be completed because the scan package was invalid.
//
#define WU_E_OL_INVALID_SCANFILE         _HRESULT_TYPEDEF_(0x80247001L)

//
// MessageId: WU_E_OL_NEWCLIENT_REQUIRED
//
// MessageText:
//
// An operation could not be completed because the scan package requires a greater version of the Windows Update Agent.
//
#define WU_E_OL_NEWCLIENT_REQUIRED       _HRESULT_TYPEDEF_(0x80247002L)

//
// MessageId: WU_E_OL_UNEXPECTED
//
// MessageText:
//
// Search using the scan package failed.
//
#define WU_E_OL_UNEXPECTED               _HRESULT_TYPEDEF_(0x80247FFFL)

#endif //_WUERROR_
