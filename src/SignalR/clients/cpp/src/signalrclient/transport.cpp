// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "transport.h"
#include "connection_impl.h"

namespace signalr
{
    transport::transport(const logger& logger)
        : m_logger(logger)
    {}

    // Do NOT remove this destructor. Letting the compiler generate and inline the default dtor may lead to
    // undefinded behavior since we are using an incomplete type. More details here:  http://herbsutter.com/gotw/_100/
    transport::~transport()
    { }
}
