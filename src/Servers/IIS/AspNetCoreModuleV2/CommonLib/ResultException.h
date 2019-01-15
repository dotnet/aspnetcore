// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class ResultException: public std::runtime_error
{
public:
    ResultException(HRESULT hr, LOCATION_ARGUMENTS_ONLY) :
        runtime_error(format("HRESULT 0x%x returned at " LOCATION_FORMAT, hr, LOCATION_CALL_ONLY)),
        m_hr(hr)
    {
    }

    HRESULT GetResult() const noexcept { return m_hr; }

private:
    HRESULT m_hr;
};
