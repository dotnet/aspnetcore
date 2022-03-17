// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#ifndef _DATETIME_H_
#define _DATETIME_H_
#include <ahadmin.h>

BOOL
StringTimeToFileTime(
    PCSTR           pszTime,
    ULONGLONG *     pulTime
);

#endif

