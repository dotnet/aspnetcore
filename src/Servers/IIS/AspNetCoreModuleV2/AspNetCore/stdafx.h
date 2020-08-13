// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#pragma warning( disable : 4091)

//
// System related headers
//
#define WIN32_LEAN_AND_MEAN

#define NTDDI_VERSION NTDDI_WIN7
#define WINVER _WIN32_WINNT_WIN7 
#define _WIN32_WINNT 0x0601

#include <Windows.h>
#include <atlbase.h>
#include <httpserv.h>
#include <ntassert.h>
#include "stringu.h"
#include "stringa.h"

#pragma warning( error : 4091)
