// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

struct ErrorContext
{
    // TODO consider adding HRESULT here
    std::string detailedErrorContent;
    USHORT statusCode;
    USHORT subStatusCode;
    std::string generalErrorType;
    std::string errorReason;
};
