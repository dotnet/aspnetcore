// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _DATETIME_H_
#define _DATETIME_H_

BOOL
StringTimeToFileTime(
    PCSTR           pszTime,
    ULONGLONG *     pulTime
);

#endif

