// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "Utility.h"
#include "resources.h"

#define EVENTLOG(log, name, ...) UTILITY::LogEventF(log, ASPNETCORE_EVENT_ ## name ## _LEVEL, ASPNETCORE_EVENT_ ## name, ASPNETCORE_EVENT_ ## name ## _MSG, __VA_ARGS__)
