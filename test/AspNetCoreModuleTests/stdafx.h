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

#include "..\..\src\IISLib\hashtable.h"
#include "..\..\src\IISLib\stringu.h"
#include "..\..\src\IISLib\stringa.h"
#include "..\..\src\IISLib\multisz.h"
#include "..\..\src\IISLib\dbgutil.h"
#include "..\..\src\IISLib\ahutil.h"
#include "..\..\src\IISLib\hashfn.h"

#include "..\..\src\CommonLib\hostfxr_utility.h"
#include "..\..\src\CommonLib\environmentvariablehash.h"
#include "..\..\src\CommonLib\aspnetcoreconfig.h"
#include "..\..\src\CommonLib\application.h"
#include "..\..\src\CommonLib\utility.h"
#include "..\..\src\CommonLib\debugutil.h"
#include "..\..\src\CommonLib\requesthandler.h"
#include "..\..\src\CommonLib\resources.h"
#include "..\..\src\CommonLib\aspnetcore_msg.h"

#include "CppUnitTest.h"
