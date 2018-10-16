// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

//
// Use C++ standard 'nullptr'
//

#ifdef NULL
#undef NULL
#endif

#ifdef __cplusplus
#ifdef _NATIVE_NULLPTR_SUPPORTED
#define NULL    nullptr
#else
#define NULL    0
#define nullptr 0
#endif
#else
#define NULL    ((void *)0)
//#define nullptr ((void *)0)
#endif
