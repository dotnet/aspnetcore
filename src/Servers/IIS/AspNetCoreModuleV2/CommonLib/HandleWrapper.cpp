// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "HandleWrapper.h"

// Workaround for VC++ bug https://developercommunity.visualstudio.com/content/problem/33928/constexpr-failing-on-nullptr-v141-compiler-regress.html
const HANDLE InvalidHandleTraits::DefaultHandle = INVALID_HANDLE_VALUE;
const HANDLE FindFileHandleTraits::DefaultHandle = INVALID_HANDLE_VALUE;
