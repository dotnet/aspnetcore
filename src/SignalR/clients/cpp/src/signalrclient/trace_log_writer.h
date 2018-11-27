// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include "signalrclient/log_writer.h"

namespace signalr
{
    class trace_log_writer : public log_writer
    {
    public:
        void __cdecl write(const utility::string_t &entry) override;
    };
}