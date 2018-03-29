// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

namespace signalr
{
    enum class trace_level : int
    {
        none = 0,
        messages = 1,
        events = 2,
        state_changes = 4,
        errors = 8,
        info = 16,
        all = messages | events | state_changes | errors | info
    };

    inline trace_level operator|(trace_level lhs, trace_level rhs)
    {
        return static_cast<trace_level>(static_cast<int>(lhs) | static_cast<int>(rhs));
    }

    inline trace_level operator&(trace_level lhs, trace_level rhs)
    {
        return static_cast<trace_level>(static_cast<int>(lhs) & static_cast<int>(rhs));
    }
}