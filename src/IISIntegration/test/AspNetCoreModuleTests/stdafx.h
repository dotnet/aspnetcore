// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN

#include <Windows.h>
#include <atlbase.h>
#include <pdh.h>
#include <vector>
#include <Shlobj.h>
#include <httpserv.h>
#include <winhttp.h>
#include <httptrace.h>
#include <cstdlib>
#include <wchar.h>
#include <io.h>
#include <stdio.h>

#include <hashfn.h>
#include <hashtable.h>
#include "stringa.h"
#include "stringu.h"
#include "dbgutil.h"
#include "ahutil.h"
#include "multisz.h"
#include "multisza.h"
#include "base64.h"
#include <listentry.h>
#include <datetime.h>
#include <reftrace.h>
#include <acache.h>
#include <time.h>

#include "..\..\src\AspNetCoreModuleV2\IISLib\hashtable.h"
#include "..\..\src\AspNetCoreModuleV2\IISLib\stringu.h"
#include "..\..\src\AspNetCoreModuleV2\IISLib\stringa.h"
#include "..\..\src\AspNetCoreModuleV2\IISLib\multisz.h"
#include "..\..\src\AspNetCoreModuleV2\IISLib\dbgutil.h"
#include "..\..\src\AspNetCoreModuleV2\IISLib\ahutil.h"
#include "..\..\src\AspNetCoreModuleV2\IISLib\hashfn.h"

#include "..\..\src\AspNetCoreModuleV2\CommonLib\hostfxr_utility.h"
#include "..\..\src\AspNetCoreModuleV2\CommonLib\environmentvariablehash.h"
#include "..\..\src\AspNetCoreModuleV2\CommonLib\aspnetcoreconfig.h"
#include "..\..\src\AspNetCoreModuleV2\CommonLib\application.h"
#include "..\..\src\AspNetCoreModuleV2\CommonLib\utility.h"
#include "..\..\src\AspNetCoreModuleV2\CommonLib\debugutil.h"
#include "..\..\src\AspNetCoreModuleV2\CommonLib\requesthandler.h"
#include "..\..\src\AspNetCoreModuleV2\CommonLib\resources.h"
#include "..\..\src\AspNetCoreModuleV2\CommonLib\aspnetcore_msg.h"

#include "CppUnitTest.h"
