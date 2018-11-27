// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#ifdef _WIN32 // used in the default log writer and to build the dll

// prevents from defining min/max macros that conflict with std::min()/std::max() functions
#define NOMINMAX

#include <SDKDDKVer.h>

#define WIN32_LEAN_AND_MEAN

#include <windows.h>

#endif

#include <functional>
#include <unordered_map>
#include "cpprest/details/basic_types.h"
#include "cpprest/json.h"
#include "pplx/pplxtasks.h"