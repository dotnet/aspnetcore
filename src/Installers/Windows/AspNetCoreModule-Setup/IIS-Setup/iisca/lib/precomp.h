// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once 

// Don't throw exceptions for CComVariant failures.
// Check for VT_ERROR instead.
#define _ATL_NO_VARIANT_THROW
#define WIN32_LEAN_AND_MEAN

#include <atlbase.h>
#include <atlcom.h>
#include <windows.h>
#include <ahadmin.h>
#pragma warning( disable:4127 )
#include <atlcomcli.h>
#include "dbgutil.h"
#include <msiquery.h>
#define IRTL_DLLEXP
#include <stringu.h>
#include <strsafe.h>
#include <xmllite.h>
#include <shlobj.h>
#include <http.h>
#include <sddl.h>
#include <netfw.h>

//
// Security APIs.
//
#include <Aclapi.h>
#include <lm.h>
#include <lmaccess.h>
#include <Ntsecapi.h>


#include "ahutil.h"
#include "msiutil.h"
#include "defaults.h"
#include "cgi_restrictions.h"
#include "handlers.h"
#include "tracing.h"
#include "config_custom.h"
#include "setup_log.h"
#include "httpapi.h"
#include "secutils.h"
#include "ConfigShared.h"
#include "iisca.h"
#include "iiscaexp.h"
extern HINSTANCE g_hinst;

